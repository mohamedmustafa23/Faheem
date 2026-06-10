using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Features.Groups.Commands
{
    public class UpdateGroupCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public UpdateGroupRequest Request { get; set; } = new();
    }

    public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, Result>
    {
        private readonly IGroupService _groupService;
        public UpdateGroupCommandHandler(IGroupService groupService) => _groupService = groupService;

        public async Task<Result> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
        {
            var result = await _groupService.UpdateGroupAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
