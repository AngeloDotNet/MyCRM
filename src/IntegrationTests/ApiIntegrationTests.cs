using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace IntegrationTests;

public class ApiIntegrationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Register_Login_And_Refresh_Works()
    {
        var client = factory.CreateClient();

        var email = $"inttest{Guid.NewGuid().ToString()[..6]}@local";
        var password = "Password123!";

        // Register
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = password });
        Assert.True(regRes.IsSuccessStatusCode);

        // Login
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        Assert.True(loginRes.IsSuccessStatusCode);
        var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(loginJson);
        var refreshToken = loginJson?["refresh_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(refreshToken));
        var accessToken = loginJson?["access_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(accessToken));

        // Refresh (rotate)
        var refreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = refreshToken });
        Assert.True(refreshRes.IsSuccessStatusCode);
        var refreshJson = await refreshRes.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(refreshJson);
        var newRefresh = refreshJson?["refresh_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(newRefresh));
    }

    [Fact]
    public async Task Refresh_Reuse_Detection_Revokes_All_Sessions()
    {
        var client = factory.CreateClient();

        var email = $"reuse{System.Guid.NewGuid().ToString().Substring(0, 6)}@local";
        var password = "Password123!";

        // Register
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = password });
        Assert.True(regRes.IsSuccessStatusCode);

        // Login -> obtain rt1
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        Assert.True(loginRes.IsSuccessStatusCode);
        var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonObject>();
        var rt1 = loginJson?["refresh_token"]?.GetValue<string>();
        var at1 = loginJson?["access_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(rt1));
        Assert.False(string.IsNullOrEmpty(at1));

        // First refresh -> rotate -> get rt2
        var refreshRes1 = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = rt1 });
        Assert.True(refreshRes1.IsSuccessStatusCode);
        var refreshJson1 = await refreshRes1.Content.ReadFromJsonAsync<JsonObject>();
        var rt2 = refreshJson1?["refresh_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(rt2));

        // Simulate token reuse: call refresh with old rt1 again -> the server must detect reuse and revoke all tokens
        var refreshResReuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = rt1 });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResReuse.StatusCode);

        // Now try to refresh with rt2 -> must be unauthorized because server revoked all sessions
        var refreshRes2 = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = rt2 });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes2.StatusCode);
    }

    [Fact]
    public async Task RevokeAll_Endpoint_RevokesSessions()
    {
        var client = factory.CreateClient();

        var email = $"revoke{System.Guid.NewGuid().ToString().Substring(0, 6)}@local";
        var password = "Password123!";

        // Register
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = password });
        Assert.True(regRes.IsSuccessStatusCode);

        // Login -> get tokens
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        Assert.True(loginRes.IsSuccessStatusCode);
        var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonObject>();
        var rt = loginJson?["refresh_token"]?.GetValue<string>();
        var at = loginJson?["access_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(rt));
        Assert.False(string.IsNullOrEmpty(at));

        // Call revoke/all (requires Authorization)
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", at);
        var revokeRes = await client.PostAsync("/api/v1/auth/revoke/all", null);
        Assert.True(revokeRes.IsSuccessStatusCode);

        // Now refresh with previous refresh token must fail
        var refreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = rt });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }
}
