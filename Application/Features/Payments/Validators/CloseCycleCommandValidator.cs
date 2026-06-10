using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class CloseCycleCommandValidator : AbstractValidator<CloseCycleCommand>
    {
        public CloseCycleCommandValidator()
        {
            RuleFor(x => x.CycleId)
                .NotEmpty().WithMessage("Cycle ID is required.");
        }
    }
}
