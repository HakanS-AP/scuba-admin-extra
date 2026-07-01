using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScubaHub.AdminWeb.Services;

namespace ScubaHub.AdminWeb.Pages;

// Server-side equivalent of Dashboard.jsx. The React version kept the "which
// row is being edited" state in the browser; here it lives in the query string
// (?edit=2), and each save round-trips to the server (Post-Redirect-Get).
public class DashboardModel : PageModel
{
    private readonly AdminApiClient _api;

    public DashboardModel(AdminApiClient api) => _api = api;

    public IReadOnlyList<WeatherForecastDto> Forecasts { get; private set; }
        = new List<WeatherForecastDto>();

    // Which row is in edit mode (-1 = none), driven by the ?edit= query string.
    public int EditingIndex { get; private set; } = -1;

    public string? Error { get; private set; }

    public async Task OnGetAsync(int edit = -1)
    {
        EditingIndex = edit;
        try
        {
            Forecasts = await _api.GetForecastsAsync();
        }
        catch
        {
            Error = "Could not load forecasts from the backend.";
        }
    }

    public async Task<IActionResult> OnPostSaveAsync(
        int index, int temperatureC, string? summary)
    {
        try
        {
            await _api.UpdateForecastAsync(index, temperatureC, summary);
        }
        catch
        {
            Error = "Save failed.";
            await OnGetAsync(index);
            return Page();
        }

        // PRG: redirect so a refresh doesn't re-submit the edit.
        return RedirectToPage("/Dashboard");
    }
}
