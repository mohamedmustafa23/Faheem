using Application.Features.Grades.Commands;
using FluentValidation;

namespace Application.Features.Grades.Validators
{
    public class SaveGradesCommandValidator : AbstractValidator<SaveGradesCommand>
    {
        public SaveGradesCommandValidator()
        {
            RuleFor(x => x.Request.ExamId).NotEmpty().WithMessage("Exam ID is required.");
            RuleFor(x => x.Request.StudentScores).NotEmpty().WithMessage("You must provide scores for students.");

            RuleForEach(x => x.Request.StudentScores).ChildRules(student =>
            {
                student.RuleFor(s => s.Score)
                    .GreaterThanOrEqualTo(0).WithMessage("Score cannot be negative.");
            });
        }
    }
}