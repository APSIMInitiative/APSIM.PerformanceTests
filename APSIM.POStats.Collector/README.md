# APSIM.POStats.Collector

Console app that uploads validation data for pull request reports.

Prerequisites
- .NET SDK 8.0+

Build
- `dotnet build APSIM.PerformanceTests.sln`

Run (direct)
- `dotnet run --project APSIM.POStats.Collector/APSIM.POStats.Collector.csproj -- <pullRequestNumber> <commitId> <author> <validationPath>`

Run (VS Code)
- Use the `Collector` launch configuration (or the compound `Portal + Collector`).

Notes
- The collector is intended to be run regularly (CI or scheduled) or launched from the debugger to insert PR validation data into the portal database.
- Shared utilities and DB context are in the `APSIM.POStats.Shared` project.
