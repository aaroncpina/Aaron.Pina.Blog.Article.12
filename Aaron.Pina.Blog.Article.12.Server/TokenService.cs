using Aaron.Pina.Blog.Article._12.Shared.Responses;
using Aaron.Pina.Blog.Article._12.Shared;
using Microsoft.Extensions.Options;

namespace Aaron.Pina.Blog.Article._12.Server;

public class TokenService(
    UserRepository userRepo,
    TokenRepository tokenRepo,
    JwksKeyManager keyManager,
    IOptionsSnapshot<TokenConfig> config)
{
    public async Task<TokenResult> HandleAccessTokenRequestAsync(Guid? userId, string? scope)
    {
        if (userId is null) return TokenResult.Fail("User id is required");
        if (string.IsNullOrEmpty(scope)) return TokenResult.Fail("Scope is required");
        if (!AudienceExtractor.TryExtractAudience(scope, out var audience)) return TokenResult.Fail("Invalid scope");
        if (!Api.IsValidTarget(audience)) return TokenResult.Fail("Invalid audience");
        var token = tokenRepo.TryGetTokenByUserIdAndAudience(userId.Value, audience);
        if (token is not null) return TokenResult.Fail("An active token exists for this audience");
        var user = userRepo.TryGetUserById(userId.Value);
        if (user is null) return TokenResult.Fail("Invalid user id");
        var jti = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var signingKey = await keyManager.GetOrCreateSigningKeyAsync();
        var refreshToken = TokenGenerator.GenerateRefreshToken();
        var accessToken = TokenGenerator.GenerateToken(
            signingKey, jti, userId.Value, user.Role, audience, scope, now, config.Value.AccessTokenLifetime);
        var response = new TokenResponse(
            jti, accessToken, refreshToken, config.Value.AccessTokenLifetime.TotalMinutes);
        tokenRepo.SaveToken(new TokenEntity
        {
            RefreshTokenExpiresAt = now.Add(config.Value.RefreshTokenLifetime),
            RefreshToken = refreshToken,
            UserId = userId.Value,
            Audience = audience,
            CreatedAt = now,
            Scope = scope
        });
        return TokenResult.Success(response);
    }

    public async Task<TokenResult> HandleRefreshTokenRequestAsync(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return TokenResult.Fail("Refresh token is required");
        var token = tokenRepo.TryGetTokenByRefreshToken(refreshToken);
        if (token is null) return TokenResult.Fail("Invalid refresh token");
        if (token.RefreshTokenExpiresAt < DateTime.UtcNow) return TokenResult.Fail("Refresh token has expired");
        var user = userRepo.TryGetUserById(token.UserId);
        if (user is null) return TokenResult.Fail("Invalid user id");
        var jti = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var signingKey = await keyManager.GetOrCreateSigningKeyAsync();
        var newRefreshToken = TokenGenerator.GenerateRefreshToken();
        var accessToken = TokenGenerator.GenerateToken(
            signingKey, jti, token.UserId, user.Role, token.Audience, token.Scope, now, config.Value.AccessTokenLifetime);
        var response = new TokenResponse(
            jti, accessToken, newRefreshToken, config.Value.AccessTokenLifetime.TotalMinutes);
        token.RefreshTokenExpiresAt = now.Add(config.Value.RefreshTokenLifetime);
        token.RefreshToken = newRefreshToken;
        tokenRepo.UpdateToken(token);
        return TokenResult.Success(response);
    }
}
