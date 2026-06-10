using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class RecalibrateCycleCommandValidator : AbstractValidator<RecalibrateCycleCommand>
    {
        public RecalibrateCycleCommandValidator()
        {
            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required.");
        }
    }
}
