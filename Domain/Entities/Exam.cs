using Domain.Contracts;

namespace Domain.Entities
{
    public class Exam : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal MaxScore { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public ICollection<StudentGrade> StudentGrades { get; set; } = new List<StudentGrade>();
    }
}
