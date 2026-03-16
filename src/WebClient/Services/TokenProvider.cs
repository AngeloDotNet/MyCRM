using Microsoft.JSInterop;

namespace WebClient.Services;

public class TokenProvider(IJSRuntime js) : ITokenProvider
{
    private const string AccessKey = "mycrm_access_token";
    private const string RefreshKey = "mycrm_refresh_token";
    private const string EmailKey = "mycrm_user_email";

    public event Action? TokensChanged;

    public async Task SetTokensAsync(string accessToken, string refreshToken, string? email = null)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);

        if (!string.IsNullOrEmpty(email))
        {
            await js.InvokeVoidAsync("localStorage.setItem", EmailKey, email);
        }
    }

    public async Task ClearTokensAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
        await js.InvokeVoidAsync("localStorage.removeItem", EmailKey);

        TokensChanged?.Invoke();
    }

    public async Task<(string? accessToken, string? refreshToken, string? email)> GetTokensAsync()
    {
        var access = await js.InvokeAsync<string?>("localStorage.getItem", AccessKey);
        var refresh = await js.InvokeAsync<string?>("localStorage.getItem", RefreshKey);
        var email = await js.InvokeAsync<string?>("localStorage.getItem", EmailKey);

        return (access, refresh, email);
    }

    public Task NotifyTokensChangedAsync()
    {
        TokensChanged?.Invoke();
        return Task.CompletedTask;
    }
}
