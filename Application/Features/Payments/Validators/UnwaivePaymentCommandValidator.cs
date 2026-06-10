using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class UnwaivePaymentCommandValidator : AbstractValidator<UnwaivePaymentCommand>
    {
        public UnwaivePaymentCommandValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty().WithMessage("Record ID is required.");
        }
    }
}
