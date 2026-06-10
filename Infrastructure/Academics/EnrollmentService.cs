using Application.Exceptions;
using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public EnrollmentService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<string> JoinGroupAsync(string studentId, string enrollmentCode, CancellationToken ct = default)
        {
            var code = enrollmentCode.ToUpper();

            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.EnrollmentCode == code, ct);

            if (group == null)
                throw new NotFoundException(["Invalid enrollment code or the group no longer exists."]);

            var alreadyEnrolled = await _dbContext.GroupStudents
                .AnyAsync(gs => gs.GroupId == group.Id && gs.StudentId == studentId, ct);

            if (alreadyEnrolled)
                throw new ConflictException(["You are already enrolled in this group."]);

            if (group.MaxStudents.HasValue) 
            {
                var currentStudentCount = await _dbContext.GroupStudents
                    .CountAsync(gs => gs.GroupId == group.Id, ct);

                if (currentStudentCount >= group.MaxStudents.Value)
                {
                    throw new ConflictException(["This group has reached its maximum capacity and cannot accept new students."]);
                }
            }

            var enrollment = new GroupStudent
            {
                GroupId = group.Id,
                StudentId = studentId,
                TenantId = group.TenantId
            };

            await _dbContext.GroupStudents.AddAsync(enrollment, ct);

            // Create a payment record for the current open cycle (if any)
            var openCycle = await _dbContext.PaymentCycles
                .FirstOrDefaultAsync(c => c.GroupId == group.Id && !c.IsCompleted, ct);

            if (openCycle != null)
            {
                // A returning student may still hold a preserved record for this
                // cycle — CleanupOpenPaymentRecordsAsync keeps records that carry
                // payment history when they leave. Re-inserting would violate the
                // unique (PaymentCycleId, StudentId) index and surface as a 500,
                // so only add a record when none already exists.
                var cycleRecordExists = await _dbContext.StudentPaymentRecords
                    .AnyAsync(r => r.PaymentCycleId == openCycle.Id && r.StudentId == studentId, ct);

                if (!cycleRecordExists)
                {
                    await _dbContext.StudentPaymentRecords.AddAsync(new Domain.Entities.StudentPaymentRecord
                    {
                        StudentId = studentId,
                        GroupId = group.Id,
                        PaymentCycleId = openCycle.Id,
                        EnrolledAtSession = openCycle.SessionsCompleted,
                        ExpectedAmount = openCycle.BaseFee + openCycle.ExtraFee,
                        Status = Domain.Enums.PaymentStatus.Unpaid,
                        TenantId = group.TenantId
                    }, ct);
                }
            }

            // Also seed records for any upcoming Standalone occurrences the student
            // should also have a record for.
            await SeedPendingStandaloneRecordsAsync(group.Id, studentId, group.TenantId, ct);

            await _dbContext.SaveChangesAsync(ct);

            // ── Notify the teacher (and any assistants on the same tenant) ──
            var student = await _userManager.FindByIdAsync(studentId);
            string studentName = student != null ? $"{student.FirstName} {student.LastName}" : "طالب جديد";
            string studentPhone = student?.PhoneNumber ?? string.Empty;

            await NotifyTeachersAsync(
                group.TenantId,
                title: "طالب جديد انضم",
                message: $"انضم {studentName} لمجموعة ({group.Name}){(string.IsNullOrEmpty(studentPhone) ? "" : $" — {studentPhone}")}.",
                NotificationType.StudentJoined,
                ct);

            return $"Successfully joined '{group.Name}'";
        }

        /// <summary>
        /// Notifies the teacher (and any assistants) that own the given tenant.
        /// Resolves teacher userIds via their Tenant claim.
        /// </summary>
        private async Task NotifyTeachersAsync(
            string tenantId, string title, string message, NotificationType type, CancellationToken ct)
        {
            var teacherUserIds = await _dbContext.UserClaims
                .Where(uc => uc.ClaimType == ClaimConstants.Tenant && uc.ClaimValue == tenantId)
                .Select(uc => uc.UserId)
                .Distinct()
                .ToListAsync(ct);

            if (teacherUserIds.Count == 0) return;

            await _notificationService.SendToUsersAsync(
                teacherUserIds, title, message, type, tenantId, ct: ct);
        }

        public async Task<string> RemoveStudentAsync(Guid groupId, string studentId, string tenantId, CancellationToken ct = default)
        {
            var enrollment = await _dbContext.GroupStudents
                .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId, ct);

            if (enrollment == null)
                throw new NotFoundException(["Student is not enrolled in this group."]);

            _dbContext.GroupStudents.Remove(enrollment);
            await CleanupOpenPaymentRecordsAsync(groupId, studentId, ct);
            await _dbContext.SaveChangesAsync(ct);

            return "Student removed successfully from the group.";
        }

        public async Task<string> LeaveGroupAsync(Guid groupId, string studentId, CancellationToken ct = default)
        {
            var enrollment = await _dbContext.GroupStudents
                .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId, ct);

            if (enrollment == null)
                throw new NotFoundException(["You are not enrolled in this group."]);

            _dbContext.GroupStudents.Remove(enrollment);
            await CleanupOpenPaymentRecordsAsync(groupId, studentId, ct);
            await _dbContext.SaveChangesAsync(ct);

            return "You have successfully left the group.";
        }

        /// <summary>
        /// Removes only fully-unpaid records (no transactions) when a student leaves.
        /// Records with any payment history (PartiallyPaid, Paid, or even Unpaid+Waived)
        /// are preserved for audit. Standalone-occurrence records follow the same rule.
        /// </summary>
        private async Task CleanupOpenPaymentRecordsAsync(Guid groupId, string studentId, CancellationToken ct)
        {
            var deletableRecords = await _dbContext.StudentPaymentRecords
                .Where(r => r.GroupId == groupId
                         && r.StudentId == studentId
                         && r.Status == PaymentStatus.Unpaid
                         && !r.Transactions.Any())
                .ToListAsync(ct);

            if (deletableRecords.Count > 0)
                _dbContext.StudentPaymentRecords.RemoveRange(deletableRecords);
        }

        // Manually add a young student (no phone). Creates a ghost account whose
        // identity + claim key is a unique StudentCode, optionally links a parent,
        // and returns the code for the teacher to hand to the student / parent.
        public async Task<string> ManualAddStudentAsync(ManualAddStudentRequest request, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
            if (group == null) throw new NotFoundException(["Group not found."]);

            var parentUser = await ResolveParentAsync(request.ParentPhoneNumber, ct);

            var studentCode = await GenerateUniqueStudentCodeAsync(ct);

            var studentUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = $"ghost_{studentCode}",
                Email = $"{studentCode}@faheem.local",
                UserType = UserType.Student,
                IsActive = true,
                EmailConfirmed = false,
                IsGhostAccount = true,
                StudentCode = studentCode,
                StudentProfile = new StudentProfile
                {
                    EducationalStage = request.EducationalStage,
                    GradeYear = request.GradeYear
                }
            };

            var result = await _userManager.CreateAsync(studentUser, GenerateRandomGhostPassword());
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());
            await _userManager.AddToRoleAsync(studentUser, RoleConstants.Student);

            await EnrollStudentInGroupAsync(studentUser, request.GroupId, tenantId, parentUser, ct);

            return studentCode;
        }

        // Add an existing manually-added (ghost) student to another group via their code.
        public async Task<string> AddStudentByCodeAsync(Guid groupId, string studentCode, string tenantId, CancellationToken ct = default)
        {
            var code = (studentCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code))
                throw new ConflictException(["Enter the student's code."]);

            var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
            if (group == null) throw new NotFoundException(["Group not found."]);

            var studentUser = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentCode == code, ct);
            if (studentUser == null)
                throw new NotFoundException(["No student found with this code."]);

            if (!studentUser.IsGhostAccount)
                throw new ForbiddenException(["This student has an active account — give them the group code to join themselves."]);

            await EnrollStudentInGroupAsync(studentUser, groupId, tenantId, null, ct);
            return "Student added successfully.";
        }

        // Edit a manually-added (ghost) student's roster info and, optionally, link a
        // parent later (e.g. the parent created their account after the student was added).
        public async Task<string> EditGhostStudentAsync(Guid groupId, string studentId, EditStudentRequest request, string tenantId, CancellationToken ct = default)
        {
            var enrolled = await _dbContext.GroupStudents
                .AnyAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId, ct);
            if (!enrolled) throw new NotFoundException(["Student is not enrolled in this group."]);

            var studentUser = await _userManager.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == studentId, ct);
            if (studentUser == null) throw new NotFoundException(["Student not found."]);

            if (!studentUser.IsGhostAccount)
                throw new ForbiddenException(["This student has an active account and manages their own details."]);

            if (!string.IsNullOrWhiteSpace(request.FirstName)) studentUser.FirstName = request.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(request.LastName)) studentUser.LastName = request.LastName.Trim();

            if (studentUser.StudentProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(request.EducationalStage)) studentUser.StudentProfile.EducationalStage = request.EducationalStage;
                if (!string.IsNullOrWhiteSpace(request.GradeYear)) studentUser.StudentProfile.GradeYear = request.GradeYear;
            }

            var parentUser = await ResolveParentAsync(request.ParentPhoneNumber, ct);
            if (parentUser != null)
            {
                var linkExists = await _dbContext.ParentStudentLinks
                    .AnyAsync(l => l.ParentUserId == parentUser.Id && l.StudentUserId == studentUser.Id, ct);
                if (!linkExists)
                {
                    _dbContext.ParentStudentLinks.Add(new ParentStudentLink
                    {
                        ParentUserId = parentUser.Id,
                        StudentUserId = studentUser.Id,
                        Status = Domain.Enums.LinkStatus.Accepted,
                        RequestedAt = DateTime.UtcNow,
                        ConfirmedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            return "Student updated successfully.";
        }

        // Resolves a parent account by phone; throws if a phone was given but no
        // parent account exists. Returns null when no phone was supplied.
        private async Task<ApplicationUser?> ResolveParentAsync(string? parentPhone, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(parentPhone)) return null;

            var parent = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == parentPhone && u.UserType == UserType.Parent, ct);
            if (parent == null)
                throw new ConflictException([
                    "No parent account exists with this phone number. Ask the parent to register first, then link."
                ]);
            return parent;
        }

        // Shared enrolment: membership + open-cycle payment record + standalone seeds
        // + optional parent link. Used by manual add and add-by-code.
        private async Task EnrollStudentInGroupAsync(ApplicationUser studentUser, Guid groupId, string tenantId, ApplicationUser? parentUser, CancellationToken ct)
        {
            var alreadyEnrolled = await _dbContext.GroupStudents
                .AnyAsync(gs => gs.GroupId == groupId && gs.StudentId == studentUser.Id, ct);
            if (alreadyEnrolled) throw new ConflictException(["This student is already enrolled in this group."]);

            _dbContext.GroupStudents.Add(new GroupStudent
            {
                GroupId = groupId,
                StudentId = studentUser.Id,
                TenantId = tenantId
            });

            var openCycle = await _dbContext.PaymentCycles
                .FirstOrDefaultAsync(c => c.GroupId == groupId && !c.IsCompleted, ct);

            if (openCycle != null)
            {
                var cycleRecordExists = await _dbContext.StudentPaymentRecords
                    .AnyAsync(r => r.PaymentCycleId == openCycle.Id && r.StudentId == studentUser.Id, ct);

                if (!cycleRecordExists)
                {
                    _dbContext.StudentPaymentRecords.Add(new Domain.Entities.StudentPaymentRecord
                    {
                        StudentId = studentUser.Id,
                        GroupId = groupId,
                        PaymentCycleId = openCycle.Id,
                        EnrolledAtSession = openCycle.SessionsCompleted,
                        ExpectedAmount = openCycle.BaseFee + openCycle.ExtraFee,
                        Status = Domain.Enums.PaymentStatus.Unpaid,
                        TenantId = tenantId
                    });
                }
            }

            await SeedPendingStandaloneRecordsAsync(groupId, studentUser.Id, tenantId, ct);

            if (parentUser != null)
            {
                var linkExists = await _dbContext.ParentStudentLinks
                    .AnyAsync(l => l.ParentUserId == parentUser.Id && l.StudentUserId == studentUser.Id, ct);
                if (!linkExists)
                {
                    _dbContext.ParentStudentLinks.Add(new ParentStudentLink
                    {
                        ParentUserId = parentUser.Id,
                        StudentUserId = studentUser.Id,
                        Status = Domain.Enums.LinkStatus.Accepted,
                        RequestedAt = DateTime.UtcNow,
                        ConfirmedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        private async Task<string> GenerateUniqueStudentCodeAsync(CancellationToken ct)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // skip ambiguous 0/O/1/I
            string code;
            int attempts = 0;
            do
            {
                if (attempts++ >= 10)
                    throw new Exception("Failed to generate a unique student code.");
                code = System.Security.Cryptography.RandomNumberGenerator.GetString(chars, 6);
            } while (await _userManager.Users.AnyAsync(u => u.StudentCode == code, ct));
            return code;
        }

        /// <summary>
        /// When a new student joins, automatically create payment records for any
        /// Standalone manual occurrences that are still Scheduled (i.e. haven't happened yet).
        /// AddToCycle and Free occurrences are excluded — Free has no cost, and AddToCycle
        /// already inflates the cycle's ExpectedAmount which the cycle record above captures.
        /// </summary>
        private async Task SeedPendingStandaloneRecordsAsync(
            Guid groupId, string studentId, string tenantId, CancellationToken ct)
        {
            var pendingStandalone = await _dbContext.SessionOccurrences
                .Where(o => o.GroupId == groupId
                         && o.SessionId == null
                         && o.PaymentMode == SessionPaymentMode.Standalone
                         && o.Status == SessionStatus.Scheduled
                         && o.SessionPrice != null)
                .Select(o => new { o.Id, o.SessionPrice })
                .ToListAsync(ct);

            if (pendingStandalone.Count == 0) return;

            // Skip occurrences the student already has a record for. A returning
            // (or re-added) student can retain preserved standalone records, and
            // re-inserting would violate the unique (OccurrenceId, StudentId)
            // index — the same duplicate-key crash as the cycle record above.
            var pendingIds = pendingStandalone.Select(o => o.Id).ToList();
            var existingOccurrenceIds = (await _dbContext.StudentPaymentRecords
                .Where(r => r.StudentId == studentId
                         && r.OccurrenceId != null
                         && pendingIds.Contains(r.OccurrenceId.Value))
                .Select(r => r.OccurrenceId!.Value)
                .ToListAsync(ct))
                .ToHashSet();

            foreach (var occ in pendingStandalone)
            {
                if (existingOccurrenceIds.Contains(occ.Id)) continue;
                _dbContext.StudentPaymentRecords.Add(new StudentPaymentRecord
                {
                    StudentId      = studentId,
                    GroupId        = groupId,
                    OccurrenceId   = occ.Id,
                    ExpectedAmount = occ.SessionPrice!.Value,
                    Status         = PaymentStatus.Unpaid,
                    TenantId       = tenantId,
                });
            }
        }

        private string GenerateRandomGhostPassword()
        {
            return Guid.NewGuid().ToString("N") + "F@h33m1!";
        }
    }
}