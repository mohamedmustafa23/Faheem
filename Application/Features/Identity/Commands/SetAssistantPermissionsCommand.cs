using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class SetAssistantPermissionsCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string AssistantUserId { get; set; } = string.Empty;
        [JsonIgnore] public string TeacherTenantId { get; set; } = string.Empty;
        public int Permissions { get; set; }
    }

    public class SetAssistantPermissionsCommandHandler : IRequestHandler<SetAssistantPermissionsCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public SetAssistantPermissionsCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(SetAssistantPermissionsCommand command, CancellationToken cancellationToken)
        {
            var message = await _authService.SetAssistantPermissionsAsync(
                command.AssistantUserId, command.TeacherTenantId, command.Permissions, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
