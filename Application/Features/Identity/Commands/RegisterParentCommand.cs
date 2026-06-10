using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class RegisterParentCommand : IRequest<Result<string>>
    {
        public RegisterParentRequest Request { get; set; } = new();
    }
    public class RegisterParentCommandHandler : IRequestHandler<RegisterParentCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public RegisterParentCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(RegisterParentCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterParentAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Parent registered successfully. Please verify your email.");
        }
    }
}
