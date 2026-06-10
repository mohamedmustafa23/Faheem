using Application.Features.Tokens.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Tokens.Queries
{
    public class GetRefreshTokenQuery : IRequest<Result<TokenResponse>>
    {
        public RefreshTokenRequest RefreshTokenRequest { get; set; } = new();
    }

    public class GetRefreshTokenQueryHandler : IRequestHandler<GetRefreshTokenQuery, Result<TokenResponse>>
    {
        private readonly ITokenService _tokenService;
        public GetRefreshTokenQueryHandler(ITokenService tokenService) => _tokenService = tokenService;

        public async Task<Result<TokenResponse>> Handle(GetRefreshTokenQuery request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.RefreshTokenAsync(request.RefreshTokenRequest);
            return Result<TokenResponse>.Success(token, "Token refreshed successfully.");
        }
    }
}
