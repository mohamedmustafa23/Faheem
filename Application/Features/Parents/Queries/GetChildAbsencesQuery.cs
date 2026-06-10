using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Recent absence timeline for one of the parent's linked children.
    /// Delegates to ParentService — which enforces the parent-link guard and
    /// caps the page size.
    /// </summary>
    public class GetChildAbsencesQuery : IRequest<Result<List<ChildAbsenceDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
        public int Take { get; set; } = 30;
    }

    public class GetChildAbsencesQueryHandler : IRequestHandler<GetChildAbsencesQuery, Result<List<ChildAbsenceDto>>>
    {
        private readonly IParentService _parentService;

        public GetChildAbsencesQueryHandler(IParentService parentService) => _parentService = parentService;

        public async Task<Result<List<ChildAbsenceDto>>> Handle(GetChildAbsencesQuery query, CancellationToken cancellationToken)
        {
            var data = await _parentService.GetChildAbsencesAsync(query.ParentId, query.ChildId, query.Take, cancellationToken);
            return Result<List<ChildAbsenceDto>>.Success(data);
        }
    }
}
