using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
    {
        public RecordPaymentCommandValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty().WithMessage("Record ID is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Amount exceeds the allowed maximum.");

            RuleFor(x => x.PaidAt)
                .Must(d => !d.HasValue || d.Value.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Payment date cannot be in the future.");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
        }
    }
}
