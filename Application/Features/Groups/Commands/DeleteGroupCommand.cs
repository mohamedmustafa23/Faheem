using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class DeleteGroupCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, Result>
    {
        private readonly IGroupService _groupService;
        public DeleteGroupCommandHandler(IGroupService groupService) => _groupService = groupService;

        public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
        {
            var result = await _groupService.DeleteGroupAsync(command.GroupId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
