namespace WebClient.Models;

public record LoginResponse(string AccessToken, string RefreshToken)
{
    // JSON property names from API: access_token, refresh_token
    public static LoginResponse FromJson(System.Text.Json.JsonElement el)
    {
        return new LoginResponse(el.GetProperty("access_token").GetString()!, el.GetProperty("refresh_token").GetString()!);
    }
}