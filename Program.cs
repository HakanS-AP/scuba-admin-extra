using Microsoft.AspNetCore.Authentication.Cookies;
using ScubaHub.AdminWeb.Services;
using System.Security.Cryptography.X509Certificates;

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

// ── Cloudflare origin-cert gate (mutual TLS via Azure) ───────────────────────
// SECURITY: this check is only as strong as two platform guarantees, because
// the header carries only the PUBLIC certificate (not a secret) — it proves
// "this is the expected cert", not "the caller owns its private key":
//   1. Azure App Service "Incoming client certificates" MUST be set to Require,
//      so the platform strips any inbound X-ARR-ClientCert and only sets it
//      after a real mutual-TLS handshake (that is where key possession is proven).
//   2. The app MUST be unreachable except through Azure's front end. If the
//      default *.azurewebsites.net host or any sidecar port is exposed, anyone
//      can forge this header with the (public) cert and pass.
// Skipped in Development — there is no Azure front end locally to inject the
// header, so leaving it on would 403 every request (including static files).
app.Use(async (context, next) =>
    {
        // Azure WebApp serves the certificate through the X-ARR-ClientCert header field
        var header = context.Request.Headers["X-ARR-ClientCert"].FirstOrDefault();
        if (string.IsNullOrEmpty(header)) { context.Response.StatusCode = 403; return; }
        try
        {
            using var cert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(header));
            var expected = Environment.GetEnvironmentVariable("CF_CLIENT_THUMBPRINT");
            var now = DateTime.UtcNow;
            if (!string.Equals(cert.Thumbprint, expected, StringComparison.OrdinalIgnoreCase)
                || now < cert.NotBefore.ToUniversalTime()
                || now > cert.NotAfter.ToUniversalTime())
            {
                context.Response.StatusCode = 403;
                return;
            }
        }
        catch
        {
            context.Response.StatusCode = 403;
            return;
        }
        await next();
    });


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
