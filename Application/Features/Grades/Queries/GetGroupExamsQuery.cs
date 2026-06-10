using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Queries
{
    public class GetGroupExamsQuery : IRequest<Result<List<GroupExamResponseDto>>>
    {
        public Guid GroupId { get; set; }
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetGroupExamsQueryHandler : IRequestHandler<GetGroupExamsQuery, Result<List<GroupExamResponseDto>>>
    {
        private readonly IGradeService _gradeService;
        public GetGroupExamsQueryHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result<List<GroupExamResponseDto>>> Handle(GetGroupExamsQuery query, CancellationToken cancellationToken)
        {
            var result = await _gradeService.GetGroupExamsForTeacherAsync(query.GroupId, query.TenantId, cancellationToken);
            return Result<List<GroupExamResponseDto>>.Success(result);
        }
    }
}