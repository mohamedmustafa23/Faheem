using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class SessionPaymentLinker : ISessionPaymentLinker
    {
        private readonly ApplicationDbContext _dbContext;

        public SessionPaymentLinker(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<string>> ApplyAddToCycleAsync(
            SessionOccurrence occurrence, decimal price, string tenantId, CancellationToken ct)
        {
            if (price <= 0)
                throw new ConflictException(["AddToCycle price must be greater than zero."]);

            var openCycle = await _dbContext.PaymentCycles
                .FirstOrDefaultAsync(c => c.GroupId == occurrence.GroupId && !c.IsCompleted, ct)
                ?? throw new ConflictException([
                    "No open payment cycle on this group. Open or close a cycle first."
                ]);

            openCycle.ExtraFee       += price;
            openCycle.SessionsTarget += 1;

            // Charge every enrolled student who isn't waived for this cycle —
            // INCLUDING those who had already fully paid. Their ExpectedAmount
            // goes up by the extra session's price, which re-resolves them to
            // PartiallyPaid so they now owe the difference. (Waived students were
            // forgiven for the cycle, so they stay waived and aren't charged.)
            var affectedRecords = await _dbContext.StudentPaymentRecords
                .Include(r => r.Transactions)
                .Where(r => r.PaymentCycleId == openCycle.Id
                         && r.Status != PaymentStatus.Waived)
                .ToListAsync(ct);

            foreach (var record in affectedRecords)
            {
                record.ExpectedAmount += price;
                decimal totalPaid = record.Transactions.Sum(t => t.Amount);
                record.Status = ResolveStatus(totalPaid, record.ExpectedAmount - record.DiscountAmount);
            }

            occurrence.PaymentCycleId = openCycle.Id;

            return affectedRecords.Select(r => r.StudentId).ToList();
        }

        public async Task<IReadOnlyList<string>> RevertAddToCycleAsync(SessionOccurrence occurrence, CancellationToken ct)
        {
            if (occurrence.PaymentMode != SessionPaymentMode.AddToCycle || occurrence.SessionPrice is null)
                return Array.Empty<string>();

            decimal price = occurrence.SessionPrice.Value;

            // Use the captured source cycle if available — falls back to current open cycle
            // for occurrences created before PaymentCycleId existed.
            var cycle = occurrence.PaymentCycleId.HasValue
                ? await _dbContext.PaymentCycles
                    .FirstOrDefaultAsync(c => c.Id == occurrence.PaymentCycleId.Value, ct)
                : await _dbContext.PaymentCycles
                    .FirstOrDefaultAsync(c => c.GroupId == occurrence.GroupId && !c.IsCompleted, ct);

            if (cycle == null) return Array.Empty<string>();

            cycle.ExtraFee       = Math.Max(0, cycle.ExtraFee - price);
            cycle.SessionsTarget = Math.Max(1, cycle.SessionsTarget - 1);

            var openRecords = await _dbContext.StudentPaymentRecords
                .Include(r => r.Transactions)
                .Where(r => r.PaymentCycleId == cycle.Id
                         && (r.Status == PaymentStatus.Unpaid || r.Status == PaymentStatus.PartiallyPaid))
                .ToListAsync(ct);

            foreach (var record in openRecords)
            {
                decimal totalPaid   = record.Transactions.Sum(t => t.Amount);
                decimal discount    = record.DiscountAmount;
                // Never push ExpectedAmount below (totalPaid + discount) so NetExpected stays ≥ totalPaid.
                decimal newExpected = Math.Max(totalPaid + discount, record.ExpectedAmount - price);
                record.ExpectedAmount = newExpected;

                if (record.Status != PaymentStatus.Waived)
                    record.Status = ResolveStatus(totalPaid, newExpected - discount);
            }

            return openRecords.Select(r => r.StudentId).ToList();
        }

        public async Task<IReadOnlyList<string>> CreateStandalonePaymentsAsync(
            SessionOccurrence occurrence, decimal price, string tenantId, CancellationToken ct)
        {
            if (price <= 0)
                throw new ConflictException(["Standalone session price must be greater than zero."]);

            var enrolledStudentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == occurrence.GroupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            foreach (var sid in enrolledStudentIds)
            {
                _dbContext.StudentPaymentRecords.Add(new StudentPaymentRecord
                {
                    StudentId      = sid,
                    GroupId        = occurrence.GroupId,
                    OccurrenceId   = occurrence.Id,
                    ExpectedAmount = price,
                    Status         = PaymentStatus.Unpaid,
                    TenantId       = tenantId
                });
            }

            return enrolledStudentIds;
        }

        public async Task<IReadOnlyList<string>> CancelStandalonePaymentsAsync(SessionOccurrence occurrence, CancellationToken ct)
        {
            if (occurrence.PaymentMode != SessionPaymentMode.Standalone)
                return Array.Empty<string>();

            var records = await _dbContext.StudentPaymentRecords
                .Include(r => r.Transactions)
                .Where(r => r.OccurrenceId == occurrence.Id)
                .ToListAsync(ct);

            var waivedStudentIds = new List<string>();

            foreach (var record in records)
            {
                if (!record.Transactions.Any())
                {
                    _dbContext.StudentPaymentRecords.Remove(record);
                }
                else if (record.Status != PaymentStatus.Waived)
                {
                    record.Status = PaymentStatus.Waived;
                    waivedStudentIds.Add(record.StudentId);
                }
            }

            return waivedStudentIds;
        }

        public async Task EnsureStandaloneSafeToDeleteAsync(SessionOccurrence occurrence, CancellationToken ct)
        {
            if (occurrence.PaymentMode != SessionPaymentMode.Standalone)
                return;

            var hasTransactions = await _dbContext.StudentPaymentRecords
                .Where(r => r.OccurrenceId == occurrence.Id)
                .AnyAsync(r => r.Transactions.Any(), ct);

            if (hasTransactions)
                throw new ConflictException([
                    "Cannot delete a standalone session with recorded payments. Refund the transactions first, or cancel the session instead."
                ]);
        }

        public async Task DeleteStandalonePaymentsAsync(SessionOccurrence occurrence, CancellationToken ct)
        {
            if (occurrence.PaymentMode != SessionPaymentMode.Standalone)
                return;

            var records = await _dbContext.StudentPaymentRecords
                .Where(r => r.OccurrenceId == occurrence.Id)
                .ToListAsync(ct);

            if (records.Count > 0)
                _dbContext.StudentPaymentRecords.RemoveRange(records);
        }

        private static PaymentStatus ResolveStatus(decimal totalPaid, decimal expected)
        {
            if (expected <= 0)         return PaymentStatus.Paid;
            if (totalPaid <= 0)        return PaymentStatus.Unpaid;
            if (totalPaid >= expected) return PaymentStatus.Paid;
            return PaymentStatus.PartiallyPaid;
        }
    }
}
