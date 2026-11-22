---
name: run-e2e-tests
description: Run the Playwright E2E tests with annotated videos and traces, then report results by requirement. Use when the user asks to run the E2E or browser tests, check that the requirements or screens still work end to end, or record test videos.
---

# Run the E2E tests

Runs `tests/RentalPortal.E2E.Tests` through `run-e2e.ps1`, which records an annotated video and a
trace of each test by default. See `docs/e2e-tests.md` for details.

## 1. Choose what to run

Take the scope from the user's request:

| Request | Command |
|---|---|
| everything (default) | `./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open` |
| one requirement | `./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open -Filter "Requirement=FR 3.b"` |
| one area or class | `./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open -Filter "FullyQualifiedName~ReviewTests"` |
| one test | `./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open -Filter "FullyQualifiedName~ReviewTests.Deny_WithComment_IsTerminal"` |
| quick, no videos | add `-NoVideo` |
| watch the browser | add `-Headed` |

Combine filters with `|` (or) and `&` (and), for example `"Requirement=FR 4.a|Requirement=FR 4.b"`.

**Always pass `-Open`** when running through this skill. It opens the report
(`TestResults/e2e/latest/index.html`) in the user's browser when the run finishes. The report shows
every test grouped by requirement, failures first, each with its steps, error, and video.

- Run it with the PowerShell tool and a timeout of 10 minutes.
- The app doesn't need to be running. The tests start it, on a fresh `RentalPortalE2E` database.
- A running dev app is fine: it uses a different database and port, and the script builds the tests to
  `$env:TEMP/RentalPortal.E2E.Tests`, not `bin/`, so the app's locked files don't matter. Never build
  the tests to `bin/`.

## 2. Report

Read the results from the `e2e.trx` file in the output folder the script prints
(`TestResults/e2e/<timestamp>/`). Each test's `StdOut` lists its `[ID] step` lines.

- **Summary:** passed, failed, and total, plus the duration.
- **By requirement:** group the tests by their `Requirement` trait (see `docs/e2e-coverage.md`).
  Give each requirement ID ✅ or ❌.
- **Each failure:**
  - the test name and the step it failed in (the last `[ID] step` line before the error)
  - the assertion message, trimmed to what matters (expected vs. received)
  - the trace path, and how to open it:
    `pwsh $env:TEMP/RentalPortal.E2E.Tests/playwright.ps1 show-trace <trace path>`
  - your read on whether the app is wrong or the test is out of date. Say which, and what you
    checked (the view, the controller message, or a frame of the video).
- **Report:** end with the report's path on its own line, as plain text, not a link (long links
  break when the chat wraps them). With videos that's `TestResults\e2e\latest\index.html`; with
  `-NoVideo` it's the `index.html` in the run's folder. Say it opened in the browser.
  Don't list a link per video; the report has them all.

If the run fails before any tests run (build error, browser install, LocalDB), report that error and
the fix from the troubleshooting table in `docs/e2e-tests.md`.

## Rules

- Don't change test code or app code while running the tests. Offer a fix and wait. Use the
  `e2e-tests` skill to update tests.
- Don't re-run failing tests until they pass. If a test fails once and passes on a second run, report
  it as flaky, with both results.
- Don't delete old output folders under `TestResults/e2e` unless the user asks.
