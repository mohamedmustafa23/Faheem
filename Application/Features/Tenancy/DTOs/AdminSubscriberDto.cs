namespace Application.Features.Tenancy.DTOs
{
    // The admin control-center view of one subscriber (a teacher workspace or a
    // center): subscription status, owner contact for follow-up, and live counts.
    public class AdminSubscriberDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>"Individual" (single teacher) or "Center".</summary>
        public string Type { get; set; } = "Individual";

        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime ValidUpTo { get; set; }

        public int TeachersCount { get; set; }
        public int StudentsCount { get; set; }
        public int GroupsCount { get; set; }

        /// <summary>Seat limit — centers only. Null for individual workspaces.</summary>
        public int? MaxTeachers { get; set; }
    }

    public class ExtendSubscriptionRequest
    {
        public int Months { get; set; }

        /// <summary>Centers only — the teacher seat limit for this package. Ignored for individuals.</summary>
        public int? MaxTeachers { get; set; }
    }

    public class SetSubscriberActiveRequest
    {
        public bool IsActive { get; set; }
    }

    public class SetCenterSeatsRequest
    {
        /// <summary>Teacher seat limit. Null = unlimited.</summary>
        public int? MaxTeachers { get; set; }
    }
}
