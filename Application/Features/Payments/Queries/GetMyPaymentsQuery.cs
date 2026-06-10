using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Queries
{
    /// <summary>
    /// "ماليتي" — student-facing aggregate of all groups they're enrolled in
    /// with their open cycle record + standalone records and grand totals.
    /// </summary>
    public class GetMyPaymentsQuery : IRequest<Result<StudentPaymentsOverviewDto>>
    {
        [JsonIgnore]
        public string StudentId { get; set; } = string.Empty;
    }

    public class GetMyPaymentsQueryHandler : IRequestHandler<GetMyPaymentsQuery, Result<StudentPaymentsOverviewDto>>
    {
        private readonly IPaymentService _paymentService;
        public GetMyPaymentsQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<StudentPaymentsOverviewDto>> Handle(GetMyPaymentsQuery query, CancellationToken cancellationToken)
        {
            var overview = await _paymentService.GetMyPaymentsAsync(query.StudentId, cancellationToken);
            return Result<StudentPaymentsOverviewDto>.Success(overview);
        }
    }
}
