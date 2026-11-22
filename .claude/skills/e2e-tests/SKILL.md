---
name: e2e-tests
description: Write, update, or extend the Playwright E2E tests in tests/RentalPortal.E2E.Tests and keep docs/e2e-coverage.md in sync. Use when the user asks to add or fix E2E tests, cover a requirement or screen in the browser, or after a feature or view changes and its E2E tests need updating.
---

# Write and maintain E2E tests

The goal: every screen works, and every ticked item in `docs/requirements-checklist.md` is verified in
a real browser, or `docs/e2e-coverage.md` explains why E2E doesn't apply. Read `docs/e2e-tests.md`
first for how the project works.

## 1. Find what needs tests

- Read `docs/requirements-checklist.md`, `docs/e2e-coverage.md`, and the test classes in
  `tests/RentalPortal.E2E.Tests/Tests`.
- Find what changed: `git status --short`, `git log --oneline -10`, and `git diff` on
  `src/RentalPortal.Web/Controllers`, `Views`, `ViewComponents`, and `wwwroot/js/site.js`.
- List the gaps:
  - ticked checklist items with no test (⬜ in the coverage doc, or missing from it)
  - new screens, modals, or buttons with no test
  - tests whose selectors or messages no longer match the views (button text, labels, TempData
    messages, `ModelState` errors)
- Tell the user the plan (which tests you'll add or change, and why) before writing many tests.

## 2. Write the tests

Follow the rules in `docs/e2e-tests.md` ("Writing a test"). In short:

- One class per area in `Tests/`, inheriting `E2ETestBase(fixture, output)`. Add to an existing
  class when the area exists.
- Name tests by behavior: `Action_Condition_Result`, for example `DeleteUnit_WithApplications_IsRefusedWithMessage`.
- `[Trait("Requirement", "FR 3.b")]` for each checklist ID verified. Use `Roles` for the roles section
  and `Screens` for smoke tests. Use IDs exactly as the checklist has them.
- `await Step("FR 3.b", "...")` before each check. Write the description as plain behavior for
  someone watching the video.
- Arrange with `Data` (`TestData`): `CreatePropertyAsync`, `CreateUnitAsync`, `CreateApplicantAsync`,
  `CreateApplicationAsync(unitId, email, status)`. Add a helper there if you need new data, and
  build it through the app's services and domain methods, never raw SQL. Each test creates its own
  data and never changes seeded rows other tests read.
- Act and assert through the UI:
  - locators by role and label, scoped to a row or refresh target
  - `Modal.OpenAsync`, `Modal.Field`, `Modal.SubmitAsync`, `Modal.SubmitExpectingErrorAsync`
  - `EnterAsync` and `ChooseAsync` (`FieldExtensions`), never `FillAsync` or `SelectOptionAsync`
    directly, so the cursor moves to each field in videos
  - web-first `Expect(...)` assertions; never fixed waits or `WaitForTimeoutAsync`
- Check the server as well as the UI when a requirement says "enforced on the server". Use
  `PostFormAsync` for crafted posts, or inject a forbidden option, as
  `UnitsTests.InactiveUnitType_*` does.
- Copy expected text from the view or controller rather than guessing it. If a selector would need
  a CSS class or position because the view has no label or role, stop and ask the user before adding
  `data-testid` or changing the view.
- Follow the repo conventions in `CLAUDE.md`: plain C#, `_camelCase` fields, lines under 120
  characters, American English.
- Don't change app code to make a test pass. If a test finds a bug, keep the test, report the bug,
  and let the user decide.

## 3. Run what you changed

```powershell
./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -NoVideo -Filter "FullyQualifiedName~<Class>"
```

- Fix failures in the test, not the app, unless the app is wrong. If the app is wrong, report it.
- When a modal or page doesn't behave as expected, re-run with video and look at the frames, or open
  the trace, before changing the test.
- Run the affected class twice. A test that fails only sometimes is flaky: find out why (usually a
  missing wait on a refresh or a redirect) rather than adding retries.
- Finish with the full suite (`run-e2e.ps1 -NoVideo`) with no build warnings.
- Always build the tests to a temp folder (`run-e2e.ps1` does), never `bin/`. A running copy of the app
  locks `src/RentalPortal.Web/bin`.

## 4. Update the coverage doc

In `docs/e2e-coverage.md`:

- Mark an item ✅ only when a **passing** test checks it. Name the test (`Class.Method`).
- Mark ➖ for items a browser can't verify (unit tests, code structure, "done in the database"), and
  say what verifies them instead.
- Use ⬜ for items still uncovered.
- Update the "Status as of" date and the test count.
- Never change `docs/requirements.md`. Don't tick the checklist from here; that is the
  `update-checklist` skill's job, and it is based on the code.

## 5. Report

- Tests added or changed, grouped by requirement ID
- The result of the full run (passed / failed / total)
- Requirements still ⬜, and why
- Bugs or UX problems the tests found in the app (with the test that shows it), for the user to decide
