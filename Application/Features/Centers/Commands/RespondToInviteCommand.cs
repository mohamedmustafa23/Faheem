using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class RespondToInviteCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;

        /// <summary>True to accept the invite, false to decline.</summary>
        public bool Accept { get; set; }
    }

    public class RespondToInviteCommandHandler : IRequestHandler<RespondToInviteCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public RespondToInviteCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(RespondToInviteCommand command, CancellationToken cancellationToken)
        {
            var message = await _centerService.RespondToInviteAsync(command.UserId, command.TenantId, command.Accept, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
