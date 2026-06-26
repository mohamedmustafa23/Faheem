using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    public class GetCenterOverviewQuery : IRequest<Result<CenterOverviewDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
    }

    public class GetCenterOverviewQueryHandler : IRequestHandler<GetCenterOverviewQuery, Result<CenterOverviewDto>>
    {
        private readonly ICenterService _centerService;
        public GetCenterOverviewQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<CenterOverviewDto>> Handle(GetCenterOverviewQuery query, CancellationToken cancellationToken)
        {
            var overview = await _centerService.GetCenterOverviewAsync(query.TenantId, query.OwnerUserId, cancellationToken);
            return Result<CenterOverviewDto>.Success(overview);
        }
    }
}
