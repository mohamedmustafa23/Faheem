using System.ComponentModel.DataAnnotations;

namespace Application.Features.Identity.DTOs
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [MaxLength(50, ErrorMessage = "الاسم الأول لا يتجاوز 50 حرفاً")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [MaxLength(50, ErrorMessage = "اسم العائلة لا يتجاوز 50 حرفاً")]
        public string LastName { get; set; } = string.Empty;
    }
}
