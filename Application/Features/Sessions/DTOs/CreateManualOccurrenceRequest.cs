using Domain.Enums;

namespace Application.Features.Sessions.DTOs
{
    public class CreateManualOccurrenceRequest
    {
        public Guid GroupId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Free = gift/bonus session, no payment.
        /// Standalone = own price, separate payment cycle.
        /// AddToCycle = extra amount added to the current open monthly cycle.
        /// </summary>
        public SessionPaymentMode PaymentMode { get; set; } = SessionPaymentMode.Free;

        /// <summary>Required when PaymentMode is Standalone or AddToCycle.</summary>
        public decimal? SessionPrice { get; set; }
    }
}
