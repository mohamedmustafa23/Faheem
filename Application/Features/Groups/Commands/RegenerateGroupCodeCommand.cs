using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class RegenerateGroupCodeCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class RegenerateGroupCodeCommandHandler : IRequestHandler<RegenerateGroupCodeCommand, Result<string>>
    {
        private readonly IGroupService _groupService;
        public RegenerateGroupCodeCommandHandler(IGroupService groupService) => _groupService = groupService;

        public async Task<Result<string>> Handle(RegenerateGroupCodeCommand command, CancellationToken cancellationToken)
        {
            var newCode = await _groupService.RegenerateCodeAsync(command.GroupId, command.TenantId, cancellationToken);
            return Result<string>.Success(newCode, "Group code regenerated successfully.");
        }
    }
}