using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Enums;
using MediatR;

namespace Application.Features.Payments.Queries
{
    public class GetStandaloneOccurrenceRecordsQuery : IRequest<Result<PaginatedResult<StudentPaymentRecordDto>>>
    {
        public Guid OccurrenceId { get; set; }
        public PaymentStatus? FilterStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetStandaloneOccurrenceRecordsQueryHandler : IRequestHandler<GetStandaloneOccurrenceRecordsQuery, Result<PaginatedResult<StudentPaymentRecordDto>>>
    {
        private readonly IPaymentService _paymentService;
        public GetStandaloneOccurrenceRecordsQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<PaginatedResult<StudentPaymentRecordDto>>> Handle(GetStandaloneOccurrenceRecordsQuery query, CancellationToken cancellationToken)
        {
            var data = await _paymentService.GetStandaloneOccurrenceRecordsAsync(query.OccurrenceId, query.FilterStatus, query.Page, query.PageSize, cancellationToken);
            return Result<PaginatedResult<StudentPaymentRecordDto>>.Success(data);
        }
    }
}
