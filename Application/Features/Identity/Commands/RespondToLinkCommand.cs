using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class RespondToLinkCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string StudentId { get; set; } = string.Empty;
        public Guid LinkId { get; set; }
        public bool Accept { get; set; } 
    }

    public class RespondToLinkCommandHandler : IRequestHandler<RespondToLinkCommand, Result>
    {
        private readonly ILinkService _linkService;
        public RespondToLinkCommandHandler(ILinkService linkService) => _linkService = linkService;

        public async Task<Result> Handle(RespondToLinkCommand command, CancellationToken cancellationToken)
        {
            var result = await _linkService.RespondToLinkAsync(command.StudentId, command.LinkId, command.Accept, cancellationToken);
            return Result<string>.Success(result);
        }
    }
}