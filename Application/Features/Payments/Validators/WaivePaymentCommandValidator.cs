using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class WaivePaymentCommandValidator : AbstractValidator<WaivePaymentCommand>
    {
        public WaivePaymentCommandValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty().WithMessage("Record ID is required.");
        }
    }
}
