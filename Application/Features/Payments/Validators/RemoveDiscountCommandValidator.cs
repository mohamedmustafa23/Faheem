using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class RemoveDiscountCommandValidator : AbstractValidator<RemoveDiscountCommand>
    {
        public RemoveDiscountCommandValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty().WithMessage("Record ID is required.");
        }
    }
}
