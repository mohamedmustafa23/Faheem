using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Queries
{
    /// <summary>
    /// Returns the grand-total finance report for the current teacher: every
    /// group, every cycle, every standalone — summed into headline totals
    /// plus a per-group breakdown.
    /// </summary>
    public class GetTeacherFinancialOverviewQuery : IRequest<Result<TeacherFinancialOverviewDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetTeacherFinancialOverviewQueryHandler
        : IRequestHandler<GetTeacherFinancialOverviewQuery, Result<TeacherFinancialOverviewDto>>
    {
        private readonly IPaymentService _paymentService;
        public GetTeacherFinancialOverviewQueryHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result<TeacherFinancialOverviewDto>> Handle(GetTeacherFinancialOverviewQuery query, CancellationToken cancellationToken)
        {
            var data = await _paymentService.GetTeacherFinancialOverviewAsync(query.TenantId, cancellationToken);
            return Result<TeacherFinancialOverviewDto>.Success(data);
        }
    }
}
