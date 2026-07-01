using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ScubaHub.AdminWeb.Services;

/// <summary>
/// Server-side replacement for the SPA's <c>api.js</c>.
///
/// admin-frontend ran in the browser and let the browser carry the HttpOnly
/// <c>admin_session</c> cookie. scuba-frontend renders on the server, so there
/// is no browser to carry that cookie — instead this typed client attaches the
/// admin key (captured at login, stored as an encrypted auth-cookie claim) as
/// the <c>admin_session</c> cookie header on every backend call.
/// </summary>
public class AdminApiClient
{
    public const string AdminKeyClaim = "admin_key";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public AdminApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    /// <summary>
    /// Validates the password against the backend. Returns the key on success
    /// (callers persist it as a claim), or null when the backend rejects it.
    /// </summary>
    public async Task<string?> VerifyPasswordAsync(string password)
    {
        var res = await _http.PostAsJsonAsync(
            "/api/admin/auth/verify", new { password });

        if (res.StatusCode == HttpStatusCode.Unauthorized) return null;
        res.EnsureSuccessStatusCode();

        // The backend's session cookie value IS the admin key, and verify only
        // succeeds when password == key, so the validated password is the key.
        return password;
    }

    public async Task<IReadOnlyList<WeatherForecastDto>> GetForecastsAsync()
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get, "/api/admin/weatherforecast");
        AttachSession(req);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<List<WeatherForecastDto>>()
               ?? new List<WeatherForecastDto>();
    }

    public async Task<WeatherForecastDto?> UpdateForecastAsync(
        int index, int temperatureC, string? summary)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Put, $"/api/admin/weatherforecast/{index}")
        {
            Content = JsonContent.Create(new { temperatureC, summary })
        };
        AttachSession(req);

        var res = await _http.SendAsync(req);
        if (res.StatusCode == HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<WeatherForecastDto>();
    }

    // Adds "Cookie: admin_session=<key>" so the backend's AdminAuthFilter passes.
    private void AttachSession(HttpRequestMessage req)
    {
        var key = _accessor.HttpContext?.User
                           .FindFirstValue(AdminKeyClaim);

        if (!string.IsNullOrEmpty(key))
        {
            req.Headers.Add("Cookie", $"admin_session={key}");
        }
    }
}
