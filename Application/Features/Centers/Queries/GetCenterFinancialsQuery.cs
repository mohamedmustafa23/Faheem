using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    public class GetCenterFinancialsQuery : IRequest<Result<CenterFinancialsDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
    }

    public class GetCenterFinancialsQueryHandler : IRequestHandler<GetCenterFinancialsQuery, Result<CenterFinancialsDto>>
    {
        private readonly ICenterService _centerService;
        public GetCenterFinancialsQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<CenterFinancialsDto>> Handle(GetCenterFinancialsQuery query, CancellationToken cancellationToken)
        {
            var data = await _centerService.GetCenterFinancialsAsync(query.TenantId, query.OwnerUserId, cancellationToken);
            return Result<CenterFinancialsDto>.Success(data);
        }
    }
}
