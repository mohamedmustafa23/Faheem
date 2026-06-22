using Application.Features.Tokens.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Tokens.Commands
{
    public class SelectWorkspaceCommand : IRequest<Result<TokenResponse>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;

        /// <summary>The workspace (tenant) the user is signing into.</summary>
        public string TenantId { get; set; } = string.Empty;
    }

    public class SelectWorkspaceCommandHandler : IRequestHandler<SelectWorkspaceCommand, Result<TokenResponse>>
    {
        private readonly ITokenService _tokenService;
        public SelectWorkspaceCommandHandler(ITokenService tokenService) => _tokenService = tokenService;

        public async Task<Result<TokenResponse>> Handle(SelectWorkspaceCommand command, CancellationToken cancellationToken)
        {
            var token = await _tokenService.SelectWorkspaceAsync(command.UserId, command.TenantId);
            return Result<TokenResponse>.Success(token, "Workspace selected.");
        }
    }
}
