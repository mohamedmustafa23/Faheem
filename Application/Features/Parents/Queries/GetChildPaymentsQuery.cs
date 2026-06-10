using Application.Exceptions;
using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Payments overview for one of the parent's linked children — same shape
    /// as the student's "ماليتي" screen. Reuses the existing aggregation.
    /// </summary>
    public class GetChildPaymentsQuery : IRequest<Result<StudentPaymentsOverviewDto>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
    }

    public class GetChildPaymentsQueryHandler : IRequestHandler<GetChildPaymentsQuery, Result<StudentPaymentsOverviewDto>>
    {
        private readonly IPaymentService _paymentService;
        private readonly IParentService _parentService;

        public GetChildPaymentsQueryHandler(IPaymentService paymentService, IParentService parentService)
        {
            _paymentService = paymentService;
            _parentService = parentService;
        }

        public async Task<Result<StudentPaymentsOverviewDto>> Handle(GetChildPaymentsQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's payments."]);

            var data = await _paymentService.GetMyPaymentsAsync(query.ChildId, cancellationToken);
            return Result<StudentPaymentsOverviewDto>.Success(data);
        }
    }
}
