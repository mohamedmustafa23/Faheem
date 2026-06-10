using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    public class Group : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;
        public int? MaxStudents { get; set; }
        public int? SessionsPerCycle { get; set; }
        /// <summary>Fee amount per payment cycle (e.g. 500 EGP per month).</summary>
        public decimal? MonthlyFee { get; set; }
        public string? Description { get; set; }

        public string EnrollmentCode { get; set; } = string.Empty;

        public GroupStatus Status { get; set; } = GroupStatus.Active;

        public string TenantId { get; set; } = string.Empty;

        /// <summary>Teacher-pinned groups bubble up to the top of their groups list.</summary>
        public bool IsPinned { get; set; } = false;

        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<GroupStudent> Students { get; set; } = new List<GroupStudent>();
        public ICollection<PaymentCycle> PaymentCycles { get; set; } = new List<PaymentCycle>();
    }
}
