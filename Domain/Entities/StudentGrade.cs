using Domain.Contracts;

namespace Domain.Entities
{
    public class StudentGrade : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid ExamId { get; set; }
        public Exam Exam { get; set; } = null!;

        public string StudentId { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}