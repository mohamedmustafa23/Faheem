using Application.Exceptions;
using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _dbContext;

        public GroupService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> CreateGroupAsync(CreateGroupRequest request, string tenantId, CancellationToken ct = default)
        {
            string enrollmentCode = await GenerateUniqueCodeAsync(ct);

            var group = new Group
            {
                Name = request.Name,
                Subject = request.Subject,
                EducationalStage = request.EducationalStage,
                GradeYear = request.GradeYear,
                MaxStudents = request.MaxStudents,
                SessionsPerCycle = request.SessionsPerCycle,
                MonthlyFee = request.MonthlyFee,
                Description = request.Description,
                EnrollmentCode = enrollmentCode,
                Status = GroupStatus.Active,
                TenantId = tenantId
            };

            await _dbContext.Groups.AddAsync(group, ct);

            // Open first payment cycle if session count is configured
            if (request.SessionsPerCycle.HasValue && request.SessionsPerCycle.Value > 0)
            {
                await _dbContext.PaymentCycles.AddAsync(new PaymentCycle
                {
                    Group = group,
                    CycleNumber = 1,
                    SessionsTarget = request.SessionsPerCycle.Value,
                    BaseFee = request.MonthlyFee ?? 0,
                    TenantId = tenantId
                }, ct);
            }

            await _dbContext.SaveChangesAsync(ct);

            return enrollmentCode;
        }

        public async Task<string> UpdateGroupAsync(UpdateGroupRequest request, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantId, ct);

            if (group == null)
                throw new NotFoundException(["Group not found or it has been archived."]);

            group.Name = request.Name;
            group.Subject = request.Subject;
            group.EducationalStage = request.EducationalStage;
            group.GradeYear = request.GradeYear;
            group.MaxStudents = request.MaxStudents;
            group.SessionsPerCycle = request.SessionsPerCycle;
            group.MonthlyFee = request.MonthlyFee;
            group.Description = request.Description;

            await _dbContext.SaveChangesAsync(ct);

            // Sync the open payment cycle with the new sessions/fee values
            if (request.SessionsPerCycle.HasValue && request.SessionsPerCycle.Value > 0)
            {
                var openCycle = await _dbContext.PaymentCycles
                    .FirstOrDefaultAsync(c => c.GroupId == request.Id && !c.IsCompleted, ct);

                if (openCycle != null)
                {
                    // Update the open cycle's target to match the new setting
                    openCycle.SessionsTarget = request.SessionsPerCycle.Value;
                    await _dbContext.SaveChangesAsync(ct);
                }
                else
                {
                    // No open cycle — create first one if none exist at all
                    var hasAnyCycle = await _dbContext.PaymentCycles
                        .AnyAsync(c => c.GroupId == request.Id, ct);

                    if (!hasAnyCycle)
                    {
                        await _dbContext.PaymentCycles.AddAsync(new PaymentCycle
                        {
                            GroupId = request.Id,
                            CycleNumber = 1,
                            SessionsTarget = request.SessionsPerCycle.Value,
                            BaseFee = request.MonthlyFee ?? 0,
                            TenantId = tenantId
                        }, ct);
                        await _dbContext.SaveChangesAsync(ct);
                    }
                }
            }

            return "Group updated successfully.";
        }

        public async Task<List<GroupResponseDto>> GetTeacherGroupsAsync(string tenantId, CancellationToken ct = default)
        {
            var groups = await _dbContext.Groups
                .OrderByDescending(g => g.IsPinned)
                .ThenBy(g => g.Name)
                .Select(g => new GroupResponseDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Subject = g.Subject,
                    EducationalStage = g.EducationalStage,
                    GradeYear = g.GradeYear,
                    EnrollmentCode = g.EnrollmentCode,
                    MaxStudents = g.MaxStudents,
                    SessionsPerCycle = g.SessionsPerCycle,
                    MonthlyFee = g.MonthlyFee,
                    Status = g.Status.ToString(),
                    EnrolledStudentsCount = g.Students.Count(),
                    CurrentCycleSessionsCompleted = g.PaymentCycles
                        .Where(c => !c.IsCompleted)
                        .Select(c => (int?)c.SessionsCompleted)
                        .FirstOrDefault(),
                    IsPinned = g.IsPinned,
                })
                .ToListAsync(ct);

            return groups;
        }

        public async Task<string> TogglePinAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId, ct)
                ?? throw new NotFoundException(["Group not found."]);

            group.IsPinned = !group.IsPinned;
            await _dbContext.SaveChangesAsync(ct);
            return group.IsPinned ? "تم تثبيت المجموعة." : "تم إلغاء التثبيت.";
        }


        public async Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .Include(g => g.Sessions)
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group == null)
                throw new NotFoundException(["Group not found."]);

            var groupStudents = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == groupId)
                .ToListAsync(ct);

            var studentIds = groupStudents.Select(gs => gs.StudentId).ToList();

            var users = await _dbContext.Users
                .Include(u => u.StudentProfile)
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u, ct);

            // Which of these students already have an accepted parent link.
            var linkedSet = (await _dbContext.ParentStudentLinks
                .Where(l => studentIds.Contains(l.StudentUserId) && l.Status == Domain.Enums.LinkStatus.Accepted)
                .Select(l => l.StudentUserId)
                .Distinct()
                .ToListAsync(ct)).ToHashSet();

            var studentsList = groupStudents.Select(gs =>
            {
                users.TryGetValue(gs.StudentId, out var user);
                return new StudentDto
                {
                    StudentId = gs.StudentId,
                    FullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    FirstName = user?.FirstName ?? "",
                    LastName = user?.LastName ?? "",
                    PhoneNumber = user?.PhoneNumber ?? "",
                    EducationalStage = user?.StudentProfile?.EducationalStage ?? "",
                    GradeYear = user?.StudentProfile?.GradeYear ?? "",
                    IsGhostAccount = user?.IsGhostAccount ?? false,
                    StudentCode = user?.StudentCode,
                    IsLinkedToParent = linkedSet.Contains(gs.StudentId),
                    JoinedAt = gs.JoinedAt
                };
            }).ToList();

            var activeCycle = await _dbContext.PaymentCycles
                .Where(c => c.GroupId == groupId && !c.IsCompleted)
                .Select(c => (int?)c.SessionsCompleted)
                .FirstOrDefaultAsync(ct);

            return new GroupDetailsResponseDto
            {
                Id = group.Id,
                Name = group.Name,
                Subject = group.Subject,
                EducationalStage = group.EducationalStage,
                GradeYear = group.GradeYear,
                EnrollmentCode = group.EnrollmentCode,
                MaxStudents = group.MaxStudents,
                SessionsPerCycle = group.SessionsPerCycle,
                MonthlyFee = group.MonthlyFee,
                Description = group.Description,
                Status = group.Status.ToString(),
                CurrentCycleSessionsCompleted = activeCycle,
                Schedules = group.Sessions
                    .Where(s => s.IsActive)
                    .Select(s => new SessionDto
                    {
                        Id = s.Id,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        IsActive = s.IsActive
                    }).ToList(),
                Students = studentsList
            };
        }

        public async Task<string> DeleteGroupAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group == null)
                throw new NotFoundException(["Group not found."]);

            // SessionOccurrence has a direct FK to Group (GroupId) configured as NoAction
            // to avoid multiple cascade paths in SQL Server.
            // Standalone occurrences (SessionId = null) won't be reached by the
            // Session → Occurrence cascade, so we delete them manually first.
            var standaloneOccurrences = await _dbContext.SessionOccurrences
                .Where(o => o.GroupId == groupId && o.SessionId == null)
                .ToListAsync(ct);

            if (standaloneOccurrences.Count > 0)
            {
                _dbContext.SessionOccurrences.RemoveRange(standaloneOccurrences);
                await _dbContext.SaveChangesAsync(ct);
            }

            // DB CASCADE handles the rest:
            // Sessions → SessionOccurrences (via SessionId) → AttendanceRecords
            // GroupStudents, PaymentCycles → StudentPayments
            // Exams → StudentGrades, Materials, GroupAnnouncements
            _dbContext.Groups.Remove(group);
            await _dbContext.SaveChangesAsync(ct);

            return "Group deleted successfully.";
        }

        public async Task<string> RegenerateCodeAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && g.TenantId == tenantId, ct);

            if (group == null)
                throw new NotFoundException(["Group not found."]);

            string newCode = await GenerateUniqueCodeAsync(ct);
            group.EnrollmentCode = newCode;

            await _dbContext.SaveChangesAsync(ct);
            return newCode;
        }

        private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string code;
            bool isUnique;
            int attempts = 0;

            do
            {
                if (attempts >= 10)
                    throw new Exception("Failed to generate a unique group code after multiple attempts. Please try again.");

                code = System.Security.Cryptography.RandomNumberGenerator.GetString(chars, 6);
                isUnique = !await _dbContext.Groups.AnyAsync(g => g.EnrollmentCode == code, ct);
                attempts++;
            } while (!isUnique);

            return code;
        }
    }
}
