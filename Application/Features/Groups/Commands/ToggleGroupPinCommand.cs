using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class ToggleGroupPinCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid GroupId { get; set; }
    }

    public class ToggleGroupPinCommandHandler : IRequestHandler<ToggleGroupPinCommand, Result>
    {
        private readonly IGroupService _groupService;
        public ToggleGroupPinCommandHandler(IGroupService groupService) => _groupService = groupService;

        public async Task<Result> Handle(ToggleGroupPinCommand command, CancellationToken cancellationToken)
        {
            var message = await _groupService.TogglePinAsync(command.GroupId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
