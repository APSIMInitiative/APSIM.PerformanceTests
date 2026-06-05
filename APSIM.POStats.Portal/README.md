# APSIM.POStats.Portal

ASP.NET Core web portal for viewing predicted vs observed statistics for pull requests.

Prerequisites
- .NET SDK 8.0+
- Environment variable `PORTAL_DB` (see notes below)

Database
- For local development set `PORTAL_DB=portal.db` to use SQLite (create `portal.db` at the repo root).
- To use MySQL provide a full connection string in `PORTAL_DB` (e.g. `Server=...;Database=...;User=...;Password=...;`).

Build & Run (local)
- `dotnet build APSIM.PerformanceTests.sln`
- `dotnet run --project APSIM.POStats.Portal/APSIM.POStats.Portal.csproj`

Development
- Default dev URLs: `https://localhost:5001` and `http://localhost:5000`.
- Open `/` to see the landing page; open `/{pullRequestNumber}` to view a PR report.
- Use the `Portal` launch configuration or the compound `Portal + Collector` for debugging both services.

Docker
- Build and run via `./build.sh` and `./deploy.sh`. Local service runs at `http://localhost:8081/`.

Troubleshooting
- Error: Cannot find environment variable `PORTAL_DB` — ensure it is set in your environment or run profile.
- HTTPS cert warnings: `dotnet dev-certs https --trust`.
