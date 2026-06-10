using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Payments.Queries
{
    public class GetGroupCyclesQuery : IRequest<Result<PaginatedResult<PaymentCycleDto>>>
    {
        public Guid GroupId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetGroupCyclesQueryHandler : IRequestHandler<GetGroupCyclesQuery, Result<PaginatedResult<PaymentCycleDto>>>
    {
        private readonly IPaymentService _paymentService;
        public GetGroupCyclesQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<PaginatedResult<PaymentCycleDto>>> Handle(GetGroupCyclesQuery query, CancellationToken cancellationToken)
        {
            var data = await _paymentService.GetGroupCyclesAsync(query.GroupId, query.Page, query.PageSize, cancellationToken);
            return Result<PaginatedResult<PaymentCycleDto>>.Success(data);
        }
    }
}
