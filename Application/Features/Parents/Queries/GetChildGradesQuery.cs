using Application.Exceptions;
using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    public class GetChildGradesQuery : IRequest<Result<List<StudentGradeResponseDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
    }

    public class GetChildGradesQueryHandler : IRequestHandler<GetChildGradesQuery, Result<List<StudentGradeResponseDto>>>
    {
        private readonly IGradeService _gradeService;
        private readonly IParentService _parentService; 

        public GetChildGradesQueryHandler(IGradeService gradeService, IParentService parentService)
        {
            _gradeService = gradeService;
            _parentService = parentService;
        }

        public async Task<Result<List<StudentGradeResponseDto>>> Handle(GetChildGradesQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);

            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's grades."]);

            var result = await _gradeService.GetStudentGradesAsync(query.ChildId, cancellationToken);
            return Result<List<StudentGradeResponseDto>>.Success(result);
        }
    }
}