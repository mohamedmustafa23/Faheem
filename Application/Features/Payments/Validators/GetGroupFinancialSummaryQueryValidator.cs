using Application.Features.Payments.Queries;
using FluentValidation;

namespace Application.Features.Payments.Validators
{
    public class GetGroupFinancialSummaryQueryValidator : AbstractValidator<GetGroupFinancialSummaryQuery>
    {
        public GetGroupFinancialSummaryQueryValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty().WithMessage("Group ID is required.");
        }
    }
}
