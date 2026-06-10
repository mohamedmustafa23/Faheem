using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Queries
{
    public class GetGroupFinancialSummaryQuery : IRequest<Result<GroupFinancialSummaryDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetGroupFinancialSummaryQueryHandler : IRequestHandler<GetGroupFinancialSummaryQuery, Result<GroupFinancialSummaryDto>>
    {
        private readonly IPaymentService _paymentService;
        public GetGroupFinancialSummaryQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<GroupFinancialSummaryDto>> Handle(GetGroupFinancialSummaryQuery query, CancellationToken cancellationToken)
        {
            var data = await _paymentService.GetGroupFinancialSummaryAsync(query.GroupId, query.TenantId, cancellationToken);
            return Result<GroupFinancialSummaryDto>.Success(data);
        }
    }
}
