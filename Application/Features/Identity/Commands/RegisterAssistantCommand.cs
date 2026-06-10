using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Commands
{
    public class RegisterAssistantCommand : IRequest<Result<string>>
    {
        // بناخد الـ TenantId من التوكن بتاع المدرس اللي بيكريت الأكونت
        [JsonIgnore] public string TeacherTenantId { get; set; } = string.Empty;
        public RegisterAssistantRequest Request { get; set; } = new();
    }

    public class RegisterAssistantCommandHandler : IRequestHandler<RegisterAssistantCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public RegisterAssistantCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(RegisterAssistantCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAssistantAsync(command.Request, command.TeacherTenantId, cancellationToken);
            return Result<string>.Success(result, "Assistant account created successfully.");
        }
    }
}