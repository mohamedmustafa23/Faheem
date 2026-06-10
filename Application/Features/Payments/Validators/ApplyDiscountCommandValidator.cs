using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
    {
        public ApplyDiscountCommandValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty().WithMessage("Record ID is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Discount amount must be greater than zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Discount amount exceeds the allowed maximum.");

            RuleFor(x => x.Reason)
                .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
        }
    }
}
