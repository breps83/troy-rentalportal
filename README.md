# RentalPortal

An ASP.NET Core MVC (.NET 10) rental portal. Property managers maintain properties and units and
review rental applications; applicants browse available units and apply for them.

## Documentation

| Doc | What it covers |
|---|---|
| [requirements.md](docs/requirements.md) | The assessment spec, with requirement IDs such as FR 3.b and TR 1.b |
| [requirements-checklist.md](docs/requirements-checklist.md) | Progress against each requirement, naming the code that satisfies it |
| [diagrams.md](docs/diagrams.md) | Progress by area, roles, domain model, status lifecycle, page and modal flows |
| [e2e-tests.md](docs/e2e-tests.md) | How the E2E tests work, their settings, writing one, and troubleshooting |
| [e2e-coverage.md](docs/e2e-coverage.md) | Which E2E test verifies each checklist item |
| [ef-commands.md](docs/ef-commands.md) | EF Core CLI commands for migrations and the database, and pitfalls |
| [interview-prep.md](docs/interview-prep.md) | What to expect in the technical interview, and the pre-interview checklist |
| [claude-code.md](docs/claude-code.md) | Working with Claude Code: project skills and the built-in skills used here |

## Getting started

### Prerequisites

- **.NET SDK 10.0.404** or a later 10.0 feature band. `global.json` pins the SDK, so an older one
  fails at build time.
- **SQL Server LocalDB**, instance `(localdb)\mssqllocaldb`. It comes with the Visual Studio "Data
  storage and processing" workload or the SQL Server Express installer. LocalDB is Windows only; on
  macOS or Linux, point the connection string at another SQL Server (for example, a Docker container).

### Database

- The connection string is `ConnectionStrings:DefaultConnection` in
  `src/RentalPortal.Web/appsettings.json` and uses the `RentalPortalDb` database. To use a different
  server without editing the file, set the `ConnectionStrings__DefaultConnection` environment variable.
- **No `dotnet ef database update` is needed.** The app creates the database and applies migrations
  on startup.
- **Demo data is seeded only in the Development environment**, and only when the database has no
  users yet. `dotnet run` and Visual Studio use Development through `launchSettings.json`. To start
  over with fresh demo data, drop the database and run the app again:

  ```powershell
  dotnet ef database drop --project src/RentalPortal.Web
  ```

  This needs the EF Core CLI (`dotnet tool install --global dotnet-ef`). See
  [docs/ef-commands.md](docs/ef-commands.md) for this and the other EF Core commands.

### Run

```powershell
dotnet run --project src/RentalPortal.Web --launch-profile https
```

The app serves on https://localhost:7153 (and http://localhost:5164). On a new machine, trust the
development HTTPS certificate first with `dotnet dev-certs https --trust`.

### Demo logins

Every demo account uses the password `Password1**`.

| Role | Accounts |
|---|---|
| Property manager | `manager@rentalportal.test`, `manager2@rentalportal.test` |
| Applicant | `applicant@rentalportal.test`, `applicant2@rentalportal.test` through `applicant8@rentalportal.test` |

Email confirmation is turned off, so a newly registered account can sign in right away, and no email
service needs to be configured.

## Testing

There are two test projects, both xUnit:

| Project | What it checks | Database |
|---|---|---|
| `tests/RentalPortal.Tests` | Unit tests for the business rules in the domain and services | None |
| `tests/RentalPortal.E2E.Tests` | End-to-end tests in a real browser (Playwright, Chromium): every screen, and every requirement in the checklist that a browser can verify | Its own LocalDB database, `RentalPortalE2E` |

Build the tests to a temp folder, not `bin/`. A running copy of the app locks
`src/RentalPortal.Web/bin`, so a normal build fails while the app is open.

### Unit tests

```powershell
dotnet build tests/RentalPortal.Tests -o $env:TEMP/RentalPortal.Tests
dotnet test $env:TEMP/RentalPortal.Tests/RentalPortal.Tests.dll
```

### End-to-end tests

**You need:** SQL Server LocalDB (the same as the app) and PowerShell 7 (`pwsh`). The script installs
Chromium the first time. You don't need to start the app: the tests start it themselves, on a random
port.

Run them with the script:

```powershell
./tests/RentalPortal.E2E.Tests/run-e2e.ps1                                         # all tests, with video
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open                                   # open the report when done
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "Requirement=FR 3.b"            # one requirement
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "FullyQualifiedName~ReviewTests"  # one test class
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -NoVideo                                # fast, no video
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Headed                                 # watch the browser
```

- **Report:** each run writes `TestResults/e2e/<timestamp>/index.html`. It shows every test grouped
  by requirement, failures first, each with its steps, its error, and a video. The latest run with
  videos is always at **`TestResults/e2e/latest/index.html`**.
- **Videos** are annotated: a title card for each step names the requirement being checked, and the
  cursor and an action label show each click and entry. Passwords are hidden. A full run with video
  takes about 15 minutes; without video, under a minute.
- **Traces** for each test are saved next to the videos. Open one with
  `pwsh $env:TEMP/RentalPortal.E2E.Tests/playwright.ps1 show-trace <file.zip>`.
- **Database:** each run drops and recreates `RentalPortalE2E`, and the app's startup migrates and
  seeds it. Your development database, `RentalPortalDb`, is never touched. Tests create their own
  properties and units, so they don't depend on each other or on run order.

**Visual Studio Test Explorer** runs the E2E tests too, without video by default. To record video,
choose `tests/RentalPortal.E2E.Tests/e2e-video.runsettings` under **Test > Configure Run Settings >
Select Solution Wide runsettings File**. Test Explorer builds to `bin/`, so stop the app first.

More detail:

- [docs/e2e-tests.md](docs/e2e-tests.md): settings, how the tests work, how to write one, and
  troubleshooting
- [docs/e2e-coverage.md](docs/e2e-coverage.md): which test verifies each checklist item
- [docs/claude-code.md](docs/claude-code.md): Claude Code skills that run and write the E2E tests
