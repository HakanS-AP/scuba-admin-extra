using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScubaHub.AdminWeb.Services;

namespace ScubaHub.AdminWeb.Pages;

// Server-side equivalent of Login.jsx + adminLogin(). The browser never sees
// the admin key: it is validated against the backend, then tucked into the
// encrypted ASP.NET auth cookie as a claim for later backend calls.
public class LoginModel : PageModel
{
    private readonly AdminApiClient _api;

    public LoginModel(AdminApiClient api) => _api = api;

    [BindProperty]
    public string Password { get; set; } = "";

    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        // Already signed in? Skip straight to the dashboard (mirrors the SPA's
        // catch-all redirect).
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Dashboard");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string? key;
        try
        {
            key = await _api.VerifyPasswordAsync(Password);
        }
        catch
        {
            Error = "Could not reach the backend. Is ScubaHub.Api running?";
            return Page();
        }

        if (key is null)
        {
            Error = "Invalid password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(AdminApiClient.AdminKeyClaim, key),
        };
        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Dashboard");
    }
}
