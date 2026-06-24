namespace Application.Features.Groups.DTOs
{
    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;
        public int? MaxStudents { get; set; }
        public int? SessionsPerCycle { get; set; }
        /// <summary>Fee per payment cycle in local currency (e.g. 500 EGP).</summary>
        public decimal? MonthlyFee { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Center only: the teacher this group belongs to. When a center owner/staff creates
        /// a group on a teacher's behalf, this stamps <c>Group.OwnerUserId</c> so payout and
        /// group scoping stay correct. Ignored when the caller is a plain member teacher
        /// (they can only create their own groups) and when omitted (defaults to the caller).
        /// </summary>
        public string? OwnerTeacherId { get; set; }
    }
}
