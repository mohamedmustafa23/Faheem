using Application.Features.Groups.Commands;
using FluentValidation;

namespace Application.Features.Groups.Validators
{
    // A manually-added student must carry a name + stage + grade. Input validation
    // lives here (the Application layer), mirroring CreateGroupCommandValidator — the
    // service trusts the request once the pipeline has validated it.
    public class ManualAddStudentCommandValidator : AbstractValidator<ManualAddStudentCommand>
    {
        public ManualAddStudentCommandValidator()
        {
            RuleFor(x => x.Request.FirstName)
                .NotEmpty().WithMessage("اسم الطالب الأول مطلوب")
                .MaximumLength(50);

            RuleFor(x => x.Request.LastName)
                .NotEmpty().WithMessage("اسم عائلة الطالب مطلوب")
                .MaximumLength(50);

            RuleFor(x => x.Request.EducationalStage)
                .NotEmpty().WithMessage("لازم تختار المرحلة الدراسية للطالب")
                .MaximumLength(100);

            RuleFor(x => x.Request.GradeYear)
                .NotEmpty().WithMessage("لازم تختار الصف الدراسي للطالب")
                .MaximumLength(50);
        }
    }
}
