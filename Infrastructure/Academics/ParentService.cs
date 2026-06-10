using Application.Exceptions;
using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Services
{
    public class ParentService : IParentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IStudentService _studentService;
        private readonly IAttendanceService _attendanceService;
        private readonly IPaymentService _paymentService;
        private readonly IGradeService _gradeService;
        private readonly TenantDbContext _tenantDbContext;
        private readonly IDateTimeService _dateTime;

        public ParentService(
            ApplicationDbContext dbContext,
            IStudentService studentService,
            IAttendanceService attendanceService,
            IPaymentService paymentService,
            IGradeService gradeService,
            TenantDbContext tenantDbContext,
            IDateTimeService dateTime)
        {
            _dbContext = dbContext;
            _studentService = studentService;
            _attendanceService = attendanceService;
            _paymentService = paymentService;
            _gradeService = gradeService;
            _tenantDbContext = tenantDbContext;
            _dateTime = dateTime;
        }

        public async Task<List<LinkedChildDto>> GetMyChildrenAsync(string parentId, CancellationToken ct = default)
        {
            return await _dbContext.ParentStudentLinks
                .Include(l => l.Student)
                .ThenInclude(s => s.StudentProfile)
                .Where(l => l.ParentUserId == parentId && l.Status == LinkStatus.Accepted)
                .Select(l => new LinkedChildDto
                {
                    StudentId = l.StudentUserId,
                    FullName = $"{l.Student.FirstName} {l.Student.LastName}",
                    EducationalStage = l.Student.StudentProfile!.EducationalStage,
                    GradeYear = l.Student.StudentProfile!.GradeYear
                }).ToListAsync(ct);
        }

        public async Task<ChildDetailsDto> GetChildDetailsAsync(string parentId, string childId, DateOnly today, CancellationToken ct = default)
        {
            var link = await _dbContext.ParentStudentLinks
                .Include(l => l.Student)
                .ThenInclude(s => s.StudentProfile)
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId && l.StudentUserId == childId && l.Status == LinkStatus.Accepted, ct);

            if (link == null)
                throw new ForbiddenException(["You do not have access to this student's data."]);

            var childInfo = new LinkedChildDto
            {
                StudentId = link.StudentUserId,
                FullName = $"{link.Student.FirstName} {link.Student.LastName}",
                EducationalStage = link.Student.StudentProfile!.EducationalStage,
                GradeYear = link.Student.StudentProfile!.GradeYear
            };

            var groups = await _studentService.GetMyGroupsAsync(childId, ct);
            var schedule = await _studentService.GetMyTodayScheduleAsync(childId, today, ct);

            return new ChildDetailsDto
            {
                ChildInfo = childInfo,
                Groups = groups,
                TodaySchedule = schedule
            };
        }

        public async Task<bool> IsParentLinkedToChildAsync(string parentId, string childId, CancellationToken ct = default)
        {
            return await _dbContext.ParentStudentLinks
                .AnyAsync(l => l.ParentUserId == parentId && l.StudentUserId == childId && l.Status == LinkStatus.Accepted, ct);
        }

        // ════════════════════════════════════════════════════════════════════
        // Aggregated overview — single round-trip for the dashboard card.
        // ════════════════════════════════════════════════════════════════════

        public async Task<ChildOverviewDto> GetChildOverviewAsync(string parentId, string childId, DateOnly today, CancellationToken ct = default)
        {
            var link = await _dbContext.ParentStudentLinks
                .Include(l => l.Student).ThenInclude(s => s.StudentProfile)
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId
                                       && l.StudentUserId == childId
                                       && l.Status == LinkStatus.Accepted, ct)
                ?? throw new ForbiddenException(["You do not have access to this student's data."]);

            var dto = new ChildOverviewDto
            {
                Child = new LinkedChildDto
                {
                    StudentId        = link.StudentUserId,
                    FullName         = $"{link.Student.FirstName} {link.Student.LastName}",
                    EducationalStage = link.Student.StudentProfile!.EducationalStage,
                    GradeYear        = link.Student.StudentProfile!.GradeYear
                }
            };

            // Attendance — reuse the existing student-facing summary. Sum across
            // every group the child is in (each entry already counts only sessions
            // the student has a record for, so the rate is correct).
            var attendance = await _attendanceService.GetMyAttendanceSummaryAsync(childId, ct);
            int totalCompleted = attendance.Sum(g => g.TotalCompleted);
            int present  = attendance.Sum(g => g.Present);
            int absent   = attendance.Sum(g => g.Absent);
            int excused  = attendance.Sum(g => g.Excused);

            // Excused is neutral — kept out of the rate denominator so that
            // a child off sick doesn't drag their percentage down. Same policy
            // as AttendanceService.GetMyAttendanceSummaryAsync.
            int counted = present + absent;

            dto.Present                = present;
            dto.Absent                 = absent;
            dto.Excused                = excused;
            dto.TotalCompletedSessions = totalCompleted;
            dto.AttendanceRate         = counted > 0
                ? (int)Math.Round((double)present / counted * 100)
                : 0;
            dto.AttendanceStreak       = await _attendanceService.GetMyAttendanceStreakAsync(childId, ct);

            // Grades — overall average across every exam the child has taken.
            var grades = await _gradeService.GetStudentGradesAsync(childId, ct);
            if (grades.Count > 0)
            {
                var avgPct = grades.Average(g => g.MaxScore > 0
                    ? (double)g.Score / (double)g.MaxScore * 100.0
                    : 0.0);
                dto.GradesAveragePercent = (int)Math.Round(avgPct);
            }
            dto.ExamsCount = grades.Count;

            // Payments — reuse the student "my payments" aggregation. Waived
            // records are already excluded from the remaining totals.
            var payments = await _paymentService.GetMyPaymentsAsync(childId, ct);
            dto.TotalRemaining         = payments.TotalRemaining;
            dto.TotalPaid              = payments.TotalPaid;
            dto.GroupsWithOutstanding  = payments.GroupsWithOutstanding;

            // Today's schedule — count + the next upcoming session today.
            var todaySchedule = await _studentService.GetMyTodayScheduleAsync(childId, today, ct);
            dto.TodaySessionsCount = todaySchedule.Count;
            if (todaySchedule.Count > 0)
            {
                var now = _dateTime.NowInAppZone.TimeOfDay;
                // Earliest session whose end is still in the future is "next".
                var next = todaySchedule
                    .Where(s => s.EndTime > now)
                    .OrderBy(s => s.StartTime)
                    .FirstOrDefault();
                if (next != null)
                {
                    int minutesUntil = (int)Math.Round((next.StartTime - now).TotalMinutes);
                    dto.NextSession = new ChildNextSessionDto
                    {
                        OccurrenceId     = next.OccurrenceId,
                        GroupName        = next.GroupName ?? string.Empty,
                        Subject          = next.Subject,
                        StartTime        = next.StartTime.ToString(@"hh\:mm\:ss"),
                        EndTime          = next.EndTime.ToString(@"hh\:mm\:ss"),
                        MinutesUntilStart = minutesUntil,
                    };
                }
            }

            return dto;
        }

        // ════════════════════════════════════════════════════════════════════
        // Announcements feed — across every group the child is in.
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<ChildAnnouncementDto>> GetChildAnnouncementsAsync(string parentId, string childId, CancellationToken ct = default)
        {
            var isLinked = await IsParentLinkedToChildAsync(parentId, childId, ct);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's announcements."]);

            // One query joining child enrollments → group announcements. Pull
            // tenant ids so we can resolve the teacher (tenant) display name.
            var rows = await (
                from gs in _dbContext.GroupStudents
                join a in _dbContext.GroupAnnouncements on gs.GroupId equals a.GroupId
                where gs.StudentId == childId
                orderby a.IsPinned descending, a.CreatedAt descending
                select new
                {
                    a.Id,
                    a.Message,
                    a.IsPinned,
                    a.CreatedAt,
                    a.GroupId,
                    GroupName = gs.Group.Name,
                    a.TenantId
                }
            ).ToListAsync(ct);

            if (rows.Count == 0) return [];

            var tenantIds = rows.Select(r => r.TenantId).Distinct().ToList();
            var tenants = await _tenantDbContext.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            return rows.Select(r => new ChildAnnouncementDto
            {
                Id          = r.Id,
                Message     = r.Message,
                IsPinned    = r.IsPinned,
                CreatedAt   = r.CreatedAt,
                GroupId     = r.GroupId,
                GroupName   = r.GroupName,
                TeacherName = tenants.TryGetValue(r.TenantId, out var name) ? name : null
            }).ToList();
        }

        // ════════════════════════════════════════════════════════════════════
        // Absences feed — recent missed sessions across every group, newest
        // first. Drives the parent's attendance-tab timeline.
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<ChildAbsenceDto>> GetChildAbsencesAsync(string parentId, string childId, int take = 30, CancellationToken ct = default)
        {
            var isLinked = await IsParentLinkedToChildAsync(parentId, childId, ct);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's attendance."]);

            // Clamp take so a hostile client can't ask for everything at once
            // and a forgetful caller can't ship a zero.
            take = Math.Clamp(take, 1, 100);

            // Pull attendance rows where the child was Absent or Excused. We
            // include the related occurrence + session + group so we can render
            // the time (which falls back from occurrence-level to session-level
            // for recurring sessions, same as the student-facing summary does).
            var rows = await _dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.StudentId == childId
                         && (a.Status == AttendanceStatus.Absent || a.Status == AttendanceStatus.Excused))
                .OrderByDescending(a => a.Occurrence!.OccurrenceDate)
                .ThenByDescending(a => a.MarkedAt)
                .Take(take)
                .Select(a => new
                {
                    a.OccurrenceId,
                    Date          = a.Occurrence!.OccurrenceDate,
                    OccStart      = a.Occurrence.StartTime,
                    OccEnd        = a.Occurrence.EndTime,
                    SessionStart  = a.Occurrence.Session != null ? a.Occurrence.Session.StartTime : (TimeSpan?)null,
                    SessionEnd    = a.Occurrence.Session != null ? a.Occurrence.Session.EndTime   : (TimeSpan?)null,
                    GroupId       = a.Occurrence.GroupId,
                    GroupName     = a.Occurrence.Group.Name,
                    Subject       = a.Occurrence.Group.Subject,
                    a.Status,
                    a.Notes
                })
                .ToListAsync(ct);

            return rows.Select(r => new ChildAbsenceDto
            {
                OccurrenceId   = r.OccurrenceId,
                OccurrenceDate = r.Date,
                StartTime      = (r.OccStart ?? r.SessionStart ?? TimeSpan.Zero).ToString(@"hh\:mm\:ss"),
                EndTime        = (r.OccEnd   ?? r.SessionEnd   ?? TimeSpan.Zero).ToString(@"hh\:mm\:ss"),
                GroupId        = r.GroupId,
                GroupName      = r.GroupName,
                Subject        = r.Subject,
                Status         = r.Status.ToString(),
                Notes          = r.Notes
            }).ToList();
        }
    }
}
