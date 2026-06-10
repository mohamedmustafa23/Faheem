using Application.Features.Payments.Commands;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class DeletePaymentTransactionCommandValidator : AbstractValidator<DeletePaymentTransactionCommand>
    {
        public DeletePaymentTransactionCommandValidator()
        {
            RuleFor(x => x.TransactionId)
                .NotEmpty().WithMessage("Transaction ID is required.");
        }
    }
}
