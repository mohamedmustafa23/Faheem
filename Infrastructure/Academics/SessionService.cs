using Application.Exceptions;
using Application.Features.Notifications.DTOs;
using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Academics
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly ISessionPaymentLinker _paymentLinker;
        private readonly IDateTimeService _dateTime;

        public SessionService(
            ApplicationDbContext dbContext,
            INotificationService notificationService,
            ISessionPaymentLinker paymentLinker,
            IDateTimeService dateTime)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _paymentLinker = paymentLinker;
            _dateTime = dateTime;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Recurring schedules
        // ════════════════════════════════════════════════════════════════════

        public async Task<string> CreateSchedulesAsync(CreateSessionRequest request, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
                ?? throw new NotFoundException(["Group not found."]);

            if (!request.TimeSlots.Any())
                throw new FluentValidation.ValidationException("You must provide at least one time slot.");

            // ── Internal conflict check (slots inside this request against each other) ──
            var slots = request.TimeSlots.ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].StartTime >= slots[i].EndTime)
                    throw new FluentValidation.ValidationException($"Start time must be before end time for {slots[i].DayOfWeek}.");

                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[i].DayOfWeek == slots[j].DayOfWeek &&
                        slots[i].StartTime < slots[j].EndTime &&
                        slots[i].EndTime   > slots[j].StartTime)
                    {
                        throw new ConflictException([
                            $"Two slots in this request overlap on {slots[i].DayOfWeek}."
                        ]);
                    }
                }
            }

            var requestedDays = slots.Select(t => t.DayOfWeek).Distinct().ToList();

            // ── External conflict check (existing recurring schedules) ──
            var existingSchedules = await _dbContext.Sessions
                .Where(s => s.IsActive && requestedDays.Contains(s.DayOfWeek))
                .ToListAsync(ct);

            var today = _dateTime.TodayInAppZone;
            var newSchedules = new List<Session>();
            var newOccurrences = new List<SessionOccurrence>();

            foreach (var slot in slots)
            {
                var overlapping = existingSchedules.FirstOrDefault(s =>
                    s.DayOfWeek == slot.DayOfWeek &&
                    slot.StartTime < s.EndTime &&
                    slot.EndTime   > s.StartTime);

                if (overlapping != null)
                {
                    var conflictGroup = await _dbContext.Groups.FindAsync([overlapping.GroupId], ct);
                    throw new ConflictException([
                        $"Time conflict on {slot.DayOfWeek}: group [{conflictGroup?.Name}] already has a schedule from [{overlapping.StartTime:hh\\:mm}] to [{overlapping.EndTime:hh\\:mm}]."
                    ]);
                }

                var schedule = new Session
                {
                    GroupId   = request.GroupId,
                    DayOfWeek = slot.DayOfWeek,
                    StartTime = slot.StartTime,
                    EndTime   = slot.EndTime,
                    IsActive  = true,
                    TenantId  = tenantId
                };

                newSchedules.Add(schedule);

                var firstOccurrenceDate = GetNextOccurrenceDate(today, slot.DayOfWeek);
                newOccurrences.Add(new SessionOccurrence
                {
                    Session          = schedule,
                    GroupId          = request.GroupId,
                    OccurrenceDate   = firstOccurrenceDate,
                    CountsForPayment = true,
                    Status           = SessionStatus.Scheduled,
                    TenantId         = tenantId
                });
            }

            await _dbContext.Sessions.AddRangeAsync(newSchedules, ct);
            await _dbContext.SessionOccurrences.AddRangeAsync(newOccurrences, ct);
            await _dbContext.SaveChangesAsync(ct);

            return $"Successfully created {newSchedules.Count} schedule(s) with their first occurrence.";
        }

        public async Task<string> UpdateScheduleAsync(UpdateSessionRequest request, string tenantId, CancellationToken ct = default)
        {
            var schedule = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == request.ScheduleId, ct)
                ?? throw new NotFoundException(["Schedule not found."]);

            if (!schedule.IsActive)
                throw new ConflictException(["Deactivated schedules cannot be updated."]);

            if (request.StartTime >= request.EndTime)
                throw new FluentValidation.ValidationException("Start time must be before end time.");

            // ── Conflict against other recurring schedules on the requested day ──
            var overlapping = await _dbContext.Sessions
                .FirstOrDefaultAsync(s =>
                    s.Id        != request.ScheduleId &&
                    s.IsActive  &&
                    s.DayOfWeek == request.DayOfWeek &&
                    request.StartTime < s.EndTime &&
                    request.EndTime   > s.StartTime, ct);

            if (overlapping != null)
            {
                var conflictGroup = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == overlapping.GroupId, ct);
                throw new ConflictException([
                    $"Time conflict with group [{conflictGroup?.Name}] from [{overlapping.StartTime:hh\\:mm}] to [{overlapping.EndTime:hh\\:mm}]."
                ]);
            }

            bool dayChanged = schedule.DayOfWeek != request.DayOfWeek;

            schedule.DayOfWeek = request.DayOfWeek;
            schedule.StartTime = request.StartTime;
            schedule.EndTime   = request.EndTime;

            // ── If the day shifted, move every future scheduled occurrence + re-check conflicts on new dates ──
            if (dayChanged)
            {
                var today = _dateTime.TodayInAppZone;
                var futureOccurrences = await _dbContext.SessionOccurrences
                    .Where(o =>
                        o.SessionId == request.ScheduleId &&
                        o.Status    == SessionStatus.Scheduled &&
                        o.OccurrenceDate >= today)
                    .OrderBy(o => o.OccurrenceDate)
                    .ToListAsync(ct);

                // Re-home each future occurrence onto the new weekday, one per
                // week. Computing a single shared date would collapse multiple
                // pre-generated occurrences onto the same day (duplicate + a
                // false self-conflict on the 2nd one).
                var baseDate = GetNextOccurrenceDate(today, request.DayOfWeek);
                for (int i = 0; i < futureOccurrences.Count; i++)
                {
                    var newDate = baseDate.AddDays(7 * i);
                    futureOccurrences[i].OccurrenceDate = newDate;

                    await EnsureNoConflictOnDateAsync(futureOccurrences[i].Id, newDate, request.StartTime, request.EndTime, ct);
                }
            }

            await _dbContext.SaveChangesAsync(ct);

            var studentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == schedule.GroupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            if (studentIds.Any())
            {
                var group = await _dbContext.Groups.FindAsync([schedule.GroupId], ct);
                string groupName = group?.Name ?? "المجموعة";
                string dayAr = ArabicDay(request.DayOfWeek);
                string newTime = $"{request.StartTime:hh\\:mm} – {request.EndTime:hh\\:mm}";

                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "تحديث الجدول",
                        message: $"تم تحديث جدول مجموعة ({groupName}). الميعاد الجديد: {newTime} كل يوم {dayAr}.",
                        type: NotificationType.SessionUpdated,
                        route: $"/student/groups/{schedule.GroupId}"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "تحديث جدول ابنك",
                        message: $"تم تحديث جدول مجموعة {name} ({groupName}). الميعاد الجديد: {newTime} كل يوم {dayAr}.",
                        type: NotificationType.SessionUpdated,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Schedule updated successfully.";
        }

        // Arabic day names — DayOfWeek enum is English; we localize on render.
        private static string ArabicDay(DayOfWeek day) => day switch
        {
            DayOfWeek.Sunday    => "الأحد",
            DayOfWeek.Monday    => "الإثنين",
            DayOfWeek.Tuesday   => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday  => "الخميس",
            DayOfWeek.Friday    => "الجمعة",
            DayOfWeek.Saturday  => "السبت",
            _                   => day.ToString()
        };

        public async Task<string> DeactivateScheduleAsync(Guid scheduleId, string tenantId, CancellationToken ct = default)
        {
            var schedule = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
                ?? throw new NotFoundException(["Schedule not found."]);

            if (!schedule.IsActive)
                throw new ConflictException(["Schedule is already deactivated."]);

            schedule.IsActive = false;

            var futureOccurrences = await _dbContext.SessionOccurrences
                .Where(o => o.SessionId == scheduleId
                         && o.Status == SessionStatus.Scheduled
                         && o.OccurrenceDate >= _dateTime.TodayInAppZone)
                .ToListAsync(ct);

            foreach (var occurrence in futureOccurrences)
                occurrence.Status = SessionStatus.Cancelled;

            await _dbContext.SaveChangesAsync(ct);

            return "Schedule deactivated and upcoming occurrences cancelled.";
        }

        // ════════════════════════════════════════════════════════════════════
        //  Today / list
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<TodaySessionResponseDto>> GetTodaySessionsAsync(string tenantId, DateOnly today, bool includePending, CancellationToken ct = default)
        {
            // includePending = true → also surface past-dated occurrences that are still Scheduled
            // (teacher forgot to take attendance or cancel the session, so it's now overdue).
            IQueryable<SessionOccurrence> query = _dbContext.SessionOccurrences
                .AsNoTracking()
                .Include(o => o.Session)
                .Include(o => o.Group);

            if (includePending)
            {
                query = query.Where(o =>
                    o.OccurrenceDate == today ||
                    (o.OccurrenceDate < today && o.Status == SessionStatus.Scheduled));
            }
            else
            {
                query = query.Where(o => o.OccurrenceDate == today);
            }

            var occurrences = await query
                .OrderBy(o => o.OccurrenceDate)
                .ThenBy(o => o.Session != null ? o.Session.StartTime : o.StartTime)
                .Select(o => new TodaySessionResponseDto
                {
                    OccurrenceId   = o.Id,
                    ScheduleId     = o.SessionId,
                    GroupId        = o.GroupId,
                    GroupName      = o.Group.Name,
                    Subject        = o.Group.Subject,
                    StartTime      = o.StartTime ?? o.Session!.StartTime,
                    EndTime        = o.EndTime   ?? o.Session!.EndTime,
                    Status         = o.Status.ToString(),
                    OccurrenceDate = o.OccurrenceDate
                })
                .ToListAsync(ct);

            return occurrences;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Manual occurrence — create / cancel / update / delete
        // ════════════════════════════════════════════════════════════════════

        public async Task<string> CreateManualOccurrenceAsync(CreateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
                ?? throw new NotFoundException(["Group not found."]);

            await EnsureNoConflictOnDateAsync(
                excludeOccurrenceId: null,
                date: request.OccurrenceDate,
                start: request.StartTime,
                end: request.EndTime,
                ct: ct);

            var (message, affectedStudents, paymentEvent) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                // CountsForPayment is true ONLY for AddToCycle — extra session inside a paid cycle.
                bool countsForPayment = request.PaymentMode == SessionPaymentMode.AddToCycle;

                var occurrence = new SessionOccurrence
                {
                    SessionId        = null,
                    GroupId          = request.GroupId,
                    OccurrenceDate   = request.OccurrenceDate,
                    StartTime        = request.StartTime,
                    EndTime          = request.EndTime,
                    PaymentMode      = request.PaymentMode,
                    SessionPrice     = request.SessionPrice,
                    CountsForPayment = countsForPayment,
                    Status           = SessionStatus.Scheduled,
                    TenantId         = tenantId
                };
                _dbContext.SessionOccurrences.Add(occurrence);

                IReadOnlyList<string> students = Array.Empty<string>();
                string msg;
                string paymentEventLocal = "free";

                switch (request.PaymentMode)
                {
                    case SessionPaymentMode.Standalone:
                        students = await _paymentLinker.CreateStandalonePaymentsAsync(
                            occurrence, request.SessionPrice!.Value, tenantId, ct);
                        msg = $"Standalone session added at price {request.SessionPrice:0.##}. A payment record was created for each enrolled student.";
                        paymentEventLocal = $"standalone:{request.SessionPrice:0.##}";
                        break;

                    case SessionPaymentMode.AddToCycle:
                        students = await _paymentLinker.ApplyAddToCycleAsync(
                            occurrence, request.SessionPrice!.Value, tenantId, ct);
                        msg = $"Extra session added to the current cycle — additional fee {request.SessionPrice:0.##} applied to all open student records.";
                        paymentEventLocal = $"addtocycle:{request.SessionPrice:0.##}";
                        break;

                    default:
                        msg = $"Free session added on {request.OccurrenceDate:dd/MM/yyyy}.";
                        break;
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (msg, students, paymentEventLocal);
            });

            // Notifications after commit
            if (affectedStudents.Count > 0 && paymentEvent != "free")
            {
                string title;
                string studentBody;
                string parentBodyTemplate; // {0} = child name
                string groupName = group.Name;
                string dateLabel = request.OccurrenceDate.ToString("dd/MM/yyyy");

                if (paymentEvent.StartsWith("standalone:"))
                {
                    string price = paymentEvent.Substring("standalone:".Length);
                    title              = "حصة استثنائية جديدة";
                    studentBody        = $"تمت إضافة حصة استثنائية لمجموعة ({groupName}) بتاريخ {dateLabel} بسعر {price}.";
                    parentBodyTemplate = $"تمت إضافة حصة استثنائية لـ {{0}} في مجموعة ({groupName}) بتاريخ {dateLabel} بسعر {price}.";
                }
                else
                {
                    string price = paymentEvent.Substring("addtocycle:".Length);
                    title              = "رسوم إضافية";
                    studentBody        = $"تمت إضافة حصة إضافية إلى دورة الدفع الحالية لمجموعة ({groupName}). الرسوم الإضافية: {price}.";
                    parentBodyTemplate = $"تمت إضافة حصة إضافية إلى دورة دفع {{0}} في مجموعة ({groupName}). الرسوم الإضافية: {price}.";
                }

                await _notificationService.SendStudentAndParentNotificationsAsync(
                    affectedStudents.ToList(),
                    new NotificationPayload(
                        title: title,
                        message: studentBody,
                        type: NotificationType.PaymentDue,
                        route: "/student/finance"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: title,
                        message: string.Format(parentBodyTemplate, name),
                        type: NotificationType.PaymentDue,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return message;
        }

        public async Task<string> CancelOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var (groupId, occurrenceDate, paymentMode, paymentAffected) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var occurrence = await _dbContext.SessionOccurrences
                    .Include(o => o.Session)
                    .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                    ?? throw new NotFoundException(["Session occurrence not found."]);

                if (occurrence.Status != SessionStatus.Scheduled)
                    throw new ConflictException(["Only scheduled occurrences can be cancelled."]);

                occurrence.Status = SessionStatus.Cancelled;

                IReadOnlyList<string> affected = Array.Empty<string>();

                // Payment side-effects (manual occurrences only)
                if (!occurrence.SessionId.HasValue)
                {
                    affected = occurrence.PaymentMode switch
                    {
                        SessionPaymentMode.Standalone => await _paymentLinker.CancelStandalonePaymentsAsync(occurrence, ct),
                        SessionPaymentMode.AddToCycle => await _paymentLinker.RevertAddToCycleAsync(occurrence, ct),
                        _                              => Array.Empty<string>()
                    };
                }

                // Recurring: chain to next future occurrence if not already there.
                // We advance in 7-day steps until we land on today or later — this protects
                // the chain when an overdue (past-dated) occurrence is being cancelled.
                if (occurrence.SessionId.HasValue && occurrence.Session!.IsActive)
                {
                    var todayDate = _dateTime.TodayInAppZone;
                    var nextDate = occurrence.OccurrenceDate.AddDays(7);
                    while (nextDate < todayDate)
                        nextDate = nextDate.AddDays(7);

                    var alreadyExists = await _dbContext.SessionOccurrences
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

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (occurrence.GroupId, occurrence.OccurrenceDate, occurrence.PaymentMode, affected);
            });

            // Notifications after commit
            var allStudentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == groupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            var group = await _dbContext.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (allStudentIds.Any())
            {
                string groupName = group?.Name ?? "المجموعة";
                string dateLabel = occurrenceDate.ToString("dd/MM/yyyy");
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    allStudentIds,
                    new NotificationPayload(
                        title: "إلغاء حصة",
                        message: $"تم إلغاء حصة يوم {dateLabel} لمجموعة ({groupName}).",
                        type: NotificationType.SessionUpdated,
                        route: $"/student/groups/{groupId}"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "إلغاء حصة لابنك",
                        message: $"تم إلغاء حصة يوم {dateLabel} لمجموعة {name} ({groupName}).",
                        type: NotificationType.SessionUpdated,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            // Targeted follow-ups for students whose payment state changed
            if (paymentAffected.Count > 0)
            {
                string groupName = group?.Name ?? "المجموعة";
                string dateLabel = occurrenceDate.ToString("dd/MM/yyyy");

                if (paymentMode == SessionPaymentMode.Standalone)
                {
                    await _notificationService.SendStudentAndParentNotificationsAsync(
                        paymentAffected.ToList(),
                        new NotificationPayload(
                            title: "محفوظ لك دفعك السابق",
                            message: $"تم إلغاء الحصة الاستثنائية يوم {dateLabel}، تم حفظ دفعتك السابقة دون أي مبلغ إضافي.",
                            type: NotificationType.PaymentConfirmed,
                            route: "/student/finance"),
                        parentPayloadFactory: (sid, name) => new NotificationPayload(
                            title: "محفوظ دفع ابنك",
                            message: $"تم إلغاء حصة استثنائية لـ {name} يوم {dateLabel}، وتم حفظ الدفعة السابقة دون أي مبلغ إضافي.",
                            type: NotificationType.PaymentConfirmed,
                            route: $"/parent/children/{sid}"),
                        tenantId,
                        ct);
                }
                else if (paymentMode == SessionPaymentMode.AddToCycle)
                {
                    await _notificationService.SendStudentAndParentNotificationsAsync(
                        paymentAffected.ToList(),
                        new NotificationPayload(
                            title: "تعديل الرسوم",
                            message: $"تم إلغاء حصة إضافية لمجموعة ({groupName})، وتم تخفيض المتبقي عليك.",
                            type: NotificationType.PaymentDue,
                            route: "/student/finance"),
                        parentPayloadFactory: (sid, name) => new NotificationPayload(
                            title: "تعديل رسوم ابنك",
                            message: $"تم إلغاء حصة إضافية لـ {name} في مجموعة ({groupName})، وتم تخفيض المتبقي.",
                            type: NotificationType.PaymentDue,
                            route: $"/parent/children/{sid}"),
                        tenantId,
                        ct);
                }
            }

            return "Session occurrence cancelled successfully.";
        }

        public async Task<string> UpdateManualOccurrenceAsync(Guid occurrenceId, UpdateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _dbContext.SessionOccurrences
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                ?? throw new NotFoundException(["Session occurrence not found."]);

            if (occurrence.SessionId != null)
                throw new ConflictException(["Recurring schedule occurrences cannot be edited from here."]);

            if (occurrence.Status != SessionStatus.Scheduled)
                throw new ConflictException(["Only scheduled occurrences can be rescheduled."]);

            if (request.StartTime >= request.EndTime)
                throw new FluentValidation.ValidationException("Start time must be before end time.");

            await EnsureNoConflictOnDateAsync(
                excludeOccurrenceId: occurrenceId,
                date: request.OccurrenceDate,
                start: request.StartTime,
                end: request.EndTime,
                ct: ct);

            occurrence.OccurrenceDate = request.OccurrenceDate;
            occurrence.StartTime      = request.StartTime;
            occurrence.EndTime        = request.EndTime;

            await _dbContext.SaveChangesAsync(ct);

            return $"Session rescheduled to {request.OccurrenceDate:dd/MM/yyyy} ({request.StartTime:hh\\:mm}–{request.EndTime:hh\\:mm}).";
        }

        public async Task<string> DeleteManualOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var (groupId, paymentMode, affected) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var occurrence = await _dbContext.SessionOccurrences
                    .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                    ?? throw new NotFoundException(["Session occurrence not found."]);

                if (occurrence.SessionId != null)
                    throw new ConflictException(["Recurring occurrences cannot be deleted — cancel them instead."]);

                if (occurrence.Status == SessionStatus.Completed)
                    throw new ConflictException(["Cannot delete a completed occurrence — attendance is already recorded."]);

                IReadOnlyList<string> affectedLocal = Array.Empty<string>();

                switch (occurrence.PaymentMode)
                {
                    case SessionPaymentMode.Standalone:
                        await _paymentLinker.EnsureStandaloneSafeToDeleteAsync(occurrence, ct);
                        await _paymentLinker.DeleteStandalonePaymentsAsync(occurrence, ct);
                        break;

                    case SessionPaymentMode.AddToCycle:
                        affectedLocal = await _paymentLinker.RevertAddToCycleAsync(occurrence, ct);
                        break;
                }

                _dbContext.SessionOccurrences.Remove(occurrence);
                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (occurrence.GroupId, occurrence.PaymentMode, affectedLocal);
            });

            // Targeted follow-up for AddToCycle balance reduction
            if (paymentMode == SessionPaymentMode.AddToCycle && affected.Count > 0)
            {
                var group = await _dbContext.Groups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == groupId, ct);
                string groupName = group?.Name ?? "المجموعة";

                await _notificationService.SendStudentAndParentNotificationsAsync(
                    affected.ToList(),
                    new NotificationPayload(
                        title: "تعديل الرسوم",
                        message: $"تم حذف حصة إضافية لمجموعة ({groupName})، وتم تخفيض المتبقي عليك.",
                        type: NotificationType.PaymentDue,
                        route: "/student/finance"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "تعديل رسوم ابنك",
                        message: $"تم حذف حصة إضافية لـ {name} في مجموعة ({groupName})، وتم تخفيض المتبقي.",
                        type: NotificationType.PaymentDue,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Manual session deleted successfully.";
        }

        public async Task<string> UpdateRecurringOccurrenceAsync(
            Guid occurrenceId, UpdateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _dbContext.SessionOccurrences
                .Include(o => o.Session)
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                ?? throw new NotFoundException(["Session occurrence not found."]);

            if (occurrence.SessionId == null)
                throw new ConflictException(["This occurrence is not part of a recurring schedule. Use the manual-occurrence endpoint instead."]);

            if (occurrence.Status != SessionStatus.Scheduled)
                throw new ConflictException(["Only scheduled occurrences can be rescheduled."]);

            if (request.StartTime >= request.EndTime)
                throw new FluentValidation.ValidationException("Start time must be before end time.");

            await EnsureNoConflictOnDateAsync(
                excludeOccurrenceId: occurrenceId,
                date: request.OccurrenceDate,
                start: request.StartTime,
                end: request.EndTime,
                ct: ct);

            // Override the occurrence's own date/time. The parent Session row
            // is intentionally untouched — the recurring schedule (its day +
            // time) keeps applying to every OTHER week, so next week reverts
            // to the original slot.
            occurrence.OccurrenceDate = request.OccurrenceDate;
            occurrence.StartTime      = request.StartTime;
            occurrence.EndTime        = request.EndTime;

            await _dbContext.SaveChangesAsync(ct);

            // Notify the group that THIS week's session moved. We deliberately
            // don't change the parent schedule notification (no SessionUpdated
            // for the recurring schedule itself) so students don't think the
            // permanent slot changed.
            var studentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == occurrence.GroupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            if (studentIds.Any())
            {
                var group = await _dbContext.Groups.FindAsync([occurrence.GroupId], ct);
                string groupName = group?.Name ?? "المجموعة";
                string dateLabel = request.OccurrenceDate.ToString("dd/MM/yyyy");
                string timeRange = $"{request.StartTime:hh\\:mm} – {request.EndTime:hh\\:mm}";

                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "تعديل ميعاد حصة",
                        message: $"تم تأجيل حصة مجموعة ({groupName}) لـ {dateLabel} الساعة {timeRange}.",
                        type: NotificationType.SessionUpdated,
                        route: $"/student/groups/{occurrence.GroupId}"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "تعديل ميعاد حصة لابنك",
                        message: $"تم تأجيل حصة {name} في مجموعة ({groupName}) لـ {dateLabel} الساعة {timeRange}.",
                        type: NotificationType.SessionUpdated,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return $"Session rescheduled to {request.OccurrenceDate:dd/MM/yyyy} ({request.StartTime:hh\\:mm}–{request.EndTime:hh\\:mm}).";
        }

        public async Task<string> DeleteRecurringOccurrenceAsync(
            Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var (groupId, occurrenceDate) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var occurrence = await _dbContext.SessionOccurrences
                    .Include(o => o.Session)
                    .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct)
                    ?? throw new NotFoundException(["Session occurrence not found."]);

                if (occurrence.SessionId == null)
                    throw new ConflictException(["This occurrence is not part of a recurring schedule — use the manual-delete endpoint."]);

                if (occurrence.Status == SessionStatus.Completed)
                    throw new ConflictException(["Cannot delete a completed occurrence — attendance is already recorded."]);

                Guid? sessionIdSnapshot = occurrence.SessionId;
                Guid    groupIdSnapshot = occurrence.GroupId;
                DateOnly dateSnapshot   = occurrence.OccurrenceDate;

                // Physically remove the row. Recurring sessions price through
                // the group's MonthlyFee — there are no per-occurrence payment
                // records to clean up, so this is just an EF Remove.
                _dbContext.SessionOccurrences.Remove(occurrence);

                // Keep the recurring chain alive: if no future Scheduled
                // occurrence exists on this schedule, generate next week's
                // (same logic as Cancel). Otherwise the teacher would have an
                // empty schedule until they manually nudge the system.
                if (occurrence.Session!.IsActive)
                {
                    var todayDate = _dateTime.TodayInAppZone;
                    bool hasFutureScheduled = await _dbContext.SessionOccurrences
                        .AnyAsync(o => o.SessionId == sessionIdSnapshot
                                    && o.Id != occurrenceId
                                    && o.Status == SessionStatus.Scheduled
                                    && o.OccurrenceDate >= todayDate, ct);

                    if (!hasFutureScheduled)
                    {
                        var nextDate = dateSnapshot.AddDays(7);
                        while (nextDate < todayDate)
                            nextDate = nextDate.AddDays(7);

                        bool alreadyOnDate = await _dbContext.SessionOccurrences
                            .AnyAsync(o => o.SessionId == sessionIdSnapshot
                                        && o.OccurrenceDate == nextDate, ct);

                        if (!alreadyOnDate)
                        {
                            _dbContext.SessionOccurrences.Add(new SessionOccurrence
                            {
                                SessionId        = sessionIdSnapshot,
                                GroupId          = groupIdSnapshot,
                                OccurrenceDate   = nextDate,
                                CountsForPayment = true,
                                Status           = SessionStatus.Scheduled,
                                TenantId         = tenantId
                            });
                        }
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return (groupIdSnapshot, dateSnapshot);
            });

            // Notify the group — same shape as Cancel so students see clear
            // language about which date was removed.
            var studentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == groupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            if (studentIds.Any())
            {
                var group = await _dbContext.Groups.AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == groupId, ct);
                string groupName = group?.Name ?? "المجموعة";
                string dateLabel = occurrenceDate.ToString("dd/MM/yyyy");

                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "إلغاء حصة",
                        message: $"تم حذف حصة يوم {dateLabel} لمجموعة ({groupName}).",
                        type: NotificationType.SessionUpdated,
                        route: $"/student/groups/{groupId}"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "إلغاء حصة لابنك",
                        message: $"تم حذف حصة يوم {dateLabel} لمجموعة {name} ({groupName}).",
                        type: NotificationType.SessionUpdated,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Recurring session occurrence deleted.";
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Throws ConflictException if any non-cancelled occurrence on the date overlaps.</summary>
        private async Task EnsureNoConflictOnDateAsync(
            Guid? excludeOccurrenceId, DateOnly date, TimeSpan start, TimeSpan end, CancellationToken ct)
        {
            var sameDay = await _dbContext.SessionOccurrences
                .Include(o => o.Session)
                .Where(o => o.OccurrenceDate == date
                         && o.Status != SessionStatus.Cancelled
                         && (excludeOccurrenceId == null || o.Id != excludeOccurrenceId.Value))
                .ToListAsync(ct);

            var conflict = sameDay.FirstOrDefault(o =>
            {
                var oStart = o.StartTime ?? o.Session?.StartTime;
                var oEnd   = o.EndTime   ?? o.Session?.EndTime;
                if (oStart == null || oEnd == null) return false;
                return start < oEnd.Value && end > oStart.Value;
            });

            if (conflict == null) return;

            var conflictGroup = await _dbContext.Groups.FindAsync([conflict.GroupId], ct);
            var cStart = (conflict.StartTime ?? conflict.Session?.StartTime)!.Value;
            var cEnd   = (conflict.EndTime   ?? conflict.Session?.EndTime)!.Value;

            throw new ConflictException([
                $"Time conflict: group [{conflictGroup?.Name}] already has a session that day from [{cStart:hh\\:mm}] to [{cEnd:hh\\:mm}]."
            ]);
        }

        private static DateOnly GetNextOccurrenceDate(DateOnly from, DayOfWeek targetDay)
        {
            int daysUntil = ((int)targetDay - (int)from.DayOfWeek + 7) % 7;
            return from.AddDays(daysUntil == 0 ? 0 : daysUntil);
        }
    }
}
