# RentalPortal

An ASP.NET Core MVC (.NET 10) rental portal. Property managers maintain properties and units and
review rental applications; applicants browse available units and apply for them.

- Requirements: [docs/requirements.md](docs/requirements.md)
- Progress: [docs/requirements-checklist.md](docs/requirements-checklist.md)

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

### With Claude Code

Two project skills in `.claude/skills` work with the E2E tests:

- `/run-e2e-tests` runs them with video, opens the report, and summarizes the results by requirement.
- `/e2e-tests` writes or updates tests when features or views change, and keeps
  `docs/e2e-coverage.md` up to date.
