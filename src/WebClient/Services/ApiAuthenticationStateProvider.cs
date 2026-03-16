using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebClient.Services;

// Builds ClaimsPrincipal from access token. If token missing or expired, returns anonymous.
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenProvider _tokenProvider;

    public ApiAuthenticationStateProvider(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        _tokenProvider.TokensChanged += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var tokens = await _tokenProvider.GetTokensAsync();
        var access = tokens.Item1; // or tokens.access if it's a named tuple

        if (string.IsNullOrEmpty(access))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        try
        {
            var claims = ParseClaimsFromJwt(access);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    // Simple JWT parser to extract claims without package dependency
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');

        if (parts.Length < 2)
        {
            return [];
        }

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes)!;
        var claims = new List<Claim>();

        foreach (var kv in keyValuePairs)
        {
            if (kv.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in kv.Value.EnumerateArray())
                {
                    claims.Add(new Claim(kv.Key, v.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(kv.Key, kv.Value.ToString() ?? ""));
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}