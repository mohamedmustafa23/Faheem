using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class UnlinkChildCommand : IRequest<Result>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class UnlinkChildCommandHandler : IRequestHandler<UnlinkChildCommand, Result>
    {
        private readonly ILinkService _linkService;
        public UnlinkChildCommandHandler(ILinkService linkService) => _linkService = linkService;

        public async Task<Result> Handle(UnlinkChildCommand command, CancellationToken cancellationToken)
        {
            var result = await _linkService.UnlinkChildAsync(command.ParentId, command.StudentId, cancellationToken);
            return Result.Success(result);
        }
    }
}