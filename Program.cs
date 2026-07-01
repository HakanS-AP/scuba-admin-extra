using Microsoft.AspNetCore.Authentication.Cookies;
using ScubaHub.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Razor Pages ──────────────────────────────────────────────────────────────
// The whole UI is server-rendered. Every page under /Pages is reachable by its
// path (Login.cshtml -> /Login) unless it opts out. The folder convention here
// mirrors the React SPA's routes: /Login and /Dashboard.
builder.Services.AddRazorPages(options =>
{
    // Equivalent of <ProtectedRoute>: everything requires auth by default…
    options.Conventions.AuthorizeFolder("/");
    // …except the pages an unauthenticated user must reach.
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});

// ── Cookie authentication ────────────────────────────────────────────────────
// Replaces the SPA's HttpOnly-cookie + ProtectedRoute dance. ASP.NET issues its
// own encrypted, HttpOnly auth cookie; [Authorize]/AuthorizeFolder enforce it.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // matches backend MaxAge
        options.SlidingExpiration = true;
        options.Cookie.Name = "scuba_admin_web";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// ── Backend API client (BFF pattern) ─────────────────────────────────────────
// scuba-frontend never talks to a database directly — it calls the existing
// ScubaHub.Api backend server-to-server, exactly like admin-frontend's api.js
// did from the browser. CORS does not apply to these server-side calls.
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var baseUrl = builder.Configuration["Backend:BaseUrl"]
                  ?? "http://localhost:5054";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
