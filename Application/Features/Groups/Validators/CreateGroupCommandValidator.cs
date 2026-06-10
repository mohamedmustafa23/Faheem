using Application.Features.Groups.Commands;
using FluentValidation;

namespace Application.Features.Groups.Validators
{
    public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
    {
        public CreateGroupCommandValidator()
        {
            RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.Subject).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.EducationalStage).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.GradeYear).NotEmpty().MaximumLength(50);

            RuleFor(x => x.Request.SessionsPerCycle)
                .NotNull().WithMessage("Sessions per cycle is required.")
                .GreaterThan(0).WithMessage("Sessions per cycle must be greater than zero.")
                .LessThanOrEqualTo(500).WithMessage("Sessions per cycle exceeds the allowed maximum.");

            RuleFor(x => x.Request.MonthlyFee)
                .NotNull().WithMessage("Monthly fee is required.")
                .GreaterThan(0).WithMessage("Monthly fee must be greater than zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Monthly fee exceeds the allowed maximum.");

            RuleFor(x => x.Request.MaxStudents)
                .GreaterThan(0).WithMessage("Max students must be greater than zero.")
                .LessThanOrEqualTo(10_000).WithMessage("Max students exceeds the allowed maximum.")
                .When(x => x.Request.MaxStudents.HasValue);
        }
    }
}
