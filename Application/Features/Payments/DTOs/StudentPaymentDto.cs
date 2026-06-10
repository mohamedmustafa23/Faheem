using Domain.Enums;

namespace Application.Features.Payments.DTOs
{
    public class StudentPaymentRecordDto
    {
        public Guid RecordId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>SessionsCompleted value when this student was enrolled.</summary>
        public int EnrolledAtSession { get; set; }

        /// <summary>List price before any discount.</summary>
        public decimal ExpectedAmount { get; set; }

        /// <summary>Teacher-granted discount on this record (≥ 0, ≤ ExpectedAmount).</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>Optional reason describing why the discount was granted.</summary>
        public string? DiscountReason { get; set; }

        /// <summary>ExpectedAmount − DiscountAmount — the real amount the student owes.</summary>
        public decimal NetExpected => ExpectedAmount - DiscountAmount;

        public decimal TotalPaid { get; set; }

        /// <summary>NetExpected − TotalPaid, floored at 0.</summary>
        public decimal Remaining => Math.Max(0, NetExpected - TotalPaid);

        public PaymentStatus Status { get; set; }

        public List<PaymentTransactionDto> Transactions { get; set; } = [];
    }
}
