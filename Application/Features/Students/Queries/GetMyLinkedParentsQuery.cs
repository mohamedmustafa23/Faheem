using Application.Features.Students.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Students.Queries
{
    /// <summary>
    /// Lists the accepted parent links for the current student — drives the
    /// "أهلي" panel in the student app.
    /// </summary>
    public class GetMyLinkedParentsQuery : IRequest<Result<List<LinkedParentDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetMyLinkedParentsQueryHandler : IRequestHandler<GetMyLinkedParentsQuery, Result<List<LinkedParentDto>>>
    {
        private readonly IStudentService _studentService;
        public GetMyLinkedParentsQueryHandler(IStudentService studentService) => _studentService = studentService;

        public async Task<Result<List<LinkedParentDto>>> Handle(GetMyLinkedParentsQuery request, CancellationToken cancellationToken)
        {
            var data = await _studentService.GetMyLinkedParentsAsync(request.StudentId, cancellationToken);
            return Result<List<LinkedParentDto>>.Success(data);
        }
    }
}
