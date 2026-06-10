using Application.Features.Sessions.Commands;
using FluentValidation;

namespace Application.Features.Sessions.Validators
{
    public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
    {
        public UpdateSessionCommandValidator()
        {
            RuleFor(x => x.ScheduleId)
                .NotEmpty().WithMessage("Schedule ID is required.");

            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage("Invalid day of the week.");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("Start time must be before end time.");

            RuleFor(x => x)
                .Must(cmd =>
                {
                    var duration = cmd.EndTime - cmd.StartTime;
                    return duration.TotalMinutes >= 15 && duration.TotalHours <= 6;
                })
                .WithMessage("Session duration must be between 15 minutes and 6 hours.");
        }
    }
}
