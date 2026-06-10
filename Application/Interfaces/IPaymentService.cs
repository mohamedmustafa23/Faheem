using Application.Features.Payments.DTOs;
using Application.Wrappers;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IPaymentService
    {
        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>All payment cycles for a group (newest first).</summary>
        Task<PaginatedResult<PaymentCycleDto>> GetGroupCyclesAsync(
            Guid groupId, int page = 1, int pageSize = 20, CancellationToken ct = default);

        /// <summary>Student payment records for a regular cycle.</summary>
        Task<PaginatedResult<StudentPaymentRecordDto>> GetCycleStudentRecordsAsync(
            Guid cycleId, PaymentStatus? filterStatus = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

        /// <summary>Student payment records for a standalone session occurrence.</summary>
        Task<PaginatedResult<StudentPaymentRecordDto>> GetStandaloneOccurrenceRecordsAsync(
            Guid occurrenceId, PaymentStatus? filterStatus = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

        /// <summary>Combined financial summary for a group — cycles + standalone occurrences.</summary>
        Task<GroupFinancialSummaryDto> GetGroupFinancialSummaryAsync(
            Guid groupId, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Grand-total finance picture for the teacher: sums every record
        /// across every group and every cycle (open + closed + standalones)
        /// into a single overview + per-group breakdown. Drives the headline
        /// finance card on the teacher's finance tab.
        /// </summary>
        Task<TeacherFinancialOverviewDto> GetTeacherFinancialOverviewAsync(
            string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Student-facing view: for the logged-in student, list every group they're enrolled in
        /// with the open cycle's record (if any) + all their standalone records, plus grand totals.
        /// Waived records contribute 0 to expected/remaining.
        /// </summary>
        Task<StudentPaymentsOverviewDto> GetMyPaymentsAsync(
            string studentId, CancellationToken ct = default);

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Add a payment transaction to a student's record (concurrency-safe).</summary>
        Task<string> RecordPaymentAsync(
            RecordPaymentRequest request, string tenantId, CancellationToken ct = default);

        /// <summary>Remove a payment transaction (concurrency-safe).</summary>
        Task<string> DeletePaymentTransactionAsync(
            Guid transactionId, string tenantId, CancellationToken ct = default);

        /// <summary>Manually close the current open cycle and open a new one.</summary>
        Task<string> CloseCycleManuallyAsync(
            Guid cycleId, string tenantId, CancellationToken ct = default);

        /// <summary>Waive a student's payment obligation for a record.</summary>
        Task<string> WaivePaymentRecordAsync(
            Guid recordId, string tenantId, CancellationToken ct = default);

        /// <summary>Reverse a previously-applied waive — restores Unpaid / PartiallyPaid / Paid.</summary>
        Task<string> UnwaivePaymentRecordAsync(
            Guid recordId, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Apply (or replace) a discount on a record. Net expected = ExpectedAmount - amount.
        /// Status is recomputed against the discounted total.
        /// </summary>
        Task<string> ApplyDiscountAsync(
            Guid recordId, decimal amount, string? reason, string tenantId, CancellationToken ct = default);

        /// <summary>Remove an existing discount and re-resolve the record status.</summary>
        Task<string> RemoveDiscountAsync(
            Guid recordId, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Re-apply the group's current MonthlyFee + SessionsPerCycle to the open cycle and
        /// every non-waived record in it. Records keep their existing payments and discounts;
        /// only their ExpectedAmount is updated to (MonthlyFee + cycle.ExtraFee).
        /// Paid records whose new total is below what they already paid stay anchored at
        /// (TotalPaid + Discount) so we never report a negative balance.
        /// </summary>
        Task<string> RecalibrateCurrentCycleAsync(
            Guid groupId, string tenantId, CancellationToken ct = default);
    }
}
