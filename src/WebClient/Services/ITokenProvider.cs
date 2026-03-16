namespace WebClient.Services;

public interface ITokenProvider
{
    Task SetTokensAsync(string accessToken, string refreshToken, string? email = null);
    Task ClearTokensAsync();
    Task<(string? accessToken, string? refreshToken, string? email)> GetTokensAsync();
    Task NotifyTokensChangedAsync();
    event Action? TokensChanged;
}