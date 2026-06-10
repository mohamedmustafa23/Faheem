using Application.Features.Payments.Queries;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class GetGroupCyclesQueryValidator : AbstractValidator<GetGroupCyclesQuery>
    {
        public GetGroupCyclesQueryValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty().WithMessage("Group ID is required.");
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        }
    }
}
