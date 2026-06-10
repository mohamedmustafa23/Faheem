using Application.Features.Sessions.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Sessions.Validators
{
    public class CreateManualOccurrenceCommandValidator : AbstractValidator<CreateManualOccurrenceCommand>
    {
        public CreateManualOccurrenceCommandValidator()
        {
            RuleFor(x => x.Request.GroupId)
                .NotEmpty().WithMessage("Group ID is required.");

            RuleFor(x => x.Request.OccurrenceDate)
                .NotEmpty().WithMessage("Occurrence date is required.");

            RuleFor(x => x.Request.StartTime)
                .LessThan(x => x.Request.EndTime)
                .WithMessage("Start time must be before end time.");

            RuleFor(x => x.Request)
                .Must(r =>
                {
                    var duration = r.EndTime - r.StartTime;
                    return duration.TotalMinutes >= 15 && duration.TotalHours <= 6;
                })
                .WithMessage("Session duration must be between 15 minutes and 6 hours.");

            RuleFor(x => x.Request.SessionPrice)
                .NotNull().WithMessage("Session price is required.")
                .GreaterThan(0).WithMessage("Session price must be greater than zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Session price exceeds the allowed maximum.")
                .When(x => x.Request.PaymentMode == SessionPaymentMode.Standalone
                         || x.Request.PaymentMode == SessionPaymentMode.AddToCycle);

            RuleFor(x => x.Request.SessionPrice)
                .Null().WithMessage("Free sessions cannot have a price.")
                .When(x => x.Request.PaymentMode == SessionPaymentMode.Free);

            RuleFor(x => x.Request.PaymentMode)
                .IsInEnum().WithMessage("Invalid payment mode.");
        }
    }
}
