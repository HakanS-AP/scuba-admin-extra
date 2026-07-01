using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScubaHub.AdminWeb.Pages;

// Equivalent of adminLogout(): clears the auth cookie and returns to /Login.
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    // A direct GET to /Logout just bounces to login (no state change on GET).
    public IActionResult OnGet() => RedirectToPage("/Login");
}
