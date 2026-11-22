# E2E Tests (Playwright)

Browser tests that check every screen works and that each requirement in
[requirements-checklist.md](requirements-checklist.md) behaves as described. Which tests cover which
items is in [e2e-coverage.md](e2e-coverage.md).

- **Project:** `tests/RentalPortal.E2E.Tests` (xUnit, [Microsoft.Playwright](https://playwright.dev/dotnet/))
- **Browser:** Chromium, headless unless you ask for headed
- **App:** started by the tests themselves, in-process on a random port. You don't need to run it first.
- **Database:** its own LocalDB database, `RentalPortalE2E`. Your `RentalPortalDb` is never touched.

## Setup (once)

1. Build the project to a temp folder: `dotnet build tests/RentalPortal.E2E.Tests -o $env:TEMP/RentalPortal.E2E.Tests`.
   Tests are always built to a temp folder, never `bin/`, because a running copy of the app locks
   `src/RentalPortal.Web/bin`.
2. Install Chromium for the installed Playwright version:

   ```powershell
   pwsh $env:TEMP/RentalPortal.E2E.Tests/playwright.ps1 install chromium
   ```

   `run-e2e.ps1` does this step for you. Repeat it after upgrading the `Microsoft.Playwright` package.

You need LocalDB, which is already required by the app.

## Running

### Visual Studio Test Explorer

The tests appear under `RentalPortal.E2E.Tests`, with the trait `Category: E2E` and one
`Requirement` trait per checklist ID. Group by **Traits** to run one requirement.

**No videos by default.** To record them:

1. **Test > Configure Run Settings > Select Solution Wide runsettings File**
2. Choose `tests/RentalPortal.E2E.Tests/e2e-video.runsettings`
3. Run the tests. To stop recording, choose **Auto Detect runsettings Files** again.

Each test's output shows its steps (`[FR 3.b] Continue validates and saves`) and, when recording,
the paths of its video and trace.

### Command line, with videos (recommended for reviewing)

```powershell
./tests/RentalPortal.E2E.Tests/run-e2e.ps1                                       # everything, with video
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "Requirement=FR 3.b"          # one requirement
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "FullyQualifiedName~ReviewTests"  # one class
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Headed                               # watch the browser
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -NoVideo                              # fast, no video or trace
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open                                 # open the report when done
```

Every run through the script writes a report: **`TestResults/e2e/latest/index.html`**. Open it in a
browser to see every test grouped by requirement, failures first, each with its steps, error, and
video. The run skill (`/run-e2e-tests`) passes `-Open`, so the report opens by itself. To rebuild the
report for an older run:
`./tests/RentalPortal.E2E.Tests/write-e2e-report.ps1 -RunFolder TestResults/e2e/<timestamp>`.

### Plain dotnet test

Build to a temp folder first, then test the built DLL:

```powershell
dotnet build tests/RentalPortal.E2E.Tests -o $env:TEMP/RentalPortal.E2E.Tests
dotnet test $env:TEMP/RentalPortal.E2E.Tests/RentalPortal.E2E.Tests.dll                        # all E2E, no video
dotnet test $env:TEMP/RentalPortal.E2E.Tests/RentalPortal.E2E.Tests.dll --filter "Requirement=FR 3.b"
```

Unit tests only: build `tests/RentalPortal.Tests` the same way, or run
`dotnet test RentalPortal.slnx --filter "Category!=E2E"` when the app isn't running.

### Settings

All settings are environment variables, which the script and the runsettings file set for you.

| Variable | Effect | Default |
|---|---|---|
| `E2E_VIDEO=1` | Record an annotated video of each test (also turns on traces) | off |
| `E2E_TRACE=1` | Save a Playwright trace of each test | off, or on with video |
| `E2E_HEADED=1` | Show the browser window | off |
| `E2E_SLOWMO=<ms>` | Pause on each browser action (`run-e2e.ps1 -SlowMo <ms>`) | 600 with video or headed, otherwise 0 |
| `E2E_OUTPUT=<folder>` | Where videos and traces go | `TestResults/e2e/<timestamp>` |

## Videos, annotations, and traces

Output goes to `TestResults/e2e/<timestamp>/` at the repo root, which git ignores:

```
index.html                   the report: every test with its video (run-e2e.ps1 only)
videos/<Class>.<Test>.webm   one video per test; a Theory adds the case's values (..._Draft.webm)
traces/<Class>.<Test>.zip    one trace per test
e2e.trx                      results, when run through run-e2e.ps1
```

`TestResults/e2e/latest/index.html` is the report for the most recent run **with videos**, at a path
that never changes. Runs with `-NoVideo` write their own `index.html` but leave `latest` alone.

Videos are annotated with Playwright's screencast API (`Page.Screencast`):

- **Chapter cards:** each `Step(...)` in a test shows a card with the requirement ID and what's being
  checked, for example "FR 3.e: Submit is refused with an error when the unit has an active lease".
  The test waits 1.5 seconds on each card before the step's actions start, and holds the last frame
  for 1.5 seconds at the end.
- **A banner** in the bottom-left corner keeps the current step visible.
- **Action highlights:** each click or fill shows the cursor, outlines the element, and names the action.

Open a video in any browser, or in VS Code. Open a trace in the Playwright trace viewer; it groups
actions by step and shows the page before and after each one:

```powershell
pwsh $env:TEMP/RentalPortal.E2E.Tests/playwright.ps1 show-trace TestResults/e2e/<timestamp>/traces/<file>.zip
```

## How it works

| Piece | File | Job |
|---|---|---|
| App host | `Infrastructure/E2EAppFactory.cs` | `WebApplicationFactory<Program>` with .NET 10's `UseKestrel(0)`, so a real browser can reach it. Points the connection string at `RentalPortalE2E`. |
| Fixture | `Infrastructure/E2EFixture.cs` | Once per run: drops `RentalPortalE2E`, starts the app (which migrates and seeds), and launches Chromium. |
| Base class | `Infrastructure/E2ETestBase.cs` | Per test: a new browser context (own cookies, own video), tracing, `Step`, `LoginAsAsync`, `SwitchUserAsync`, `PostFormAsync`. Fails the test on a script error or a 5xx response. |
| Modal helper | `Infrastructure/Modal.cs` | Opens `#app-modal` from a trigger, fills fields by label, and waits for the modal to close or show an error. |
| Test data | `Infrastructure/TestData.cs` | Creates properties, units, applicants, and applications in any status through the app's own services. |
| Settings | `Infrastructure/E2ESettings.cs` | Reads the environment variables above. |

### Database strategy

One fresh database per run, not per test:

- **Why not per test:** migrating and seeding takes seconds each time, and the app is shared by every
  test, so swapping its database between tests would be fragile.
- **Why not the dev database:** the seeder never deletes anything, so data from earlier runs would
  pile up. Also, approving an application leases its unit for 12 real months, so a second run would
  find units already leased.
- **How tests stay independent:** each test that changes data creates its own property or unit,
  with a unique name such as `E2E Lofts 7f3a`. Tests log in as the seeded users
  (`manager@rentalportal.test`, `applicant@rentalportal.test`, password `Password1**`) but never
  change data that other tests read. They can run in any order.
- After a run, `RentalPortalE2E` is left in place so you can inspect it. The next run drops it.

## Writing a test

```csharp
public class UnitsTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    [Fact]
    [Trait("Requirement", "FR 2.b")]
    public async Task InactiveUnitType_CantBeChosen()
    {
        var property = await Data.CreatePropertyAsync();          // arrange through services: fast

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Properties/Details/{property.Id}");

        await Step("FR 2.b", "A new unit can't choose the inactive type");   // chapter card in the video
        await Modal.OpenAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Add unit" }), "Add unit");
        await Expect(Modal.Field("Unit type").Locator("option", new() { HasText = "Penthouse" }))
            .ToHaveCountAsync(0);
    }
}
```

Rules:

- **Inherit `E2ETestBase`.** It puts the test in the shared `E2E` collection and adds `Category: E2E`.
- **Add a `[Trait("Requirement", "...")]`** for each checklist ID the test verifies, using the IDs from
  the checklist. Use `Roles` for the "Roles and permissions" section and `Screens` for smoke tests.
- **Call `Step(id, description)`** before each thing you verify. The description is what a viewer
  reads in the video, so write it as plain behavior.
- **Arrange with `Data`, act and assert through the UI.** Create your own property or unit; don't
  change seeded rows.
- **Find elements by role and label** (`GetByRole`, `GetByLabel`, `Modal.Field("Unit number")`),
  scoped to a row or one of the refresh targets (`#property-list`, `#unit-list`,
  `#residence-history`, `#application-status`). Don't use CSS classes or positions.
- **Use `EnterAsync` and `ChooseAsync`** instead of `FillAsync` and `SelectOptionAsync`, for example
  `Modal.Field("Unit number").EnterAsync("305")`. In videos they move the cursor to the field first, so
  a viewer can follow along. Playwright's cursor only moves for clicks and hovers.
- **Use web-first assertions** (`Expect(...).ToBeVisibleAsync()`), which retry. Never use fixed waits.
- **Use `Modal.SubmitAsync()`** to save a modal. It waits for the modal to close.
  `SubmitExpectingErrorAsync(message)` waits for an error.
- **Update [e2e-coverage.md](e2e-coverage.md)** when you add or change a test.

The `/e2e-tests` skill in `.claude/skills` follows these rules when Claude writes or updates tests.
The `/run-e2e-tests` skill runs them and reports results by requirement.

## Troubleshooting

| Problem | Fix |
|---|---|
| `Executable doesn't exist at ...ms-playwright\chromium...` | Run the browser install command above, or `run-e2e.ps1`, which installs it. |
| Can't connect to LocalDB, or a timeout creating the database | Run `sqllocaldb start mssqllocaldb`. The first run creates the database, which takes longer. |
| The build fails because `bin` is locked | The running app locks `src/RentalPortal.Web/bin`. Build the tests to a temp folder (`-o $env:TEMP/RentalPortal.E2E.Tests`), as `run-e2e.ps1` does. Visual Studio builds to `bin/`, so stop the app before running the tests from Test Explorer. |
| A modal test fails waiting for `#app-modal` to be hidden | The save probably failed. Look at the video's last frame, or open the trace, for the error the modal showed. |
| A click after fixing a validation error does nothing | The error message probably disappeared on the click and moved the button. `site.js` re-checks a field with an error as soon as its value changes, to prevent this. Check that the field's form has client validation (`_ValidationScriptsPartial`, or a modal form re-parsed by `site.js`). |
| `The page reported errors` after the test passed | The page threw a script error or the server returned a 5xx. The message lists them. Call `AllowPageErrors()` only if the test causes the error deliberately. |
