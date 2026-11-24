# RentalPortal

An ASP.NET Core MVC (.NET 10) rental portal. Property managers maintain properties and units and
review rental applications; applicants browse available units and apply for them.

## Quick start

You need the .NET 10 SDK and SQL Server LocalDB (see [Prerequisites](#prerequisites)). The app
creates and seeds its own database, so there is nothing else to set up.

1. Clone the repository and run the app:

   ```powershell
   git clone https://github.com/breps83/troy-rentalportal.git
   cd troy-rentalportal
   dotnet run --project src/RentalPortal.Web --launch-profile http
   ```

2. Open **http://localhost:5164** in a browser.
3. Sign in with one of these demo accounts:

   | Role | Email | Password |
   |---|---|---|
   | Property manager | `manager@rentalportal.test` | `Password1**` |
   | Property manager | `manager2@rentalportal.test` | `Password1**` |
   | Applicant | `applicant@rentalportal.test` | `Password1**` |
   | Applicant | `applicant2@rentalportal.test` through `applicant8@rentalportal.test` | `Password1**` |

   The password is `Password1**` with the two asterisks at the end. You can also register a new
   account; email confirmation is off, so it can sign in right away.

**Optional: HTTPS.** The `http` profile needs no certificate. To run over HTTPS, trust the .NET
development certificate once, then use the `https` profile and open https://localhost:7153:

```powershell
dotnet dev-certs https --trust
dotnet run --project src/RentalPortal.Web --launch-profile https
```

Without the trusted certificate, the app still runs over HTTPS, but the browser shows a security
warning. See [Run](#run) for more.

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
| [claude-code.md](docs/claude-code.md) | How Claude was used on this project (docs, requirement checks, reviews, and tests, not writing the app), and the skills used here |

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
dotnet run --project src/RentalPortal.Web --launch-profile http     # http://localhost:5164
dotnet run --project src/RentalPortal.Web --launch-profile https    # https://localhost:7153
```

The `http` profile needs no certificate. On startup it logs the warning "Failed to determine the
https port for redirect", which is harmless. The `https` profile also listens on
http://localhost:5164 and redirects it to HTTPS. Without a trusted development certificate, the
browser shows a security warning; run `dotnet dev-certs https --trust` once to trust it.

### Demo logins

Every demo account uses the password `Password1**`. The accounts are listed in
[Quick start](#quick-start). Email confirmation is turned off, so a newly registered account can sign
in right away, and no email service needs to be configured.

## Testing

There are three test projects, all xUnit:

| Project | What it checks | Database |
|---|---|---|
| `tests/RentalPortal.Tests` | Unit tests for the business rules in the domain and services | None |
| `tests/RentalPortal.Integration.Tests` | The app in memory over HTTP (`WebApplicationFactory`): permissions on requests the UI doesn't offer, EF queries that repeat a domain rule, restricted deletes, modal responses, and antiforgery | Its own LocalDB database, `RentalPortalIntegration` |
| `tests/RentalPortal.E2E.Tests` | End-to-end tests in a real browser (Playwright, Chromium): every screen, and every requirement in the checklist that a browser can verify | Its own LocalDB database, `RentalPortalE2E` |

Build the tests to a temp folder, not `bin/`. A running copy of the app locks
`src/RentalPortal.Web/bin`, so a normal build fails while the app is open.

### Using a SQL Server other than LocalDB

The integration and E2E tests use LocalDB by default. To run them against another SQL Server (for
example, on macOS or Linux, or in Docker), set **`RENTALPORTAL_TEST_SQL`** to a connection string
for that server before running the tests:

```powershell
# PowerShell
$env:RENTALPORTAL_TEST_SQL = "Server=localhost,1433;User Id=sa;Password=<your password>;TrustServerCertificate=True"
```

```bash
# bash
export RENTALPORTAL_TEST_SQL="Server=localhost,1433;User Id=sa;Password=<your password>;TrustServerCertificate=True"
```

- **Leave out the database name.** The tests always use their own databases,
  `RentalPortalIntegration` and `RentalPortalE2E`, and replace any `Database=` in the variable. Each
  run **drops and recreates** those two databases, so this keeps the tests from deleting any other
  database on the server.
- The login needs permission to create and drop databases (`sa` or `dbcreator`).
- Unset the variable to go back to LocalDB.
- The app itself uses a different variable, `ConnectionStrings__DefaultConnection` (see
  [Database](#database)).

To start a SQL Server in Docker for this:

```powershell
docker run -d --name rentalportal-sql -p 1433:1433 -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=<your password>" mcr.microsoft.com/mssql/server:2025-latest
```

### Unit tests

```powershell
dotnet build tests/RentalPortal.Tests -o $env:TEMP/RentalPortal.Tests
dotnet test $env:TEMP/RentalPortal.Tests/RentalPortal.Tests.dll
```

### Integration tests

**You need:** SQL Server LocalDB, or another SQL Server set in `RENTALPORTAL_TEST_SQL` (see
[above](#using-a-sql-server-other-than-localdb)). You don't need to start the app.

```powershell
dotnet build tests/RentalPortal.Integration.Tests -o $env:TEMP/RentalPortal.Integration.Tests
dotnet test $env:TEMP/RentalPortal.Integration.Tests/RentalPortal.Integration.Tests.dll
```

Each run drops and recreates `RentalPortalIntegration`, and the app's startup migrates and seeds it.
Tests create their own properties, units, and applications.

### End-to-end tests

**You need:** SQL Server LocalDB, or another SQL Server set in `RENTALPORTAL_TEST_SQL` (see
[above](#using-a-sql-server-other-than-localdb)), and PowerShell 7 (`pwsh`). The script installs
Chromium the first time. You don't need to start the app: the tests start it themselves, on a random
port.

**On Linux**, Chromium also needs some system libraries that the script doesn't install. Install them
once, as root or with `sudo` (this uses `apt`, so it works on Debian and Ubuntu):

```bash
dotnet build tests/RentalPortal.E2E.Tests -o /tmp/RentalPortal.E2E.Tests
pwsh /tmp/RentalPortal.E2E.Tests/playwright.ps1 install --with-deps chromium
```

Windows and macOS don't need this step.

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
