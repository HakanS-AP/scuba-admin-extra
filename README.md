# scuba-frontend

An **ASP.NET Core Razor Pages** rebuild of `admin-frontend` (which was React + Vite).
Same functionality, same dark GitHub-style UI — but rendered entirely server-side
in .NET, with no JavaScript framework.

## How it maps to the React app

| admin-frontend (React)          | scuba-frontend (ASP.NET Core)                         |
| ------------------------------- | ----------------------------------------------------- |
| `pages/Login.jsx`               | `Pages/Login.cshtml` + `Login.cshtml.cs`              |
| `pages/Dashboard.jsx`           | `Pages/Dashboard.cshtml` + `Dashboard.cshtml.cs`      |
| `components/ProtectedRoute.jsx` | Cookie auth + `AuthorizeFolder("/")` in `Program.cs`  |
| `api.js` (browser `fetch`)      | `Services/AdminApiClient.cs` (typed `HttpClient`)     |
| `server.js` runtime `__ENV__`   | `appsettings.json` → `Backend:BaseUrl`                |
| Client-side edit state          | `?edit=<index>` query string + Post-Redirect-Get      |

## Architecture (BFF)

Like the React app, scuba-frontend does **not** own the data — it calls the existing
`backend` (`ScubaHub.Api`) over `/api/admin/*`. The difference: the React app called
from the browser (so CORS + an HttpOnly cookie applied); this app calls server-to-server.

On login the admin password is validated against `/api/admin/auth/verify`. On success
the key is stored as a claim inside ASP.NET's encrypted auth cookie, and
`AdminApiClient` replays it as the `admin_session` cookie header on each backend call.

## Run

```bash
# 1. Start the backend first (in ../backend):
dotnet run            # listens on http://localhost:5054

# 2. Then this app:
dotnet run            # listens on http://localhost:5184
```

Log in with the backend's `AdminSettings:Key` (in dev: `dev-admin-key-change-me`).
