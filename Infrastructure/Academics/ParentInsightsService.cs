using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    // Group-centric insights for a parent's child, including in-group ranking.
    //
    // Ranking blend: 70% grade average + 30% attendance. Attendance excludes
    // Excused sessions from both numerator and denominator, so an excused absence
    // never helps or hurts the standing. A student missing one input is ranked on
    // the other alone; a student with neither grades nor counted attendance is not
    // ranked (Rank = null).
    public class ParentInsightsService : IParentInsightsService
    {
        private const double GradeWeight = 0.7;
        private const double AttendanceWeight = 0.3;

        private readonly ApplicationDbContext _db;
        private readonly TenantDbContext _tenantDb;

        public ParentInsightsService(ApplicationDbContext db, TenantDbContext tenantDb)
        {
            _db = db;
            _tenantDb = tenantDb;
        }

        public async Task<List<ChildGroupOverviewDto>> GetChildGroupsOverviewAsync(string childId, CancellationToken ct = default)
        {
            var myGroups = await _db.GroupStudents
                .Where(gs => gs.StudentId == childId)
                .Select(gs => new
                {
                    gs.GroupId,
                    gs.Group.Name,
                    gs.Group.Subject,
                    gs.Group.OwnerUserId,
                    gs.Group.TenantId,
                })
                .ToListAsync(ct);
            if (myGroups.Count == 0) return new();

            var groupIds = myGroups.Select(g => g.GroupId).ToList();

            // Every enrolled student across the child's groups — the ranking pool.
            var enrollments = await _db.GroupStudents
                .Where(gs => groupIds.Contains(gs.GroupId))
                .Select(gs => new { gs.GroupId, gs.StudentId })
                .ToListAsync(ct);

            // All exam grades in those groups (every student) for the grade component.
            var grades = await _db.StudentGrades
                .Where(sg => groupIds.Contains(sg.Exam.GroupId) && sg.Exam.MaxScore > 0)
                .Select(sg => new { sg.StudentId, sg.Exam.GroupId, sg.Score, sg.Exam.MaxScore })
                .ToListAsync(ct);

            // All completed-session attendance in those groups (every student).
            var attendance = await _db.AttendanceRecords
                .Where(a => groupIds.Contains(a.Occurrence!.GroupId) && a.Occurrence.Status == SessionStatus.Completed)
                .Select(a => new { a.StudentId, a.Occurrence!.GroupId, a.Status })
                .ToListAsync(ct);

            // The child's own fees, per group.
            var payRecords = await _db.StudentPaymentRecords
                .Where(r => r.StudentId == childId && groupIds.Contains(r.GroupId))
                .Select(r => new
                {
                    r.GroupId,
                    r.Status,
                    r.ExpectedAmount,
                    r.DiscountAmount,
                    Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m,
                })
                .ToListAsync(ct);

            // Teacher name: prefer the owning teacher's name, else the tenant (center) name.
            var ownerIds = myGroups.Where(g => g.OwnerUserId != null).Select(g => g.OwnerUserId!).Distinct().ToList();
            var ownerNames = ownerIds.Count == 0
                ? new Dictionary<string, string>()
                : await _db.Users.Where(u => ownerIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), ct);
            var tenantIds = myGroups.Select(g => g.TenantId).Distinct().ToList();
            var tenantNames = await _tenantDb.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            var result = new List<ChildGroupOverviewDto>();
            foreach (var g in myGroups)
            {
                // Combined score for every student in this group → the child's rank.
                var scores = new Dictionary<string, double>();
                foreach (var sid in enrollments.Where(e => e.GroupId == g.GroupId).Select(e => e.StudentId).Distinct())
                {
                    var sGrades = grades.Where(x => x.GroupId == g.GroupId && x.StudentId == sid).ToList();
                    double? gPct = sGrades.Count > 0
                        ? sGrades.Average(x => (double)x.Score / (double)x.MaxScore * 100)
                        : (double?)null;

                    var sAtt = attendance.Where(x => x.GroupId == g.GroupId && x.StudentId == sid).ToList();
                    int p = sAtt.Count(x => x.Status == AttendanceStatus.Present);
                    int ab = sAtt.Count(x => x.Status == AttendanceStatus.Absent);
                    int cnt = p + ab;
                    double? aPct = cnt > 0 ? (double)p / cnt * 100 : (double?)null;

                    double? score =
                        gPct.HasValue && aPct.HasValue ? GradeWeight * gPct.Value + AttendanceWeight * aPct.Value
                        : gPct ?? aPct;
                    if (score.HasValue) scores[sid] = score.Value;
                }

                int? rank = null;
                if (scores.TryGetValue(childId, out var myScore))
                    rank = scores.Values.Count(v => v > myScore) + 1; // standard competition ranking

                // The child's own attendance / grades for display.
                var myAtt = attendance.Where(x => x.GroupId == g.GroupId && x.StudentId == childId).ToList();
                int present = myAtt.Count(x => x.Status == AttendanceStatus.Present);
                int absent = myAtt.Count(x => x.Status == AttendanceStatus.Absent);
                int excused = myAtt.Count(x => x.Status == AttendanceStatus.Excused);
                int counted = present + absent;

                var myGrades = grades.Where(x => x.GroupId == g.GroupId && x.StudentId == childId).ToList();
                double? gradeAvg = myGrades.Count > 0
                    ? Math.Round(myGrades.Average(x => (double)x.Score / (double)x.MaxScore * 100), 0)
                    : null;

                var pr = payRecords.Where(x => x.GroupId == g.GroupId).ToList();
                decimal expected = pr.Sum(x => x.Status == PaymentStatus.Waived ? 0m : x.ExpectedAmount - x.DiscountAmount);
                decimal paid = pr.Sum(x => x.Paid);

                result.Add(new ChildGroupOverviewDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.Name,
                    Subject = g.Subject ?? string.Empty,
                    TeacherName = g.OwnerUserId != null && ownerNames.TryGetValue(g.OwnerUserId, out var tn) && !string.IsNullOrWhiteSpace(tn)
                        ? tn
                        : (tenantNames.TryGetValue(g.TenantId, out var cn) ? cn : string.Empty),
                    Present = present,
                    Absent = absent,
                    Excused = excused,
                    TotalCompleted = myAtt.Count,
                    AttendanceRate = counted > 0 ? Math.Round((double)present / counted * 100, 0) : 0,
                    GradesAveragePercent = gradeAvg,
                    ExamsCount = myGrades.Count,
                    Rank = rank,
                    RankedStudents = scores.Count,
                    TotalExpected = expected,
                    TotalPaid = paid,
                    TotalRemaining = Math.Max(0m, expected - paid),
                });
            }

            return result.OrderBy(r => r.GroupName).ToList();
        }
    }
}
