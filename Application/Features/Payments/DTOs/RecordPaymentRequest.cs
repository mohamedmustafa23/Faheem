namespace Application.Features.Payments.DTOs
{
    public class RecordPaymentRequest
    {
        /// <summary>The StudentPaymentRecord to add this transaction to.</summary>
        public Guid RecordId { get; set; }

        public decimal Amount { get; set; }

        /// <summary>When the payment was made. Defaults to UtcNow if not provided.</summary>
        public DateTime? PaidAt { get; set; }

        public string? Notes { get; set; }
    }
}
