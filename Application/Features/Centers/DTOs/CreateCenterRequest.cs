namespace Application.Features.Centers.DTOs
{
    public class CreateCenterRequest
    {
        /// <summary>Display name of the center workspace.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Phone number or email of an EXISTING registered user who will own the center.
        /// They become the center's Owner (manages members + subscription).
        /// </summary>
        public string OwnerPhoneOrEmail { get; set; } = string.Empty;

        /// <summary>Seat limit for member teachers. Null = unlimited.</summary>
        public int? MaxTeachers { get; set; }

        /// <summary>Subscription valid-until date. Defaults to one month from now when omitted.</summary>
        public DateTime? ValidUpTo { get; set; }
    }
}
