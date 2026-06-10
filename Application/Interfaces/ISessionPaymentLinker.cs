using Domain.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Bridges the Session lifecycle with the Payment domain.
    /// Owns the rules for what happens to payment records / cycles when a manual
    /// session is created, cancelled, or deleted.
    ///
    /// Methods participate in the caller's transaction — caller invokes SaveChangesAsync.
    /// Each method returns the list of student IDs whose financial state was actually
    /// modified, so the caller can fan out notifications after commit.
    /// </summary>
    public interface ISessionPaymentLinker
    {
        /// <summary>
        /// Attach an AddToCycle occurrence to the group's open cycle:
        ///   - cycle.ExtraFee       += price
        ///   - cycle.SessionsTarget += 1
        ///   - every open record (Unpaid / PartiallyPaid) → ExpectedAmount += price
        ///   - occurrence.PaymentCycleId = cycle.Id
        /// Returns the student IDs whose records were bumped.
        /// Throws ConflictException if no open cycle exists.
        /// </summary>
        Task<IReadOnlyList<string>> ApplyAddToCycleAsync(
            SessionOccurrence occurrence, decimal price, string tenantId, CancellationToken ct);

        /// <summary>
        /// Reverse a previously-applied AddToCycle:
        ///   - cycle.ExtraFee       -= price (floor 0)
        ///   - cycle.SessionsTarget -= 1 (floor 1)
        ///   - every open record (Unpaid / PartiallyPaid) → ExpectedAmount -= price (floor by total paid)
        ///   - Paid / Waived records are left untouched.
        /// Returns the student IDs whose records were reduced.
        /// Safe to call even if the cycle has been closed since.
        /// </summary>
        Task<IReadOnlyList<string>> RevertAddToCycleAsync(SessionOccurrence occurrence, CancellationToken ct);

        /// <summary>
        /// Create one StudentPaymentRecord per currently-enrolled student for a Standalone occurrence.
        /// Returns the seeded student IDs.
        /// </summary>
        Task<IReadOnlyList<string>> CreateStandalonePaymentsAsync(
            SessionOccurrence occurrence, decimal price, string tenantId, CancellationToken ct);

        /// <summary>
        /// Clean up payment records for a cancelled / soft-removed Standalone occurrence:
        ///   - records with no transactions → deleted
        ///   - records with transactions    → marked Waived (audit trail preserved)
        /// Returns the student IDs whose records were Waived (so they can be notified that
        /// their payment was preserved). Deleted records are not included.
        /// </summary>
        Task<IReadOnlyList<string>> CancelStandalonePaymentsAsync(SessionOccurrence occurrence, CancellationToken ct);

        /// <summary>
        /// Verifies that hard-deleting the occurrence will not destroy any settled payments.
        /// Throws ConflictException if any record for this occurrence has transactions.
        /// </summary>
        Task EnsureStandaloneSafeToDeleteAsync(SessionOccurrence occurrence, CancellationToken ct);

        /// <summary>
        /// Delete all StudentPaymentRecords tied to a Standalone occurrence.
        /// Caller must invoke <see cref="EnsureStandaloneSafeToDeleteAsync"/> first.
        /// </summary>
        Task DeleteStandalonePaymentsAsync(SessionOccurrence occurrence, CancellationToken ct);
    }
}
