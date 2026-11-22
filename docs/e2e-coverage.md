# E2E Coverage

Maps each item in [requirements-checklist.md](requirements-checklist.md) to the Playwright tests that verify it
in a real browser, or says why E2E doesn't apply and what verifies it instead. Tests live in
`tests/RentalPortal.E2E.Tests/Tests` and carry `[Trait("Requirement", "...")]` with the same IDs, so
`run-e2e.ps1 -Filter "Requirement=FR 3.b"` runs one requirement. See [e2e-tests.md](e2e-tests.md).

Status as of 2026-11-22: 41 tests, all passing.

**Legend:** ✅ covered by a passing E2E test · ➖ not E2E (verified another way) · ⬜ not covered yet

## Screens

Every screen opens for each role that can see it (`SmokeTests`): Home, Log in, Register, Access
denied, Available Units, Applications list, Application (all three sections), Properties, and Property
details (units). Every modal is opened by at least one test below.

## Roles and permissions

| Item | E2E | Tests |
|---|---|---|
| Two roles, seeded | ✅ | `AccountTests.Register_WithChosenAccountType_SignsInWithThatRole`; seeded logins used throughout |
| Permissions enforced in every controller | ✅ | `RolesAndPermissionsTests`: `Applicant_SeesOnlyApplicantActions` (Properties refused), `OtherApplicant_CannotViewOrEditAnApplication` (crafted post refused), `SubmittedApplication_RejectsCraftedEdit`, `Anonymous_IsSentToLogin` |
| Navbar and homepage links by role | ✅ | `RolesAndPermissionsTests.Applicant_SeesOnlyApplicantActions`, `Manager_SeesOnlyManagerActions`, `AccountTests.Register_*` |
| Application page buttons by role and status | ✅ | `Manager_SeesOnlyManagerActions`, `ApplicationWizardTests.SubmittedApplication_IsReadOnly`, `WithdrawTests.TerminalApplication_CannotBeWithdrawn` |
| List page: Applicant column only for managers | ✅ | `ApplicationListTests` (both tests) |
| Review button and history only for managers, button only while Submitted | ✅ | `Manager_SeesOnlyManagerActions` (no button on Draft), `ReviewTests.Approve_*` (history hidden from applicant), `ReviewTests.Deny_*` (no button after) |
| Applicant: create, fill out, submit; only available units | ✅ | `ApplicationWizardTests.Applicant_CompletesBothSections_AndSubmits`, `ListingsTests.ApplyForUnit_*` |
| Applicant: withdraw | ✅ | `WithdrawTests.Applicant_WithdrawsOpenApplication` (Draft, Submitted, Returned) |
| Applicant: correct and resubmit a returned application | ✅ | `ReviewTests.Return_RequiresComment_ThenApplicantCorrectsAndResubmits` |
| Manager: maintain properties and units | ✅ | `PropertiesTests`, `UnitsTests` |
| Manager: review submitted applications | ✅ | `ReviewTests` |
| Manager: approve, issuing a twelve-month lease | ✅ | `ReviewTests.Approve_IssuesTwelveMonthLease_AndRecordsHistory` |

## Technical Requirements

| Item | E2E | Tests or how it's verified |
|---|---|---|
| TR 1.a Controllers, view models, views, partials, view components | ➖ | Code structure. The pages they render are covered by `SmokeTests`; view components (unit list, residence history, application status) are exercised by `UnitsTests`, `ResidencesTests`, `ReviewTests` |
| TR 1.b Modals from partial views | ✅ | Open: every modal test. Validation re-renders the partial: `UnitsTests.AddUnit_WithDuplicateNumber_ReRendersModalWithServerError` (server), `PropertiesTests.AddProperty_ThroughModal_AppearsInList` (client), `ResidencesTests`, `ReviewTests.Return_*`. Success closes and refreshes: `PropertiesTests`, `UnitsTests`, `ResidencesTests`, `ReviewTests` (`#application-status`), `WithdrawTests` (redirect) |
| TR 1.c Structure and styling | ➖ | Code review (unchecked in the checklist) |
| TR 2.a ASP.NET Identity | ✅ | `AccountTests` |
| TR 2.b.i Database created, migrations applied on start | ➖ | Exercised by every run: the E2E fixture drops `RentalPortalE2E` and the app's startup migrates it |
| TR 2.b.ii Idempotent seeding with Bogus | ➖ | Every run seeds a fresh database, and the tests log in as seeded users and use seeded unit types. That seeding covers applications in every status and leases is checked in the database and by reading `DbSeeder`, not by a test |
| TR 2.c Unit tests | ➖ | `tests/RentalPortal.Tests` |
| TR 3.a SQL Server, EF Core migrations | ➖ | Every E2E run uses LocalDB through EF Core |

## Functional Requirements

