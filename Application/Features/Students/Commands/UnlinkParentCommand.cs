using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Students.Commands
{
    /// <summary>
    /// Student-initiated unlink. Removes the parent–student row from the
    /// student's side so the parent can no longer see the student's data.
    /// </summary>
    public class UnlinkParentCommand : IRequest<Result>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
    }

    public class UnlinkParentCommandHandler : IRequestHandler<UnlinkParentCommand, Result>
    {
        private readonly ILinkService _linkService;
        public UnlinkParentCommandHandler(ILinkService linkService) => _linkService = linkService;

        public async Task<Result> Handle(UnlinkParentCommand request, CancellationToken cancellationToken)
        {
            var result = await _linkService.UnlinkParentAsync(request.StudentId, request.ParentId, cancellationToken);
            return Result<string>.Success(result);
        }
    }
}
