using Domain.Enums;

namespace Application.Features.Attendance.DTOs
{
    public class GroupOccurrenceDto
    {
        public Guid OccurrenceId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsManual { get; set; }
        public bool CountsForPayment { get; set; }

        /// <summary>For manual occurrences: how the session is billed.</summary>
        public SessionPaymentMode PaymentMode { get; set; }

        /// <summary>For Standalone / AddToCycle manual occurrences only.</summary>
        public decimal? SessionPrice { get; set; }

        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int ExcusedCount { get; set; }
    }
}
