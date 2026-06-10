using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Groups.DTOs
{
    public class ManualAddStudentBodyRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;

        // Optional: the parent's phone number. When provided, the manually-added
        // student is linked to that parent's account right away.
        public string? ParentPhoneNumber { get; set; }
    }
}
