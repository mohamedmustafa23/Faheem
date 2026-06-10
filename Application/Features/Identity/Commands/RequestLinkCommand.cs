using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class RequestLinkCommand : IRequest<Result>
    {
        [JsonIgnore] 
        public string ParentId { get; set; } = string.Empty;
        public string StudentPhoneNumber { get; set; } = string.Empty;
    }

    public class RequestLinkCommandHandler : IRequestHandler<RequestLinkCommand, Result>
    {
        private readonly ILinkService _linkService;
        public RequestLinkCommandHandler(ILinkService linkService) => _linkService = linkService;

        public async Task<Result> Handle(RequestLinkCommand command, CancellationToken cancellationToken)
        {
            var result = await _linkService.RequestLinkAsync(command.ParentId, command.StudentPhoneNumber, cancellationToken);
            return Result<string>.Success(result);
        }
    }
}