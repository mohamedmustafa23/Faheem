using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class RemoveAssistantCommand : IRequest<Result>
    {
        [JsonIgnore] public string TeacherTenantId { get; set; } = string.Empty;
        [JsonIgnore] public string AssistantUserId { get; set; } = string.Empty;
    }

    public class RemoveAssistantCommandHandler : IRequestHandler<RemoveAssistantCommand, Result>
    {
        private readonly IAuthService _authService;
        public RemoveAssistantCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result> Handle(RemoveAssistantCommand command, CancellationToken cancellationToken)
        {
            var message = await _authService.RemoveAssistantAsync(
                command.AssistantUserId, command.TeacherTenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
