using Application.Features.Tokens.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

public class GetTokenQuery : IRequest<Result<TokenResponse>>
{
    public TokenRequest TokenRequest { get; set; } = new();
}

public class GetTokenQueryHandler : IRequestHandler<GetTokenQuery, Result<TokenResponse>>
{
    private readonly ITokenService _tokenService;
    public GetTokenQueryHandler(ITokenService tokenService) => _tokenService = tokenService;

    public async Task<Result<TokenResponse>> Handle(GetTokenQuery request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.LoginAsync(request.TokenRequest);
        return Result<TokenResponse>.Success(token, "Login successful.");
    }
}