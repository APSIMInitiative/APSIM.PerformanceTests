# APSIM.POStats.Shared

Shared models, utilities, and database helpers used by the Portal and Collector projects.

Contents (high level)
- `Collector.cs`, `PullRequestTimer.cs` — collector orchestration helpers
- `StatsDbContext.cs`, `SqliteUtilities.cs` — database helpers and EF context
- `GitHub/` — GitHub API helpers and PR models
- `Comparison/` — comparison utilities used for validation reports

Build
- This project is built as part of the solution: `dotnet build APSIM.PerformanceTests.sln`.

Usage
- Refer to this project from `APSIM.POStats.Collector` and `APSIM.POStats.Portal` for models and utilities.

Notes
- Keep shared, framework-agnostic code here. Avoid adding UI or app-host-specific behaviour.
