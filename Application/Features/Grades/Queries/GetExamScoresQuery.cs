using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Queries
{
    public class GetExamScoresQuery : IRequest<Result<List<ExamScoreResponseDto>>>
    {
        public Guid ExamId { get; set; }
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetExamScoresQueryHandler : IRequestHandler<GetExamScoresQuery, Result<List<ExamScoreResponseDto>>>
    {
        private readonly IGradeService _gradeService;
        public GetExamScoresQueryHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result<List<ExamScoreResponseDto>>> Handle(GetExamScoresQuery query, CancellationToken cancellationToken)
        {
            var result = await _gradeService.GetExamGradesForTeacherAsync(query.ExamId, query.TenantId, cancellationToken);
            return Result<List<ExamScoreResponseDto>>.Success(result);
        }
    }
}