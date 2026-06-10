using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class CreateGroupCommand : IRequest<Result<string>>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public CreateGroupRequest Request { get; set; } = new();
    }

    public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<string>>
    {
        private readonly IGroupService _groupService;

        public CreateGroupCommandHandler(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public async Task<Result<string>> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
        {
            var enrollmentCode = await _groupService.CreateGroupAsync(command.Request, command.TenantId, cancellationToken);
            return Result<string>.Success(enrollmentCode, "Group created successfully.");
        }
    }
}