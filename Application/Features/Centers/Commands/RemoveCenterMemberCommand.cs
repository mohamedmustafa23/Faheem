using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class RemoveCenterMemberCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        public string MemberUserId { get; set; } = string.Empty;
    }

    public class RemoveCenterMemberCommandHandler : IRequestHandler<RemoveCenterMemberCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public RemoveCenterMemberCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(RemoveCenterMemberCommand command, CancellationToken cancellationToken)
        {
            var message = await _centerService.RemoveMemberAsync(command.TenantId, command.OwnerUserId, command.MemberUserId, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
