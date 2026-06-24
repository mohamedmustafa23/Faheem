using Application.Features.Tokens.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Tokens.Queries
{
    public class GetMyWorkspacesQuery : IRequest<Result<List<WorkspaceOption>>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class GetMyWorkspacesQueryHandler : IRequestHandler<GetMyWorkspacesQuery, Result<List<WorkspaceOption>>>
    {
        private readonly ITokenService _tokenService;
        public GetMyWorkspacesQueryHandler(ITokenService tokenService) => _tokenService = tokenService;

        public async Task<Result<List<WorkspaceOption>>> Handle(GetMyWorkspacesQuery query, CancellationToken cancellationToken)
        {
            var workspaces = await _tokenService.GetUserWorkspacesAsync(query.UserId);
            return Result<List<WorkspaceOption>>.Success(workspaces);
        }
    }
}
