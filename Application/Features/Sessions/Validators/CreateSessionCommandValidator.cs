using Application.Features.Sessions.Commands;
using Application.Features.Sessions.DTOs;
using FluentValidation;

namespace Application.Features.Sessions.Validators
{
    public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
    {
        public CreateSessionCommandValidator()
        {
            RuleFor(x => x.Request.GroupId)
                .NotEmpty().WithMessage("Group ID is required.");

            RuleFor(x => x.Request.TimeSlots)
                .NotEmpty().WithMessage("You must provide at least one time slot.");

            RuleForEach(x => x.Request.TimeSlots).SetValidator(new SessionTimeSlotValidator());
        }
    }

    public class SessionTimeSlotValidator : AbstractValidator<SessionTimeSlot>
    {
        public SessionTimeSlotValidator()
        {
            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage("Invalid day of the week.");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("Start time must be before end time.");

            RuleFor(x => x)
                .Must(slot =>
                {
                    var duration = slot.EndTime - slot.StartTime;
                    return duration.TotalMinutes >= 15 && duration.TotalHours <= 6;
                })
                .WithMessage("Session duration must be between 15 minutes and 6 hours.");
        }
    }
}
