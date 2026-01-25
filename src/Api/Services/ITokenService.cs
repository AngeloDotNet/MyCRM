using Api.Models;

namespace Api.Services;

public interface ITokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IEnumerable<string> roles);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, ApplicationUser user);
    Task RevokeRefreshTokenAsync(string refreshToken, ApplicationUser user);
}