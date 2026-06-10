using Application.Features.Sessions.Commands;
using FluentValidation;

namespace Application.Features.Sessions.Validators
{
    public class UpdateManualOccurrenceCommandValidator : AbstractValidator<UpdateManualOccurrenceCommand>
    {
        public UpdateManualOccurrenceCommandValidator()
        {
            RuleFor(x => x.OccurrenceId)
                .NotEmpty().WithMessage("Occurrence ID is required.");

            RuleFor(x => x.OccurrenceDate)
                .NotEmpty().WithMessage("Occurrence date is required.");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("Start time must be before end time.");

            RuleFor(x => x)
                .Must(c =>
                {
                    var duration = c.EndTime - c.StartTime;
                    return duration.TotalMinutes >= 15 && duration.TotalHours <= 6;
                })
                .WithMessage("Session duration must be between 15 minutes and 6 hours.");
        }
    }
}
