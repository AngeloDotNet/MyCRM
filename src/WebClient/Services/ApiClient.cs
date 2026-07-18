using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MudBlazor;
using WebClient.Services;

namespace MyCrm.Client.Services
{
    public class ApiClient(HttpClient http, ITokenProvider tokens, ILogger<ApiClient> logger, ISnackbar snackbar)
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
            var (access, refresh, email) = await tokens.GetTokensAsync();

            HttpResponseMessage? response = null;

            async Task<HttpRequestMessage> PrepareRequestAsync(string? token)
            {
                var req = requestFactory();
                if (includeAuth && !string.IsNullOrEmpty(token))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                return req;
            }

            var reqMsg = await PrepareRequestAsync(access);
            response = await http.SendAsync(reqMsg);

            if (response.StatusCode == HttpStatusCode.Unauthorized && includeAuth)
            {
                // Try refresh if we have refresh token and email
                if (!string.IsNullOrEmpty(refresh) && !string.IsNullOrEmpty(email))
                {
                    var refreshed = await TryRefreshAsync(email, refresh);
                    if (refreshed)
                    {
                        // retry once with new access token
                        var (newAccess, _, _) = await tokens.GetTokensAsync();
                        var retryReq = await PrepareRequestAsync(newAccess);
                        response = await http.SendAsync(retryReq);
                    }
                    else
                    {
                        // Refresh failed -> force logout/clear tokens and notify user
                        await tokens.ClearTokensAsync();
                        snackbar.Add("Sessione scaduta. Effettua il login.", Severity.Warning);
                        return ApiResult<T>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    // No refresh token -> clear and notify
                    await tokens.ClearTokensAsync();
                    snackbar.Add("Sessione non valida. Effettua il login.", Severity.Warning);
                    return ApiResult<T>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
                }
            }

            if (response == null)
                return ApiResult<T>.Fail("No response");

            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
                {
                    return ApiResult<T>.Success(default!);
                }

                var value = await response.Content.ReadFromJsonAsync<T>();
                return ApiResult<T>.Success(value!);
            }

            // If after retry still unauthorized -> clear tokens
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await tokens.ClearTokensAsync();
                snackbar.Add("Accesso negato. Effettua il login.", Severity.Error);
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
                    // If 401 and reuse detection occurred on server, server revoked sessions; ensure local logout
                    logger.LogWarning("Refresh failed with status {Status}", r.StatusCode);
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
}