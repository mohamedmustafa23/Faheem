using Application.Features.Materials.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Materials.Queries
{
    /// <summary>
    /// Lists every material across every group the student is enrolled in,
    /// newest first. Drives the student-side global materials search screen.
    /// </summary>
    public class GetStudentAllMaterialsQuery : IRequest<Result<List<MaterialResponseDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetStudentAllMaterialsQueryHandler : IRequestHandler<GetStudentAllMaterialsQuery, Result<List<MaterialResponseDto>>>
    {
        private readonly IMaterialService _materialService;
        public GetStudentAllMaterialsQueryHandler(IMaterialService materialService) => _materialService = materialService;

        public async Task<Result<List<MaterialResponseDto>>> Handle(GetStudentAllMaterialsQuery request, CancellationToken cancellationToken)
        {
            var data = await _materialService.GetStudentAllMaterialsAsync(request.StudentId, cancellationToken);
            return Result<List<MaterialResponseDto>>.Success(data);
        }
    }
}
