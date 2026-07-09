using Application.Exceptions;
using Application.Features.Notifications.DTOs;
using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Academics
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly TenantDbContext _tenantDbContext;

        public PaymentService(
            ApplicationDbContext dbContext,
            INotificationService notificationService,
            TenantDbContext tenantDbContext)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _tenantDbContext = tenantDbContext;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Queries
        // ════════════════════════════════════════════════════════════════════

        public async Task<PaginatedResult<PaymentCycleDto>> GetGroupCyclesAsync(
            Guid groupId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var baseQuery = _dbContext.PaymentCycles
                .AsNoTracking()
                .Where(c => c.GroupId == groupId);

            var totalCount = await baseQuery.CountAsync(ct);

            var data = await baseQuery
                .OrderByDescending(c => c.CycleNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new PaymentCycleDto
                {
                    Id                = c.Id,
                    CycleNumber       = c.CycleNumber,
                    SessionsTarget    = c.SessionsTarget,
                    SessionsCompleted = c.SessionsCompleted,
                    IsCompleted       = c.IsCompleted,
                    OpenedAt          = c.OpenedAt,
                    ClosedAt          = c.ClosedAt,
                    BaseFee           = c.BaseFee,
                    ExtraFee          = c.ExtraFee,
                    UnpaidCount       = c.StudentRecords.Count(r =>
                        r.Status != PaymentStatus.Waived &&
                        (r.ExpectedAmount - r.DiscountAmount) > (r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m))
                })
                .ToListAsync(ct);

            return PaginatedResult<PaymentCycleDto>.Create(data, totalCount, page, pageSize);
        }

        public async Task<PaginatedResult<StudentPaymentRecordDto>> GetCycleStudentRecordsAsync(
            Guid cycleId, PaymentStatus? filterStatus = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var cycleExists = await _dbContext.PaymentCycles.AnyAsync(c => c.Id == cycleId, ct);
            if (!cycleExists)
                throw new NotFoundException(["Payment cycle not found."]);

            return await BuildRecordsPageAsync(
                r => r.PaymentCycleId == cycleId,
                filterStatus, page, pageSize, ct);
        }

        public async Task<PaginatedResult<StudentPaymentRecordDto>> GetStandaloneOccurrenceRecordsAsync(
            Guid occurrenceId, PaymentStatus? filterStatus = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var occurrenceExists = await _dbContext.SessionOccurrences.AnyAsync(o => o.Id == occurrenceId, ct);
            if (!occurrenceExists)
                throw new NotFoundException(["Session occurrence not found."]);

            return await BuildRecordsPageAsync(
                r => r.OccurrenceId == occurrenceId,
                filterStatus, page, pageSize, ct);
        }

        public async Task<TeacherFinancialOverviewDto> GetTeacherFinancialOverviewAsync(
            string tenantId, CancellationToken ct = default)
        {
            // The tenant query filter handles isolation; we just need to skip
            // soft-deleted groups. One round-trip pulls every group + every
            // payment record + every transaction sum, then we aggregate in
            // memory (small N — single teacher's groups).
            var groups = await _dbContext.Groups
                .AsNoTracking()
                .Select(g => new { g.Id, g.Name, g.Subject })
                .ToListAsync(ct);

            if (groups.Count == 0)
                return new TeacherFinancialOverviewDto();

            var groupIds = groups.Select(g => g.Id).ToList();

            // Pull every StudentPaymentRecord (cycle + standalone) with its
            // paid total — one query, no per-group N+1.
            var records = await _dbContext.StudentPaymentRecords
                .AsNoTracking()
                .Where(r => groupIds.Contains(r.GroupId))
                .Select(r => new
                {
                    r.GroupId,
                    r.StudentId,
                    r.Status,
                    r.ExpectedAmount,
                    r.DiscountAmount,
                    Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m
                })
                .ToListAsync(ct);

            // Cycle counts come from PaymentCycles directly so an empty group
            // (no records yet) still contributes 0 cycles instead of crashing.
            var cycleRows = await _dbContext.PaymentCycles
                .AsNoTracking()
                .Where(c => groupIds.Contains(c.GroupId))
                .Select(c => new { c.GroupId, c.IsCompleted })
                .ToListAsync(ct);

            var enrollments = await _dbContext.GroupStudents
                .AsNoTracking()
                .Where(gs => groupIds.Contains(gs.GroupId))
                .Select(gs => new { gs.GroupId, gs.StudentId })
                .ToListAsync(ct);

            var overview = new TeacherFinancialOverviewDto
            {
                GroupsCount         = groups.Count,
                ActiveStudentsCount = enrollments.Select(e => e.StudentId).Distinct().Count(),
                CyclesCount         = cycleRows.Count,
                OpenCyclesCount     = cycleRows.Count(c => !c.IsCompleted),
            };

            // Per-group rows, keyed by groupId so an empty group still shows up
            // with zeros (helps the teacher see they haven't logged anything
            // for that group rather than wondering if it dropped off the list).
            var rowMap = groups.ToDictionary(
                g => g.Id,
                g => new TeacherFinancialGroupRow
                {
                    GroupId   = g.Id,
                    GroupName = g.Name,
                    Subject   = g.Subject,
                });

            foreach (var gs in enrollments.GroupBy(e => e.GroupId))
                if (rowMap.TryGetValue(gs.Key, out var row))
                    row.StudentsCount = gs.Select(x => x.StudentId).Distinct().Count();

            foreach (var c in cycleRows.GroupBy(c => c.GroupId))
                if (rowMap.TryGetValue(c.Key, out var row))
                {
                    row.CyclesCount     = c.Count();
                    row.OpenCyclesCount = c.Count(x => !x.IsCompleted);
                }

            // Students with any record left to pay — counted globally (unique
            // across every record they have, since one student can have unpaid
            // balances spread across multiple groups).
            var outstandingStudents = new HashSet<string>(StringComparer.Ordinal);

            foreach (var r in records)
            {
                decimal expected = r.Status == PaymentStatus.Waived
                    ? 0m
                    : r.ExpectedAmount - r.DiscountAmount;
                decimal collected = r.Paid;

                overview.TotalCollected += collected;
                overview.TotalExpected  += expected;

                if (rowMap.TryGetValue(r.GroupId, out var row))
                {
                    row.Collected += collected;
                    row.Expected  += expected;
                }

                if (expected - collected > 0m)
                    outstandingStudents.Add(r.StudentId);
            }

            overview.OutstandingStudentsCount = outstandingStudents.Count;

            // Worst debts first — that's what the teacher actually wants to
            // see at the top. Empty groups (Remaining=0) sink to the bottom.
            overview.Groups = rowMap.Values
                .OrderByDescending(g => g.Remaining)
                .ThenByDescending(g => g.Collected)
                .ToList();

            return overview;
        }

        public async Task<GroupFinancialSummaryDto> GetGroupFinancialSummaryAsync(
            Guid groupId, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, ct)
                ?? throw new NotFoundException(["Group not found."]);

            var totalStudents = await _dbContext.GroupStudents
                .CountAsync(gs => gs.GroupId == groupId, ct);

            var summary = new GroupFinancialSummaryDto
            {
                GroupId       = group.Id,
                GroupName     = group.Name,
                MonthlyFee    = group.MonthlyFee,
                TotalStudents = totalStudents
            };

            // ── Current cycle ─────────────────────────────────────────────────
            var activeCycle = await _dbContext.PaymentCycles
                .AsNoTracking()
                .Where(c => c.GroupId == groupId && !c.IsCompleted)
                .OrderByDescending(c => c.CycleNumber)
                .FirstOrDefaultAsync(ct);

            if (activeCycle != null)
            {
                summary.CurrentCycleNumber = activeCycle.CycleNumber;
                summary.SessionsCompleted  = activeCycle.SessionsCompleted;
                summary.SessionsTarget     = activeCycle.SessionsTarget;

                var cycleRecords = await _dbContext.StudentPaymentRecords
                    .AsNoTracking()
                    .Where(r => r.PaymentCycleId == activeCycle.Id)
                    .Select(r => new
                    {
                        r.Status,
                        r.ExpectedAmount,
                        r.DiscountAmount,
                        Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m
                    })
                    .ToListAsync(ct);

                summary.PaidCount    = cycleRecords.Count(r => r.Status == PaymentStatus.Paid);
                summary.PartialCount = cycleRecords.Count(r => r.Status == PaymentStatus.PartiallyPaid);
                summary.WaivedCount  = cycleRecords.Count(r => r.Status == PaymentStatus.Waived);
                summary.UnpaidCount  = Math.Max(0,
                    totalStudents - summary.PaidCount - summary.PartialCount - summary.WaivedCount);

                summary.CycleCollected = cycleRecords.Sum(r => r.Paid);
                summary.CycleExpected  = cycleRecords
                    .Where(r => r.Status != PaymentStatus.Waived)
                    .Sum(r => r.ExpectedAmount - r.DiscountAmount);
            }

            // ── All standalone occurrences for this group ─────────────────────
            var standaloneAgg = await _dbContext.StudentPaymentRecords
                .AsNoTracking()
                .Where(r => r.GroupId == groupId && r.OccurrenceId != null)
                .Select(r => new
                {
                    r.Status,
                    r.ExpectedAmount,
                    r.DiscountAmount,
                    Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m
                })
                .ToListAsync(ct);

            summary.StandaloneCollected = standaloneAgg.Sum(r => r.Paid);
            summary.StandaloneExpected  = standaloneAgg
                .Where(r => r.Status != PaymentStatus.Waived)
                .Sum(r => r.ExpectedAmount - r.DiscountAmount);

            return summary;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Student-facing: "My Payments"
        // ════════════════════════════════════════════════════════════════════

        public async Task<StudentPaymentsOverviewDto> GetMyPaymentsAsync(
            string studentId, CancellationToken ct = default)
        {
            // 1. Groups the student is currently enrolled in.
            var enrollments = await _dbContext.GroupStudents
                .AsNoTracking()
                .Include(gs => gs.Group)
                .Where(gs => gs.StudentId == studentId)
                .Select(gs => new
                {
                    gs.GroupId,
                    GroupName = gs.Group.Name,
                    gs.Group.Subject,
                    gs.TenantId
                })
                .ToListAsync(ct);

            if (enrollments.Count == 0)
                return new StudentPaymentsOverviewDto();

            var groupIds  = enrollments.Select(e => e.GroupId).Distinct().ToList();
            var tenantIds = enrollments.Select(e => e.TenantId).Distinct().ToList();

            // Tenant names → teacher display labels
            var tenants = await _tenantDbContext.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            // 2. All this student's payment records across those groups (cycle + standalone).
            var records = await _dbContext.StudentPaymentRecords
                .AsNoTracking()
                .Include(r => r.Transactions)
                .Where(r => r.StudentId == studentId && groupIds.Contains(r.GroupId))
                .ToListAsync(ct);

            // 3. Open cycles for those groups (needed for sessionsCompleted / target / cycle number).
            var openCycles = await _dbContext.PaymentCycles
                .AsNoTracking()
                .Where(c => groupIds.Contains(c.GroupId) && !c.IsCompleted)
                .ToDictionaryAsync(c => c.Id, c => c, ct);

            // 4. Standalone occurrence dates (so we can show them with a real date).
            var standaloneOccurrenceIds = records
                .Where(r => r.OccurrenceId.HasValue)
                .Select(r => r.OccurrenceId!.Value)
                .Distinct()
                .ToList();

            var standaloneOccurrences = await _dbContext.SessionOccurrences
                .AsNoTracking()
                .Where(o => standaloneOccurrenceIds.Contains(o.Id))
                .Select(o => new { o.Id, o.OccurrenceDate })
                .ToDictionaryAsync(o => o.Id, o => o.OccurrenceDate, ct);

            var overview = new StudentPaymentsOverviewDto();

            foreach (var enr in enrollments)
            {
                var groupDto = new StudentGroupPaymentsDto
                {
                    GroupId     = enr.GroupId,
                    GroupName   = enr.GroupName,
                    Subject     = enr.Subject,
                    TeacherName = tenants.TryGetValue(enr.TenantId, out var tName) ? tName : null
                };

                // ── Cycle record (newest open cycle on the group) ─────────────
                var cycleRecord = records
                    .Where(r => r.GroupId == enr.GroupId
                             && r.PaymentCycleId.HasValue
                             && openCycles.ContainsKey(r.PaymentCycleId.Value))
                    .OrderByDescending(r => openCycles[r.PaymentCycleId!.Value].CycleNumber)
                    .FirstOrDefault();

                if (cycleRecord != null)
                {
                    var cycle = openCycles[cycleRecord.PaymentCycleId!.Value];
                    decimal totalPaid = cycleRecord.Transactions.Sum(t => t.Amount);

                    groupDto.CurrentCycle = new StudentCycleRecordDto
                    {
                        RecordId          = cycleRecord.Id,
                        CycleId           = cycle.Id,
                        CycleNumber       = cycle.CycleNumber,
                        SessionsCompleted = cycle.SessionsCompleted,
                        SessionsTarget    = cycle.SessionsTarget,
                        IsCycleClosed     = cycle.IsCompleted,
                        ExpectedAmount    = cycleRecord.ExpectedAmount,
                        DiscountAmount    = cycleRecord.DiscountAmount,
                        DiscountReason    = cycleRecord.DiscountReason,
                        TotalPaid         = totalPaid,
                        Status            = cycleRecord.Status,
                        Transactions      = cycleRecord.Transactions
                            .OrderByDescending(t => t.PaidAt)
                            .Select(t => new PaymentTransactionDto
                            {
                                Id     = t.Id,
                                Amount = t.Amount,
                                PaidAt = t.PaidAt,
                                PaidBy = t.PaidBy,
                                Notes  = t.Notes
                            })
                            .ToList()
                    };
                }

                // ── Standalone records for this group ─────────────────────────
                var standaloneRecords = records
                    .Where(r => r.GroupId == enr.GroupId && r.OccurrenceId.HasValue)
                    .OrderByDescending(r => standaloneOccurrences.TryGetValue(r.OccurrenceId!.Value, out var d) ? d : DateOnly.MinValue)
                    .ToList();

                foreach (var sr in standaloneRecords)
                {
                    decimal totalPaid = sr.Transactions.Sum(t => t.Amount);
                    groupDto.Standalone.Add(new StudentStandaloneRecordDto
                    {
                        RecordId        = sr.Id,
                        OccurrenceId    = sr.OccurrenceId!.Value,
                        OccurrenceDate  = standaloneOccurrences.TryGetValue(sr.OccurrenceId.Value, out var d) ? d : DateOnly.MinValue,
                        ExpectedAmount  = sr.ExpectedAmount,
                        DiscountAmount  = sr.DiscountAmount,
                        DiscountReason  = sr.DiscountReason,
                        TotalPaid       = totalPaid,
                        Status          = sr.Status,
                        Transactions    = sr.Transactions
                            .OrderByDescending(t => t.PaidAt)
                            .Select(t => new PaymentTransactionDto
                            {
                                Id     = t.Id,
                                Amount = t.Amount,
                                PaidAt = t.PaidAt,
                                PaidBy = t.PaidBy,
                                Notes  = t.Notes
                            })
                            .ToList()
                    });
                }

                // ── Group totals: aggregate EVERY record for this group across ALL
                //    cycles (open + closed) + standalone. This makes unpaid balances
                //    from a closed cycle visible to the student (they used to vanish),
                //    and always reflects later edits. Waived contributes 0.
                decimal groupExpected = 0;
                decimal groupPaid     = 0;
                decimal groupRemaining = 0;
                decimal previousCyclesRemaining = 0;

                foreach (var r in records.Where(r => r.GroupId == enr.GroupId))
                {
                    decimal paid = r.Transactions.Sum(t => t.Amount);
                    decimal net  = r.Status == PaymentStatus.Waived ? 0m : r.ExpectedAmount - r.DiscountAmount;
                    decimal rem  = Math.Max(0m, net - paid);

                    groupExpected  += net;
                    groupPaid      += paid;
                    groupRemaining += rem;

                    // Remaining that lives in a CLOSED cycle = debt from a previous period.
                    bool isClosedCycle = r.PaymentCycleId.HasValue && !openCycles.ContainsKey(r.PaymentCycleId.Value);
                    if (isClosedCycle) previousCyclesRemaining += rem;
                }

                groupDto.TotalExpected           = groupExpected;
                groupDto.TotalPaid               = groupPaid;
                groupDto.TotalRemaining          = groupRemaining;
                groupDto.PreviousCyclesRemaining = previousCyclesRemaining;

                overview.Groups.Add(groupDto);
            }

            // ── Grand totals across all groups ────────────────────────────────
            overview.TotalExpected  = overview.Groups.Sum(g => g.TotalExpected);
            overview.TotalPaid      = overview.Groups.Sum(g => g.TotalPaid);
            overview.TotalRemaining = overview.Groups.Sum(g => g.TotalRemaining);
            overview.GroupsWithOutstanding = overview.Groups.Count(g => g.HasOutstanding);

            // Sort: groups with outstanding first (newest first within), then settled
            overview.Groups = overview.Groups
                .OrderByDescending(g => g.HasOutstanding)
                .ThenBy(g => g.GroupName)
                .ToList();

            return overview;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Commands
        // ════════════════════════════════════════════════════════════════════

        public async Task<string> RecordPaymentAsync(
            RecordPaymentRequest request, string tenantId, CancellationToken ct = default)
        {
            // Validate timestamp at the service boundary too (defence in depth).
            DateTime paidAt = (request.PaidAt ?? DateTime.UtcNow).ToUniversalTime();
            if (paidAt > DateTime.UtcNow.AddMinutes(5))
                throw new ConflictException(["Payment date cannot be in the future."]);

            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var record = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.Id == request.RecordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                if (record.Status == PaymentStatus.Waived)
                    throw new ConflictException(["Cannot record a payment on a waived record. Unwaive it first."]);

                decimal currentPaid = record.Transactions.Sum(t => t.Amount);
                decimal newTotal    = currentPaid + request.Amount;
                decimal netExpected = record.ExpectedAmount - record.DiscountAmount;

                if (newTotal > netExpected)
                {
                    decimal remaining = Math.Max(0, netExpected - currentPaid);
                    throw new ConflictException([
                        $"Amount exceeds the remaining balance. Remaining: {remaining:0.##}, attempted: {request.Amount:0.##}."
                    ]);
                }

                var transaction = new PaymentTransaction
                {
                    StudentPaymentRecordId = record.Id,
                    Amount   = request.Amount,
                    PaidAt   = paidAt,
                    Notes    = request.Notes,
                    TenantId = tenantId
                };
                _dbContext.PaymentTransactions.Add(transaction);

                var previousStatus = record.Status;
                record.Status = ComputeStatus(newTotal, netExpected);

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // ── Notifications (outside the transaction) ───────────────────
                if (record.Status != previousStatus &&
                    (record.Status == PaymentStatus.Paid || record.Status == PaymentStatus.PartiallyPaid))
                {
                    var group = await _dbContext.Groups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Id == record.GroupId, ct);
                    string groupName = group?.Name ?? "المجموعة";
                    bool fullyPaid = record.Status == PaymentStatus.Paid;

                    string title = fullyPaid ? "تم سداد دفعتك بالكامل" : "تم تسجيل دفعة جزئية";
                    string studentBody = fullyPaid
                        ? $"تم سداد كامل دفعتك لمجموعة ({groupName}) ({newTotal:0.##}/{netExpected:0.##})."
                        : $"تم تسجيل دفعة جزئية قدرها {request.Amount:0.##} لمجموعة ({groupName}). الإجمالي: {newTotal:0.##}/{netExpected:0.##}.";

                    await _notificationService.SendStudentAndParentNotificationsAsync(
                        new List<string> { record.StudentId },
                        new NotificationPayload(
                            title: title,
                            message: studentBody,
                            type: NotificationType.PaymentConfirmed,
                            route: "/student/payments"),
                        parentPayloadFactory: (sid, name) => new NotificationPayload(
                            title: fullyPaid ? "سداد ابنك بالكامل" : "دفعة جزئية لابنك",
                            message: fullyPaid
                                ? $"تم سداد كامل دفعة {name} لمجموعة ({groupName}) ({newTotal:0.##}/{netExpected:0.##})."
                                : $"تم تسجيل دفعة جزئية قدرها {request.Amount:0.##} لـ {name} في مجموعة ({groupName}). الإجمالي: {newTotal:0.##}/{netExpected:0.##}.",
                            type: NotificationType.PaymentConfirmed,
                            route: $"/parent/children/{sid}"),
                        tenantId, ct);
                }

                return $"Payment of {request.Amount:0.##} recorded. Total paid: {newTotal:0.##}/{netExpected:0.##}.";
            });
        }

        public async Task<string> DeletePaymentTransactionAsync(
            Guid transactionId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var transaction = await _dbContext.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.Id == transactionId, ct)
                    ?? throw new NotFoundException(["Payment transaction not found."]);

                var record = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.Id == transaction.StudentPaymentRecordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                decimal removedAmount = transaction.Amount;
                _dbContext.PaymentTransactions.Remove(transaction);

                decimal remaining = record.Transactions
                    .Where(t => t.Id != transactionId)
                    .Sum(t => t.Amount);
                decimal netExpected = record.ExpectedAmount - record.DiscountAmount;

                // Don't override a Waived status here — waive is an explicit teacher decision.
                if (record.Status != PaymentStatus.Waived)
                    record.Status = ComputeStatus(remaining, netExpected);

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return $"Transaction of {removedAmount:0.##} removed. Total paid is now {remaining:0.##}/{netExpected:0.##}.";
            });
        }

        public async Task<string> CloseCycleManuallyAsync(
            Guid cycleId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var cycle = await _dbContext.PaymentCycles
                    .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
                    ?? throw new NotFoundException(["Payment cycle not found."]);

                if (cycle.IsCompleted)
                    throw new ConflictException(["This cycle is already closed."]);

                // Close + open next + carry every student's unpaid balance forward.
                var newCycle = await CycleTransition.CloseAndOpenNextAsync(_dbContext, cycle, tenantId, ct);

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return $"Cycle {cycle.CycleNumber} closed. New cycle {newCycle.CycleNumber} opened.";
            });
        }

        public async Task<string> WaivePaymentRecordAsync(
            Guid recordId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var record = await _dbContext.StudentPaymentRecords
                    .FirstOrDefaultAsync(r => r.Id == recordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                if (record.Status == PaymentStatus.Paid)
                    throw new ConflictException(["Cannot waive a record that has already been fully paid."]);

                if (record.Status == PaymentStatus.Waived)
                    throw new ConflictException(["This record is already waived."]);

                record.Status = PaymentStatus.Waived;

                await _dbContext.SaveChangesAsync(ct);

                return "Student waived from this payment obligation.";
            });
        }

        public async Task<string> UnwaivePaymentRecordAsync(
            Guid recordId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var record = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.Id == recordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                if (record.Status != PaymentStatus.Waived)
                    throw new ConflictException(["This record is not waived."]);

                decimal totalPaid = record.Transactions.Sum(t => t.Amount);
                decimal netExpected = record.ExpectedAmount - record.DiscountAmount;
                record.Status = ComputeStatus(totalPaid, netExpected);

                await _dbContext.SaveChangesAsync(ct);

                return $"Waive reversed. Status is now {record.Status}.";
            });
        }

        public async Task<string> ApplyDiscountAsync(
            Guid recordId, decimal amount, string? reason, string tenantId, CancellationToken ct = default)
        {
            if (amount <= 0)
                throw new ConflictException(["Discount amount must be greater than zero."]);

            var (studentId, groupId, newNet) = await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var record = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.Id == recordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                if (record.Status == PaymentStatus.Waived)
                    throw new ConflictException([
                        "Cannot apply a discount to a waived record. Unwaive it first."
                    ]);

                if (amount > record.ExpectedAmount)
                    throw new ConflictException([
                        $"Discount cannot exceed the expected amount ({record.ExpectedAmount:0.##})."
                    ]);

                decimal totalPaid = record.Transactions.Sum(t => t.Amount);
                decimal newNetExpected = record.ExpectedAmount - amount;

                if (newNetExpected < totalPaid)
                {
                    decimal maxAllowed = record.ExpectedAmount - totalPaid;
                    throw new ConflictException([
                        $"Discount would drop the balance below what's already been paid. " +
                        $"Maximum allowed: {Math.Max(0, maxAllowed):0.##}."
                    ]);
                }

                record.DiscountAmount = amount;
                record.DiscountReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                record.Status         = ComputeStatus(totalPaid, newNetExpected);

                await _dbContext.SaveChangesAsync(ct);

                return (record.StudentId, record.GroupId, newNetExpected);
            });

            // Notify the student (and linked parents) about the discount.
            var group = await _dbContext.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);
            string groupNameDiscount = group?.Name ?? "المجموعة";

            await _notificationService.SendStudentAndParentNotificationsAsync(
                new List<string> { studentId },
                new NotificationPayload(
                    title: "خصم جديد",
                    message: $"تم تطبيق خصم {amount:0.##} ج على دفعتك في ({groupNameDiscount}). المستحق عليك الآن: {newNet:0.##} ج.",
                    type: NotificationType.DiscountApplied,
                    route: "/student/payments"),
                parentPayloadFactory: (sid, name) => new NotificationPayload(
                    title: "خصم لابنك",
                    message: $"تم تطبيق خصم {amount:0.##} ج على دفعة {name} في ({groupNameDiscount}). المتبقي: {newNet:0.##} ج.",
                    type: NotificationType.DiscountApplied,
                    route: $"/parent/children/{sid}"),
                tenantId,
                ct);

            return $"Discount of {amount:0.##} applied. New balance: {newNet:0.##}.";
        }

        public async Task<string> RemoveDiscountAsync(
            Guid recordId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                var record = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.Id == recordId, ct)
                    ?? throw new NotFoundException(["Student payment record not found."]);

                if (record.DiscountAmount <= 0)
                    throw new ConflictException(["This record has no discount to remove."]);

                record.DiscountAmount = 0;
                record.DiscountReason = null;

                if (record.Status != PaymentStatus.Waived)
                {
                    decimal totalPaid = record.Transactions.Sum(t => t.Amount);
                    record.Status = ComputeStatus(totalPaid, record.ExpectedAmount);
                }

                await _dbContext.SaveChangesAsync(ct);

                return "Discount removed. Status recomputed against the full amount.";
            });
        }

        public async Task<string> RecalibrateCurrentCycleAsync(
            Guid groupId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.ExecuteWithConcurrencyRetryAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var group = await _dbContext.Groups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == groupId, ct)
                    ?? throw new NotFoundException(["Group not found."]);

                var cycle = await _dbContext.PaymentCycles
                    .FirstOrDefaultAsync(c => c.GroupId == groupId && !c.IsCompleted, ct)
                    ?? throw new ConflictException(["No active cycle to recalibrate."]);

                int newTarget = group.SessionsPerCycle ?? cycle.SessionsTarget;
                decimal newBase = group.MonthlyFee ?? cycle.BaseFee;

                if (newTarget < cycle.SessionsCompleted)
                    throw new ConflictException([
                        $"Cannot reduce sessions to {newTarget} — the cycle has already completed {cycle.SessionsCompleted} sessions. Close the cycle first."
                    ]);

                // Update the cycle's snapshot. ExtraFee is preserved (driven by AddToCycle sessions).
                cycle.BaseFee        = newBase;
                cycle.SessionsTarget = newTarget;

                decimal newExpectedForOpenRecords = newBase + cycle.ExtraFee;

                var records = await _dbContext.StudentPaymentRecords
                    .Include(r => r.Transactions)
                    .Where(r => r.PaymentCycleId == cycle.Id && r.Status != PaymentStatus.Waived)
                    .ToListAsync(ct);

                int updatedCount = 0;
                var notifyStudents = new List<(string studentId, decimal newRemaining)>();

                foreach (var record in records)
                {
                    decimal totalPaid = record.Transactions.Sum(t => t.Amount);
                    // Floor at (paid + discount) so we don't break the DB CHECK (Discount ≤ Expected)
                    // and so we never claim a negative balance.
                    decimal anchor = totalPaid + record.DiscountAmount;
                    decimal newExpected = Math.Max(newExpectedForOpenRecords, anchor);

                    if (newExpected == record.ExpectedAmount) continue;

                    record.ExpectedAmount = newExpected;
                    record.Status         = ComputeStatus(totalPaid, newExpected - record.DiscountAmount);
                    updatedCount++;

                    decimal remaining = Math.Max(0, (newExpected - record.DiscountAmount) - totalPaid);
                    notifyStudents.Add((record.StudentId, remaining));
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // Notify affected students after commit (with parent fan-out)
                string groupNameRecal = group.Name;
                foreach (var (studentId, remaining) in notifyStudents)
                {
                    await _notificationService.SendStudentAndParentNotificationsAsync(
                        new List<string> { studentId },
                        new NotificationPayload(
                            title: "تحديث الرسوم",
                            message: $"تم تحديث رسوم الدورة الحالية لمجموعة ({groupNameRecal}). المتبقي عليك الآن: {remaining:0.##} ج.",
                            type: NotificationType.PaymentDue,
                            route: "/student/payments"),
                        parentPayloadFactory: (sid, name) => new NotificationPayload(
                            title: "تحديث رسوم ابنك",
                            message: $"تم تحديث رسوم الدورة الحالية لـ {name} في مجموعة ({groupNameRecal}). المتبقي: {remaining:0.##} ج.",
                            type: NotificationType.PaymentDue,
                            route: $"/parent/children/{sid}"),
                        tenantId,
                        ct);
                }

                return $"Cycle recalibrated. {updatedCount} record(s) updated.";
            });
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════

        private static PaymentStatus ComputeStatus(decimal totalPaid, decimal expected)
        {
            if (expected <= 0)         return PaymentStatus.Paid;
            if (totalPaid <= 0)        return PaymentStatus.Unpaid;
            if (totalPaid >= expected) return PaymentStatus.Paid;
            return PaymentStatus.PartiallyPaid;
        }

        private async Task<PaginatedResult<StudentPaymentRecordDto>> BuildRecordsPageAsync(
            System.Linq.Expressions.Expression<Func<StudentPaymentRecord, bool>> predicate,
            PaymentStatus? filterStatus, int page, int pageSize, CancellationToken ct)
        {
            var baseQuery = _dbContext.StudentPaymentRecords
                .AsNoTracking()
                .Where(predicate);

            if (filterStatus.HasValue)
                baseQuery = baseQuery.Where(r => r.Status == filterStatus.Value);

            var totalCount = await baseQuery.CountAsync(ct);

            var records = await baseQuery
                .OrderBy(r => r.StudentId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.StudentId,
                    r.EnrolledAtSession,
                    r.ExpectedAmount,
                    r.DiscountAmount,
                    r.DiscountReason,
                    r.Status,
                    Transactions = r.Transactions
                        .OrderByDescending(t => t.PaidAt)
                        .Select(t => new PaymentTransactionDto
                        {
                            Id     = t.Id,
                            Amount = t.Amount,
                            PaidAt = t.PaidAt,
                            PaidBy = t.PaidBy,
                            Notes  = t.Notes
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            var studentIds = records.Select(r => r.StudentId).Distinct().ToList();
            var users = await _dbContext.Users
                .AsNoTracking()
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.PhoneNumber })
                .ToDictionaryAsync(u => u.Id, u => u, ct);

            var data = records.Select(r =>
            {
                users.TryGetValue(r.StudentId, out var user);
                decimal totalPaid = r.Transactions.Sum(t => t.Amount);
                return new StudentPaymentRecordDto
                {
                    RecordId         = r.Id,
                    StudentId        = r.StudentId,
                    StudentName      = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    PhoneNumber      = user?.PhoneNumber ?? string.Empty,
                    EnrolledAtSession = r.EnrolledAtSession,
                    ExpectedAmount    = r.ExpectedAmount,
                    DiscountAmount    = r.DiscountAmount,
                    DiscountReason    = r.DiscountReason,
                    TotalPaid         = totalPaid,
                    Status            = r.Status,
                    Transactions      = r.Transactions
                };
            }).ToList();

            return PaginatedResult<StudentPaymentRecordDto>.Create(data, totalCount, page, pageSize);
        }
    }
}
