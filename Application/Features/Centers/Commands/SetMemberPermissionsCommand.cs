using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class SetMemberPermissionsCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        [JsonIgnore] public string MemberUserId { get; set; } = string.Empty;
        public SetMemberPermissionsRequest Request { get; set; } = new();
    }

    public class SetMemberPermissionsCommandHandler : IRequestHandler<SetMemberPermissionsCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public SetMemberPermissionsCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(SetMemberPermissionsCommand command, CancellationToken cancellationToken)
        {
            var message = await _centerService.SetMemberPermissionsAsync(
                command.TenantId, command.OwnerUserId, command.MemberUserId, command.Request.Permissions, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