| Item | E2E | Tests |
|---|---|---|
| FR 1.a Sign up, log in, log out | ✅ | `AccountTests.Register_WithChosenAccountType_SignsInWithThatRole`, `Login_ThenLogout_Works` |
| FR 1.a.i Choose Applicant or Property Manager at sign-up | ✅ | `AccountTests.Register_WithChosenAccountType_SignsInWithThatRole` (both roles), `Register_WithoutAccountType_ShowsError` |
| FR 2.a Properties: add, edit, remove through modals | ✅ | `PropertiesTests.AddProperty_*`, `EditProperty_*`, `DeleteProperty_RemovesItAndItsUnits` |
| FR 2.a Removing a property removes its units | ✅ | `PropertiesTests.DeleteProperty_RemovesItAndItsUnits` |
| FR 2.a Units listed per property | ✅ | `UnitsTests.AddUnit_ThroughModal_AppearsInUnitList` |
| FR 2.a Units: add, edit, remove | ✅ | `UnitsTests.AddUnit_*`, `EditAndDeleteUnit_ThroughModal` |
| FR 2.a Delete refused with a message when there are applications | ✅ | `PropertiesTests.DeleteProperty_WithApplications_IsRefusedWithMessage`, `UnitsTests.DeleteUnit_WithApplications_IsRefusedWithMessage` |
| FR 2.a Applicants browse available units | ✅ | `ListingsTests.AvailableUnits_ListsUnitsFromNavbar` |
| FR 2.a Only units with no lease covering today | ✅ | `ListingsTests.LeasedUnit_IsNotListed` |
| FR 2.a Start an application from a unit (reopens an open one) | ✅ | `ListingsTests.ApplyForUnit_StartsApplication_AndReopensIt` |
| FR 2.b Unit type lookup, inactive shown on existing units | ✅ | `UnitsTests.InactiveUnitType_ShowsOnExistingUnit_ButCantBeChosen` |
| FR 2.b Inactive type can't be selected; enforced on the server | ✅ | `UnitsTests.InactiveUnitType_ShowsOnExistingUnit_ButCantBeChosen` (option injected by hand is refused) |
| FR 2.b Seed data includes an inactive type | ✅ | Same test uses the seeded `Penthouse` type |
| FR 2.c Approval creates a twelve-month lease starting today | ✅ | `ReviewTests.Approve_IssuesTwelveMonthLease_AndRecordsHistory` |
| FR 2.c A leased unit is not available | ✅ | `ListingsTests.LeasedUnit_IsNotListed`, `ListingsTests.ApplyForUnit_LeasedSinceThePageLoaded_IsRefused` |
| FR 3 Domain model and database | ➖ | Code and unit tests; exercised by every application test |
| FR 3.a Applicant Information, Residence History, Summary | ✅ | `ApplicationWizardTests.Applicant_CompletesBothSections_AndSubmits` |
| FR 3.b One section at a time; Continue validates and saves | ✅ | `ApplicationWizardTests.Applicant_CompletesBothSections_AndSubmits` |
| FR 3.b Continue re-renders with errors (field errors; alert with no residences) | ✅ | Same test |
| FR 3.b Back returns to the previous section | ✅ | Same test |
| FR 3.b Submit only from the Summary after both sections are saved | ✅ | Same test, plus `Summary_HasNoSubmit_UntilBothSectionsAreSaved` |
| FR 3.b One view model, one form, one action | ➖ | Code structure (`ApplicationFormViewModel`, `ApplicationsController.Details` POST) |
| FR 3.c Residences added, edited, removed through a modal | ✅ | `ResidencesTests.Residences_AddEditAndRemove_ThroughModal` |
| FR 3.d Same partial renders editable and read-only | ✅ | `ApplicationWizardTests.SubmittedApplication_IsReadOnly` |
| FR 3.d Editable only for the owner, only in Draft or Returned | ✅ | `RolesAndPermissionsTests.OtherApplicant_CannotViewOrEditAnApplication`, `SubmittedApplication_RejectsCraftedEdit`, `ReviewTests.Return_*` (editable again) |
| FR 3.d Controllers reject posts that aren't allowed | ✅ | `RolesAndPermissionsTests.OtherApplicant_*`, `SubmittedApplication_RejectsCraftedEdit` |
| FR 3.e Submit rejected when the unit has an active lease | ✅ | `ApplicationWizardTests.Submit_WhenUnitWasLeasedMeanwhile_IsRefused` |
| FR 3.e Approval rejected when the unit has an active lease | ✅ | `ReviewTests.SecondApproval_ForLeasedUnit_IsRefused_AndOtherApplicationUnchanged` |
| FR 3.e Other open applications left unchanged | ✅ | Same test |
| FR 4.a Review modal; Approve, Return, Deny | ✅ | `ReviewTests` (one test per outcome) |
| FR 4.a Comment required for Return and Deny | ✅ | `ReviewTests.Return_*`, `ReviewTests.Deny_*` |
| FR 4.b All six statuses; Approved, Denied, Withdrawn terminal | ✅ | `WithdrawTests` (Draft, Submitted, Returned → Withdrawn; Approved and Denied can't be withdrawn), `ReviewTests` |
| FR 4.b Return, Approve, Deny transitions | ✅ | `ReviewTests` |
| FR 4.c History with who, when, comment; managers only | ✅ | `ReviewTests.Approve_IssuesTwelveMonthLease_AndRecordsHistory` |
| FR 5.a Filter by status and property | ✅ | `ApplicationListTests.Manager_SeesAllApplications_AndFiltersByStatusAndProperty` |
| FR 5.a Filtering done in the database | ➖ | Code review (`Where` before `ToListAsync` in `ApplicationsController.Index`) |
| FR 5.a Applicants see only their own | ✅ | `ApplicationListTests.Applicant_SeesOnlyOwnApplications` |
| FR 5.a Managers see all | ✅ | `ApplicationListTests.Manager_SeesAllApplications_AndFiltersByStatusAndProperty` |

## Bonus and deliverables

Not implemented yet, so not covered. Add tests when a bonus item is built.
