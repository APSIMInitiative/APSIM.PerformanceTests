# Predicted Observed Stats

## To build, run and test docker image from a local repo
The Docker image in the repo is set up to locally build the project with the ./build.sh file

Then to run it locally, call the ./deploy.sh script

Service runs on http://localhost:8081/


## To build, run and test docker image on the web server

Refer to apsim-web repo which has a server-wide deployment script that is used.


## Local development

This repository contains:

- [APSIM.POStats.Portal/README.md](APSIM.POStats.Portal/README.md): ASP.NET Core web portal for viewing predicted/observed stats.
- [APSIM.POStats.Collector/README.md](APSIM.POStats.Collector/README.md): console app that uploads validation data.
- [APSIM.POStats.Shared/README.md](APSIM.POStats.Shared/README.md): shared models and utilities.
- APSIM.POStats.Tests: automated tests.

### Prerequisites

- .NET SDK 8.0+
- Git
- Optional: Docker Desktop (for container workflow)
- Optional: VS Code with C# Dev Kit

### Quick start (local, no Docker)

1. Clone and enter the repository.
2. Create or confirm a local sqlite database file at the repo root named portal.db.
3. Set environment variables (required).
4. Build and run the portal.

#### 1) Create local environment file

Create or update .env in the repo root with at least:

PORTAL_DB=portal.db

How it works:

- If PORTAL_DB contains .db, the app uses sqlite.
- Otherwise, the app treats PORTAL_DB as a MySQL connection string.

#### 2) Build the solution

dotnet build APSIM.PerformanceTests.sln

#### 3) Run the portal

dotnet run --project APSIM.POStats.Portal/APSIM.POStats.Portal.csproj

By default, development profile URLs are:

- https://localhost:5001
- http://localhost:5000

Home page behavior:

- Navigate to / to see the landing page and recent pull requests.
- Navigate to /{pullRequestNumber} to open a specific PR report.

### Run with VS Code (recommended)

The repo includes launch and task configuration:

- Launch config: Portal
- Launch config: Collector
- Compound launch: Portal + Collector
- Pre-launch task: Build Solution

From Run and Debug, start Portal + Collector to launch both apps together.

### Running the collector locally

Collector expects command-line arguments when run directly. Example shape:

pullRequestNumber commitId author validationPath

If you use VS Code launch configuration, arguments can be supplied in launch settings or debugger UI.

### Docker workflow (existing)

Local image build and run:

- ./build.sh
- ./deploy.sh
- Service: http://localhost:8081/

Server deployment:

- Use apsim-web repository deployment process.

### Tests

Run all tests:

dotnet test APSIM.PerformanceTests.sln

### Troubleshooting

- Error: Cannot find environment variable PORTAL_DB
	- Ensure PORTAL_DB is set in your environment or injected by your run profile.

- Portal starts but no PR data appears
	- This is expected until collector/API has inserted pull request data.

- HTTPS certificate warnings locally
	- Trust the local .NET dev certificate if prompted:
	- dotnet dev-certs https --trust

- Database provider confusion
	- sqlite: PORTAL_DB=portal.db
	- mysql: PORTAL_DB=Server=...;Port=...;Database=...;User=...;Password=...;

### Suggested local workflow

1. Set PORTAL_DB=portal.db.
2. Build solution.
3. Start Portal + Collector in VS Code.
4. Open portal at http://localhost:5000.
5. Use root page to navigate recent pull requests.