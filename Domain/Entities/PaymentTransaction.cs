using Domain.Contracts;

namespace Domain.Entities
{
    /// <summary>
    /// A single payment instalment made by (or on behalf of) a student
    /// against a StudentPaymentRecord.
    /// </summary>
    public class PaymentTransaction : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid StudentPaymentRecordId { get; set; }
        public StudentPaymentRecord PaymentRecord { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who physically made the payment. Null = the student themselves.
        /// Future: populate with a parent's UserId when parent-payment feature is added.
        /// </summary>
        public string? PaidBy { get; set; }

        public string? Notes { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}
