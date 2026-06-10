using Application.Features.Materials.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Materials.Queries
{
    public class GetGroupMaterialsQuery : IRequest<Result<List<MaterialResponseDto>>>
    {
        public Guid GroupId { get; set; }
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class GetGroupMaterialsQueryHandler : IRequestHandler<GetGroupMaterialsQuery, Result<List<MaterialResponseDto>>>
    {
        private readonly IMaterialService _materialService;
        public GetGroupMaterialsQueryHandler(IMaterialService materialService) => _materialService = materialService;

        public async Task<Result<List<MaterialResponseDto>>> Handle(GetGroupMaterialsQuery query, CancellationToken cancellationToken)
        {
            var result = await _materialService.GetGroupMaterialsAsync(query.GroupId, query.UserId, cancellationToken);
            return Result<List<MaterialResponseDto>>.Success(result);
        }
    }
}