using Application.Features.Sessions.Queries;
using FluentValidation;

namespace Application.Features.Sessions.Validators
{
    public class GetTodayScheduleQueryValidator : AbstractValidator<GetTodayScheduleQuery>
    {
        public GetTodayScheduleQueryValidator()
        {
            // TodayDate is optional; defaults to today (UTC) when not provided
        }
    }
}
