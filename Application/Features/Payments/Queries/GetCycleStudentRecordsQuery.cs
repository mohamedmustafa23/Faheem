using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Enums;
using MediatR;

namespace Application.Features.Payments.Queries
{
    public class GetCycleStudentRecordsQuery : IRequest<Result<PaginatedResult<StudentPaymentRecordDto>>>
    {
        public Guid CycleId { get; set; }
        public PaymentStatus? FilterStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetCycleStudentRecordsQueryHandler : IRequestHandler<GetCycleStudentRecordsQuery, Result<PaginatedResult<StudentPaymentRecordDto>>>
    {
        private readonly IPaymentService _paymentService;
        public GetCycleStudentRecordsQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<PaginatedResult<StudentPaymentRecordDto>>> Handle(GetCycleStudentRecordsQuery query, CancellationToken cancellationToken)
        {
            var data = await _paymentService.GetCycleStudentRecordsAsync(query.CycleId, query.FilterStatus, query.Page, query.PageSize, cancellationToken);
            return Result<PaginatedResult<StudentPaymentRecordDto>>.Success(data);
        }
    }
}
