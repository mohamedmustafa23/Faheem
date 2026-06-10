using Application.Exceptions;
using Application.Features.Attendance.DTOs;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeService _dateTime;

        public AttendanceService(ApplicationDbContext dbContext, INotificationService notificationService, IDateTimeService dateTime)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _dateTime = dateTime;
        }

        public async Task<List<StudentAttendanceDto>> GetOccurrenceAttendanceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _dbContext.SessionOccurrences
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct);

            if (occurrence == null)
                throw new NotFoundException(["Session occurrence not found."]);

            var result = await (
                from gs in _dbContext.GroupStudents
                join u in _dbContext.Users on gs.StudentId equals u.Id
                join ar in _dbContext.AttendanceRecords
                    on new { gs.StudentId, OccurrenceId = occurrenceId } equals new { ar.StudentId, ar.OccurrenceId } into arGroup
                from ar in arGroup.DefaultIfEmpty()
                where gs.GroupId == occurrence.GroupId
                select new StudentAttendanceDto
                {
                    StudentId = gs.StudentId,
                    FullName = u.FirstName + " " + u.LastName,
                    Status = ar != null ? ar.Status : AttendanceStatus.Absent,
                    Notes = ar != null ? ar.Notes : null
                }
            ).OrderBy(x => x.FullName).ToListAsync(ct);

            return result;
        }

        public async Task<string> SaveAttendanceAsync(SaveAttendanceRequest request, string tenantId, CancellationToken ct = default)
        {
            (Guid groupId, List<string> newlyAbsent) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var occurrence = await _dbContext.SessionOccurrences
                    .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId, ct)
                    ?? throw new NotFoundException(["Session occurrence not found."]);

                if (occurrence.Status == SessionStatus.Completed)
                    throw new ConflictException(["Cannot modify attendance for a completed session."]);
                if (occurrence.Status == SessionStatus.Cancelled)
                    throw new ConflictException(["Cannot record attendance for a cancelled session."]);

                var existingRecords = await _dbContext.AttendanceRecords
                    .Where(a => a.OccurrenceId == request.OccurrenceId)
                    .ToDictionaryAsync(a => a.StudentId, a => a, ct);

                var newRecords = new List<AttendanceRecord>();
                var newlyAbsentLocal = new List<string>();

                foreach (var input in request.Records)
                {
                    if (existingRecords.TryGetValue(input.StudentId, out var existing))
                    {
                        existing.Status   = input.Status;
                        existing.Notes    = input.Notes;
                        existing.MarkedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        newRecords.Add(new AttendanceRecord
                        {
                            OccurrenceId   = request.OccurrenceId,
                            StudentId      = input.StudentId,
                            Status         = input.Status,
                            Notes          = input.Notes,
                            TenantId       = tenantId,
                            IsScannedViaQR = false
                        });

                        if (input.Status == AttendanceStatus.Absent || input.Status == AttendanceStatus.Excused)
                            newlyAbsentLocal.Add(input.StudentId);
                    }
                }

                if (newRecords.Any())
                    await _dbContext.AttendanceRecords.AddRangeAsync(newRecords, ct);

                await _dbContext.SaveChangesAsync(ct);

                return (occurrence.GroupId, newlyAbsentLocal);
            });

            // Notifications after DB commit (idempotent retries shouldn't double-notify)
            if (newlyAbsent.Any())
            {
                var group = await _dbContext.Groups.FindAsync([groupId], ct);
                string groupName = group?.Name ?? "المجموعة";

                foreach (var studentId in newlyAbsent)
                {
                    var recordInput = request.Records.First(r => r.StudentId == studentId);
                    bool isExcused = recordInput.Status == AttendanceStatus.Excused;
                    string statusWordStudent = isExcused ? "غياب بعذر" : "غياب";
                    string noteText = string.IsNullOrWhiteSpace(recordInput.Notes)
                        ? string.Empty
                        : $" (الملاحظة: {recordInput.Notes})";

                    // Student: short, addressed to them directly. Deep-link to
                    // the group so they can see the session in context.
                    var studentMsg = isExcused
                        ? $"تم تسجيل غيابك بعذر في حصة بمجموعة ({groupName}){noteText}."
                        : $"تم تسجيل غيابك في حصة بمجموعة ({groupName}). برجاء التواصل مع معلمك{noteText}.";

                    await _notificationService.SendStudentAndParentNotificationsAsync(
                        new List<string> { studentId },
                        new NotificationPayload(
                            title: isExcused ? "إشعار غياب بعذر" : "إشعار غياب",
                            message: studentMsg,
                            type: NotificationType.AbsenceAlert,
                            route: $"/student/groups/{groupId}"),
                        parentPayloadFactory: (sid, name) =>
                        {
                            // Parent: addressed about their child, deep-link
                            // into the child detail screen so one tap takes
                            // them straight to the attendance tab.
                            var parentMsg = isExcused
                                ? $"تم تسجيل {statusWordStudent} لـ {name} في حصة بمجموعة ({groupName}){noteText}."
                                : $"تم تسجيل غياب {name} في حصة بمجموعة ({groupName}){noteText}.";
                            return new NotificationPayload(
                                title: isExcused ? "غياب بعذر لابنك" : "غياب لابنك",
                                message: parentMsg,
                                type: NotificationType.AbsenceAlert,
                                route: $"/parent/children/{sid}");
                        },
                        tenantId,
                        ct);
                }
            }

            return "Attendance saved successfully.";
        }

        public async Task<string> EndOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var result = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var occurrence = await _dbContext.SessionOccurrences
                    .Include(o => o.Session)
                    .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                    ?? throw new NotFoundException(["Session occurrence not found."]);

                if (occurrence.Status == SessionStatus.Completed)
                    throw new ConflictException(["Session is already completed."]);
                if (occurrence.Status == SessionStatus.Cancelled)
                    throw new ConflictException(["Cannot end a cancelled session."]);

                var enrolledStudentIds = await _dbContext.GroupStudents
                    .Where(gs => gs.GroupId == occurrence.GroupId)
                    .Select(gs => gs.StudentId)
                    .ToListAsync(ct);

                var recordedStudentIds = await _dbContext.AttendanceRecords
                    .Where(a => a.OccurrenceId == occurrenceId)
                    .Select(a => a.StudentId)
                    .ToListAsync(ct);

                var absentIds = enrolledStudentIds.Except(recordedStudentIds).ToList();

                if (absentIds.Any())
                {
                    var autoAbsent = absentIds.Select(sid => new AttendanceRecord
                    {
                        OccurrenceId   = occurrenceId,
                        StudentId      = sid,
                        Status         = AttendanceStatus.Absent,
                        TenantId       = tenantId,
                        IsScannedViaQR = false,
                        MarkedAt       = DateTime.UtcNow,
                        Notes          = "Auto-marked absent at session end."
                    }).ToList();

                    await _dbContext.AttendanceRecords.AddRangeAsync(autoAbsent, ct);
                }

                occurrence.Status = SessionStatus.Completed;

                // Next occurrence for recurring schedules.
                // Advance in 7-day steps until we land on today or later — protects
                // the chain when an overdue (past-dated) occurrence is being ended late.
                if (occurrence.SessionId.HasValue && occurrence.Session!.IsActive)
                {
                    var todayDate = _dateTime.TodayInAppZone;
                    var nextDate  = occurrence.OccurrenceDate.AddDays(7);
                    while (nextDate < todayDate)
                        nextDate = nextDate.AddDays(7);

                    bool alreadyExists = await _dbContext.SessionOccurrences
                        .AnyAsync(o => o.SessionId == occurrence.SessionId && o.OccurrenceDate == nextDate, ct);

                    if (!alreadyExists)
                    {
                        _dbContext.SessionOccurrences.Add(new SessionOccurrence
                        {
                            SessionId        = occurrence.SessionId,
                            GroupId          = occurrence.GroupId,
                            OccurrenceDate   = nextDate,
                            CountsForPayment = true,
                            Status           = SessionStatus.Scheduled,
                            TenantId         = tenantId
                        });
                    }
                }

                // Cycle accumulation
                PaymentCycle? targetCycle = null;
                if (occurrence.CountsForPayment)
                {
                    targetCycle = occurrence.PaymentCycleId.HasValue
                        ? await _dbContext.PaymentCycles.FirstOrDefaultAsync(c => c.Id == occurrence.PaymentCycleId.Value, ct)
                        : await _dbContext.PaymentCycles.FirstOrDefaultAsync(c => c.GroupId == occurrence.GroupId && !c.IsCompleted, ct);
                }

                int cycleCompletedCount = 0;
                if (targetCycle != null)
                {
                    targetCycle.SessionsCompleted++;
                    cycleCompletedCount = targetCycle.SessionsCompleted;

                    if (!targetCycle.IsCompleted && targetCycle.SessionsCompleted >= targetCycle.SessionsTarget)
                    {
                        targetCycle.IsCompleted = true;
                        targetCycle.ClosedAt    = DateTime.UtcNow;

                        var group = await _dbContext.Groups.FindAsync([occurrence.GroupId], ct);
                        int nextTarget      = group?.SessionsPerCycle ?? targetCycle.SessionsTarget;
                        decimal nextBaseFee = group?.MonthlyFee ?? 0;

                        int nextNumber = (await _dbContext.PaymentCycles
                            .Where(c => c.GroupId == occurrence.GroupId)
                            .MaxAsync(c => (int?)c.CycleNumber, ct) ?? 0) + 1;

                        var newCycle = new PaymentCycle
                        {
                            GroupId        = occurrence.GroupId,
                            CycleNumber    = nextNumber,
                            SessionsTarget = nextTarget,
                            BaseFee        = nextBaseFee,
                            TenantId       = tenantId
                        };
                        _dbContext.PaymentCycles.Add(newCycle);

                        var currentStudentIds = await _dbContext.GroupStudents
                            .Where(gs => gs.GroupId == occurrence.GroupId)
                            .Select(gs => gs.StudentId)
                            .ToListAsync(ct);

                        foreach (var sid in currentStudentIds)
                        {
                            _dbContext.StudentPaymentRecords.Add(new StudentPaymentRecord
                            {
                                StudentId         = sid,
                                GroupId           = occurrence.GroupId,
                                PaymentCycle      = newCycle,
                                EnrolledAtSession = 0,
                                ExpectedAmount    = nextBaseFee,
                                Status            = PaymentStatus.Unpaid,
                                TenantId          = tenantId
                            });
                        }

                        await _dbContext.SaveChangesAsync(ct);
                        return (occurrence.GroupId, absentIds, enrolledStudentIds, CycleCompletedSessions: (int?)cycleCompletedCount);
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                return (occurrence.GroupId, absentIds, enrolledStudentIds, CycleCompletedSessions: (int?)null);
            });

            // Notifications after DB commit
            var group2 = await _dbContext.Groups.FindAsync([result.GroupId], ct);

            if (result.absentIds.Any())
            {
                string groupName2 = group2?.Name ?? "المجموعة";
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    result.absentIds,
                    new NotificationPayload(
                        title: "إشعار غياب",
                        message: $"تم تسجيل غيابك في حصة بمجموعة ({groupName2}). برجاء التواصل مع معلمك.",
                        type: NotificationType.AbsenceAlert,
                        route: $"/student/groups/{result.GroupId}"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "غياب لابنك",
                        message: $"تم تسجيل غياب {name} في حصة بمجموعة ({groupName2}).",
                        type: NotificationType.AbsenceAlert,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            if (result.CycleCompletedSessions.HasValue)
            {
                string groupName2 = group2?.Name ?? "المجموعة";
                int cycleCount = result.CycleCompletedSessions.Value;
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    result.enrolledStudentIds,
                    new NotificationPayload(
                        title: "موعد سداد جديد",
                        message: $"تم إتمام {cycleCount} حصة في مجموعة ({groupName2})، حان موعد سداد دورة الدفع الجديدة.",
                        type: NotificationType.PaymentDue,
                        route: "/student/finance"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "موعد سداد ابنك",
                        message: $"أتم {name} {cycleCount} حصة في مجموعة ({groupName2})، حان موعد سداد دورة الدفع الجديدة.",
                        type: NotificationType.PaymentDue,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Session ended. Absent students auto-marked and next occurrence scheduled.";
        }

        public async Task<string> GenerateQrTokenAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _dbContext.SessionOccurrences
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct);

            if (occurrence == null)
                throw new NotFoundException(["Session occurrence not found."]);

            if (occurrence.Status != SessionStatus.Scheduled)
                throw new ConflictException(["Cannot generate QR for a completed or cancelled session."]);

            occurrence.QrToken = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync(ct);

            return occurrence.QrToken;
        }

        public async Task<List<StudentAttendanceSummaryDto>> GetGroupAttendanceSummaryAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var students = await (
                from gs in _dbContext.GroupStudents
                join u in _dbContext.Users on gs.StudentId equals u.Id
                where gs.GroupId == groupId
                select new { gs.StudentId, FullName = u.FirstName + " " + u.LastName }
            ).ToListAsync(ct);

            var completedOccurrenceIds = await _dbContext.SessionOccurrences
                .Where(o => o.GroupId == groupId && o.Status == SessionStatus.Completed)
                .Select(o => o.Id)
                .ToListAsync(ct);

            var records = await _dbContext.AttendanceRecords
                .Where(a => completedOccurrenceIds.Contains(a.OccurrenceId))
                .ToListAsync(ct);

            return students.Select(s =>
            {
                // Per-student counts: only count sessions this student actually
                // has a record for. A student who joined mid-cycle shouldn't be
                // penalised for sessions that happened before they enrolled.
                var sr = records.Where(r => r.StudentId == s.StudentId).ToList();
                int present = sr.Count(r => r.Status == AttendanceStatus.Present);
                int absent  = sr.Count(r => r.Status == AttendanceStatus.Absent);
                int excused = sr.Count(r => r.Status == AttendanceStatus.Excused);

                // Rate denominator excludes Excused — an excused absence is
                // neutral, neither for nor against the student. Same policy as
                // the student-facing and parent-facing summaries so all three
                // surfaces agree to the decimal.
                int counted = present + absent;
                return new StudentAttendanceSummaryDto
                {
                    StudentId      = s.StudentId,
                    FullName       = s.FullName,
                    TotalCompleted = present + absent + excused,
                    Present        = present,
                    Absent         = absent,
                    Excused        = excused,
                    AttendanceRate = counted > 0 ? Math.Round((double)present / counted * 100, 1) : 0
                };
            }).OrderBy(s => s.FullName).ToList();
        }

        public async Task<PaginatedResult<GroupOccurrenceDto>> GetGroupOccurrencesAsync(Guid groupId, int page, int pageSize, string tenantId, CancellationToken ct = default)
        {
            var query = _dbContext.SessionOccurrences
                .Include(o => o.Session)
                .Where(o => o.GroupId == groupId)
                .OrderByDescending(o => o.OccurrenceDate);

            int totalCount = await query.CountAsync(ct);

            var occurrences = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var occurrenceIds = occurrences.Select(o => o.Id).ToList();

            var attendanceCounts = await _dbContext.AttendanceRecords
                .Where(a => occurrenceIds.Contains(a.OccurrenceId))
                .GroupBy(a => new { a.OccurrenceId, a.Status })
                .Select(g => new { g.Key.OccurrenceId, g.Key.Status, Count = g.Count() })
                .ToListAsync(ct);

            var data = occurrences.Select(o =>
            {
                var counts = attendanceCounts.Where(a => a.OccurrenceId == o.Id).ToList();
                return new GroupOccurrenceDto
                {
                    OccurrenceId     = o.Id,
                    OccurrenceDate   = o.OccurrenceDate,
                    StartTime        = o.StartTime ?? o.Session!.StartTime,
                    EndTime          = o.EndTime   ?? o.Session!.EndTime,
                    Status           = o.Status.ToString(),
                    IsManual         = !o.SessionId.HasValue,
                    PaymentMode      = o.PaymentMode,
                    SessionPrice     = o.SessionPrice,
                    CountsForPayment = o.CountsForPayment,
                    PresentCount     = counts.FirstOrDefault(c => c.Status == AttendanceStatus.Present)?.Count ?? 0,
                    AbsentCount      = counts.FirstOrDefault(c => c.Status == AttendanceStatus.Absent)?.Count  ?? 0,
                    ExcusedCount     = counts.FirstOrDefault(c => c.Status == AttendanceStatus.Excused)?.Count ?? 0
                };
            }).ToList();

            return PaginatedResult<GroupOccurrenceDto>.Create(data, totalCount, page, pageSize);
        }

        public async Task<List<MyGroupAttendanceDto>> GetMyAttendanceSummaryAsync(string studentId, CancellationToken ct = default)
        {
            var myGroups = await _dbContext.GroupStudents
                .Include(gs => gs.Group)
                .Where(gs => gs.StudentId == studentId)
                .ToListAsync(ct);

            var groupIds = myGroups.Select(gs => gs.GroupId).ToList();

            // Only sessions the student actually has an AttendanceRecord for count
            // toward "total". A record is created either when the teacher saves
            // attendance OR when the teacher ends the session (auto-absent loop
            // covers enrolled students at that time). Sessions that happened
            // before the student joined have no record for them, so they're
            // correctly excluded from the denominator.
            var records = await _dbContext.AttendanceRecords
                .Where(a => a.StudentId == studentId && groupIds.Contains(a.Occurrence!.GroupId)
                         && a.Occurrence.Status == SessionStatus.Completed)
                .Select(a => new { a.Status, GroupId = a.Occurrence!.GroupId })
                .ToListAsync(ct);

            return myGroups.Select(gs =>
            {
                var gr = records.Where(r => r.GroupId == gs.GroupId).ToList();
                int present = gr.Count(r => r.Status == AttendanceStatus.Present);
                int absent  = gr.Count(r => r.Status == AttendanceStatus.Absent);
                int excused = gr.Count(r => r.Status == AttendanceStatus.Excused);
                int total   = gr.Count;
                // Rate denominator excludes Excused — an excused absence is
                // neutral, neither for nor against the student. Foundational
                // for the upcoming "top students" evaluation feature.
                int counted = present + absent;
                return new MyGroupAttendanceDto
                {
                    GroupId        = gs.GroupId,
                    GroupName      = gs.Group.Name,
                    Subject        = gs.Group.Subject,
                    TotalCompleted = total,
                    Present        = present,
                    Absent         = absent,
                    Excused        = excused,
                    AttendanceRate = counted > 0 ? Math.Round((double)present / counted * 100, 1) : 0
                };
            }).OrderBy(g => g.GroupName).ToList();
        }

        public async Task<int> GetMyAttendanceStreakAsync(string studentId, CancellationToken ct = default)
        {
            // Walk the student's records backwards through time across every
            // group. Counts only Present entries; Excused is neutral (skipped
            // — doesn't break or extend); the first true Absent stops the count.
            // We pull only the columns we need to keep the query light.
            var records = await _dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.StudentId == studentId
                         && a.Occurrence!.Status == SessionStatus.Completed)
                .OrderByDescending(a => a.Occurrence!.OccurrenceDate)
                .ThenByDescending(a => a.MarkedAt)
                .Select(a => a.Status)
                .ToListAsync(ct);

            int streak = 0;
            foreach (var status in records)
            {
                if (status == AttendanceStatus.Absent) break;
                if (status == AttendanceStatus.Present) streak++;
                // Excused → skip silently (neither for nor against)
            }
            return streak;
        }

        public async Task<MyGroupAttendanceDto> GetMyGroupAttendanceDetailAsync(string studentId, Guid groupId, CancellationToken ct = default)
        {
            var gs = await _dbContext.GroupStudents
                .Include(g => g.Group)
                .FirstOrDefaultAsync(g => g.StudentId == studentId && g.GroupId == groupId, ct);

            if (gs == null)
                throw new ForbiddenException(["You are not enrolled in this group."]);

            // Only completed occurrences the student has a record on. Sessions
            // before they enrolled would inflate the denominator and show up in
            // history as bogus "absent" rows, so we filter them out at source.
            var occurrences = await (
                from o in _dbContext.SessionOccurrences.Include(o => o.Session)
                join a in _dbContext.AttendanceRecords on o.Id equals a.OccurrenceId
                where o.GroupId == groupId
                   && o.Status == SessionStatus.Completed
                   && a.StudentId == studentId
                orderby o.OccurrenceDate descending
                select o
            ).ToListAsync(ct);

            var occurrenceIds = occurrences.Select(o => o.Id).ToList();

            var records = await _dbContext.AttendanceRecords
                .Where(a => occurrenceIds.Contains(a.OccurrenceId) && a.StudentId == studentId)
                .ToDictionaryAsync(a => a.OccurrenceId, ct);

            int total   = records.Count;
            int present = records.Values.Count(r => r.Status == AttendanceStatus.Present);
            int absent  = records.Values.Count(r => r.Status == AttendanceStatus.Absent);
            int excused = records.Values.Count(r => r.Status == AttendanceStatus.Excused);

            var history = occurrences.Select(o =>
            {
                records.TryGetValue(o.Id, out var record);
                return new AttendanceEntryDto
                {
                    OccurrenceId     = o.Id,
                    OccurrenceDate   = o.OccurrenceDate,
                    StartTime        = o.StartTime ?? o.Session!.StartTime,
                    AttendanceStatus = record?.Status.ToString() ?? AttendanceStatus.Absent.ToString(),
                    Notes            = record?.Notes,
                    ScannedViaQR     = record?.IsScannedViaQR ?? false
                };
            }).ToList();

            int counted = present + absent; // Excused excluded from rate
            return new MyGroupAttendanceDto
            {
                GroupId        = gs.GroupId,
                GroupName      = gs.Group.Name,
                Subject        = gs.Group.Subject,
                TotalCompleted = total,
                Present        = present,
                Absent         = absent,
                Excused        = excused,
                AttendanceRate = counted > 0 ? Math.Round((double)present / counted * 100, 1) : 0,
                History        = history
            };
        }

        public async Task<string> ScanQrCodeAsync(ScanQrRequest request, string studentId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var occurrence = await _dbContext.SessionOccurrences
                    .Include(o => o.Session)
                    .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId, ct);

                if (occurrence == null || occurrence.Status != SessionStatus.Scheduled)
                    throw new ConflictException(["This session is not currently active."]);

                if (string.IsNullOrEmpty(occurrence.QrToken) || occurrence.QrToken != request.QrToken)
                    throw new ConflictException(["Invalid or expired QR code. Please scan the current code shown by your teacher."]);

                // Validate date and time window in the app's business zone (Egypt).
                var nowLocal   = _dateTime.NowInAppZone;
                var todayLocal = _dateTime.TodayInAppZone;

                if (occurrence.OccurrenceDate != todayLocal)
                    throw new ConflictException(["This session is not scheduled for today."]);

                var sessionStart = occurrence.StartTime ?? occurrence.Session!.StartTime;
                var sessionEnd   = occurrence.EndTime   ?? occurrence.Session!.EndTime;

                var allowedStart = sessionStart.Subtract(TimeSpan.FromMinutes(30));
                var currentTime  = nowLocal.TimeOfDay;

                if (currentTime < allowedStart)
                    throw new ConflictException(["It is too early to scan the QR code for this session."]);

                if (currentTime > sessionEnd)
                    throw new ConflictException(["This session has already ended."]);

                var isEnrolled = await _dbContext.GroupStudents
                    .AnyAsync(gs => gs.GroupId == occurrence.GroupId && gs.StudentId == studentId, ct);

                if (!isEnrolled)
                    throw new ForbiddenException(["You are not enrolled in this group."]);

                var record = await _dbContext.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.OccurrenceId == request.OccurrenceId && a.StudentId == studentId, ct);

                if (record != null)
                {
                    if (record.Status == AttendanceStatus.Present)
                        return "You are already marked as present.";

                    record.Status         = AttendanceStatus.Present;
                    record.IsScannedViaQR = true;
                    record.MarkedAt       = DateTime.UtcNow;
                }
                else
                {
                    _dbContext.AttendanceRecords.Add(new AttendanceRecord
                    {
                        OccurrenceId   = request.OccurrenceId,
                        StudentId      = studentId,
                        Status         = AttendanceStatus.Present,
                        TenantId       = occurrence.TenantId,
                        IsScannedViaQR = true,
                        MarkedAt       = DateTime.UtcNow
                    });
                }

                await _dbContext.SaveChangesAsync(ct);

                return "Attendance recorded successfully.";
            });
        }
    }
}
