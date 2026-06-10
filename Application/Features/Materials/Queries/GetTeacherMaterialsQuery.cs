using Application.Features.Materials.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Materials.Queries
{
    public class GetTeacherMaterialsQuery : IRequest<Result<List<MaterialResponseDto>>>
    {
        public Guid GroupId { get; set; }
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetTeacherMaterialsQueryHandler : IRequestHandler<GetTeacherMaterialsQuery, Result<List<MaterialResponseDto>>>
    {
        private readonly IMaterialService _materialService;
        public GetTeacherMaterialsQueryHandler(IMaterialService materialService) => _materialService = materialService;

        public async Task<Result<List<MaterialResponseDto>>> Handle(GetTeacherMaterialsQuery query, CancellationToken cancellationToken)
        {
            var result = await _materialService.GetTeacherMaterialsAsync(query.GroupId, query.TenantId, cancellationToken);
            return Result<List<MaterialResponseDto>>.Success(result);
        }
    }
}