using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class InviteTeacherCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        public InviteTeacherRequest Request { get; set; } = new();
    }

    public class InviteTeacherCommandHandler : IRequestHandler<InviteTeacherCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public InviteTeacherCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(InviteTeacherCommand command, CancellationToken cancellationToken)
        {
            var message = await _centerService.InviteTeacherAsync(command.TenantId, command.OwnerUserId, command.Request, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
