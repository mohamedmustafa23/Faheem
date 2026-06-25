namespace Application.Features.Centers.DTOs
{
    public class SetTeacherShareRequest
    {
        /// <summary>The center's cut of this teacher's revenue (0–100). Null clears it.</summary>
        public decimal? SharePercent { get; set; }
    }
}
