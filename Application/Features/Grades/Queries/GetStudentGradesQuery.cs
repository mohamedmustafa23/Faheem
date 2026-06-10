using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Queries
{
    public class GetStudentGradesQuery : IRequest<Result<List<StudentGradeResponseDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetStudentGradesQueryHandler : IRequestHandler<GetStudentGradesQuery, Result<List<StudentGradeResponseDto>>>
    {
        private readonly IGradeService _gradeService;
        public GetStudentGradesQueryHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result<List<StudentGradeResponseDto>>> Handle(GetStudentGradesQuery query, CancellationToken cancellationToken)
        {
            var result = await _gradeService.GetStudentGradesAsync(query.StudentId, cancellationToken);
            return Result<List<StudentGradeResponseDto>>.Success(result);
        }
    }
}