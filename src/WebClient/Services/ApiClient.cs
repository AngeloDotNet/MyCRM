using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebClient.Services;

public class ApiClient(HttpClient http, ITokenProvider tokens, ILogger<ApiClient> logger)
{
    public async Task<ApiResult<T>> GetAsync<T>(string url) => await SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, url), includeAuth: true);

    public async Task<ApiResult<T>> PostAsync<T>(string url, object payload, bool includeAuth = true)
    {
        return await SendAsync<T>(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = JsonContent.Create(payload);
            return req;
        }, includeAuth);
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> requestFactory, bool includeAuth = true)
    {
        (var access, var refresh, var email) = await tokens.GetTokensAsync();

        HttpResponseMessage? response = null;
        // attempt request (with token if present)
        async Task<HttpRequestMessage> PrepareRequestAsync()
        {
            var req = requestFactory();
            if (includeAuth && !string.IsNullOrEmpty(access))
            {
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
            }

            return req;
        }

        var reqMsg = await PrepareRequestAsync();
        response = await http.SendAsync(reqMsg);

        if (response.StatusCode == HttpStatusCode.Unauthorized && includeAuth && !string.IsNullOrEmpty(refresh) && !string.IsNullOrEmpty(email))
        {
            // try refresh
            var refreshed = await TryRefreshAsync(email, refresh);
            if (refreshed)
            {
                // retry once
                (var newAccess, var _, var _) = await tokens.GetTokensAsync();
                var retryReq = requestFactory();

                if (!string.IsNullOrEmpty(newAccess))
                {
                    retryReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccess);
                }

                response = await http.SendAsync(retryReq);
            }
        }

        if (response == null)
        {
            return ApiResult<T>.Fail("No response");
        }

        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
            {
                return ApiResult<T>.Success(default!);
            }

            var value = await response.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Success(value!);
        }

        return ApiResult<T>.Fail(response.StatusCode.ToString(), response.StatusCode);
    }

    private async Task<bool> TryRefreshAsync(string email, string refreshToken)
    {
        try
        {
            var r = await http.PostAsJsonAsync("/api/v1/auth/refresh", new { Email = email, RefreshToken = refreshToken });

            if (!r.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await r.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("access_token", out var at) && json.TryGetProperty("refresh_token", out var rt))
            {
                var access = at.GetString()!;
                var refresh = rt.GetString()!;
                await tokens.SetTokensAsync(access, refresh, email);
                await tokens.NotifyTokensChangedAsync();

                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refresh failed");
        }

        return false;
    }
}

public record ApiResult<T>(bool IsSuccess, T? Value, string? Error, System.Net.HttpStatusCode? StatusCode)
{
    public static ApiResult<T> Success(T value) => new(true, value, null, null);
    public static ApiResult<T> Fail(string? err, System.Net.HttpStatusCode? status = null) => new(false, default, err, status);
}