namespace Application.Features.Payments.DTOs
{
    public class PaymentCycleDto
    {
        public Guid Id { get; set; }
        public int CycleNumber { get; set; }
        public int SessionsTarget { get; set; }
        public int SessionsCompleted { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        /// <summary>Snapshot of MonthlyFee when this cycle opened.</summary>
        public decimal BaseFee { get; set; }

        /// <summary>Cumulative extra amount from AddToCycle sessions.</summary>
        public decimal ExtraFee { get; set; }

        /// <summary>Total expected per student = BaseFee + ExtraFee.</summary>
        public decimal TotalFeePerStudent => BaseFee + ExtraFee;
    }
}
