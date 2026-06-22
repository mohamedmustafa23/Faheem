namespace Application.Features.Centers.DTOs
{
    public class InviteTeacherRequest
    {
        /// <summary>Phone number or email of an existing registered user to invite into the center.</summary>
        public string PhoneOrEmail { get; set; } = string.Empty;
    }
}
