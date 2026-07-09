using Application.Features.LessonReports.Commands;
using FluentValidation;

namespace Application.Features.LessonReports.Validators
{
    // The whole report is optional, so we only guard the structural bits: a valid
    // occurrence and length caps that match the column sizes. Empty topic/homework/
    // entries are all allowed — the service treats them as "nothing to add".
    public class SaveLessonReportCommandValidator : AbstractValidator<SaveLessonReportCommand>
    {
        public SaveLessonReportCommandValidator()
        {
            RuleFor(x => x.Request.OccurrenceId)
                .NotEmpty().WithMessage("الحصة مطلوبة");

            RuleFor(x => x.Request.LessonTopic)
                .MaximumLength(300);

            RuleFor(x => x.Request.Homework)
                .MaximumLength(500);

            RuleForEach(x => x.Request.Entries).ChildRules(entry =>
            {
                entry.RuleFor(e => e.StudentId)
                    .NotEmpty().WithMessage("معرّف الطالب مطلوب");

                entry.RuleFor(e => e.Note)
                    .MaximumLength(500);
            });
        }
    }
}
