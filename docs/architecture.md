# Architecture

How the app is put together: the layers, where the business rules live, and how a request moves
through them. For the domain model, the status lifecycle, and the page and modal flows as diagrams,
see [diagrams.md](diagrams.md).

## Overview

A single ASP.NET Core MVC project (.NET 10), `src/RentalPortal.Web`, rendered on the server with Razor
views, partial views, and view components. There is no single-page application framework. Bootstrap
and jQuery validation come from `wwwroot/lib`, and one script, `wwwroot/js/site.js`, handles the
modals.

Data is stored in SQL Server through EF Core (code-first). Accounts and roles use ASP.NET Core
Identity, stored in the same database.

## Project layout

| Folder | What it holds |
|---|---|
| `Domain/` | Entities and their business rules: `RentalApplication`, `Residence`, `Lease`, `Property`, `Unit`, `UnitType`, `ApplicationNote`, `ApplicationStatusHistory`. Also `RuleException`, `Result`, and `Roles` |
| `Enums/` | `ApplicationStatus`, `ApplicationSection`, `ApplicationReviewStatus`, `AccountType` |
| `Services/` | `RentalApplicationService`: starts, submits, withdraws, and reviews applications |
| `Controllers/` | One per area: `Properties`, `Units`, `Listings`, `Applications`, `Residences`, `Account`, `Home` |
| `ViewModels/` | Form and display models per area, with the validation attributes |
| `Views/` | Razor pages, and partial views (`_Name.cshtml`) for the modal forms |
| `ViewComponents/` | Sections loaded on their own and refreshed after a modal save: unit list, residence history, application status, status history, notes |
| `Extensions/` | Small helpers: current user and role from `ClaimsPrincipal`, view and edit checks on an application, today's date from `TimeProvider` |
| `Data/` | `AppDbContext` (EF configuration), `DbSeeder` (migrations and demo data), and `Migrations/` |

## Layers and responsibilities

**Domain.** The rules live on the entities, not in controllers. `RentalApplication` keeps its
properties `private set` and changes only through methods such as `UpdateApplicant`, `AddResidence`,
`Submit`, `Withdraw`, `ApproveApplicationForLease`, `Return`, and `Deny`. Each method checks its rule
and throws `RuleException` with a message for the user when it's broken; for example, submitting an
incomplete application or approving onto a unit that already has a lease for that period. Every status
change also adds an `ApplicationStatusHistory` row with who, when, and the comment. The domain doesn't
touch the database, so these rules are covered by plain unit tests.

**Service.** `RentalApplicationService` handles the application actions that need more than one
entity or a database lookup. It checks ownership and status, loads what the rule needs (for example,
the unit's leases before submit or approval), calls the domain method, catches `RuleException`, and
returns a `Result`. Refused actions are logged.

**Controllers.** Controllers load data, check permissions, call the domain or the service, save, and
pick the response. Simple edits (properties, units, residences) call the domain directly; the
application lifecycle goes through the service.

**Views.** View models carry only what a view needs, so entities are never bound directly from a form.
Validation attributes match the database limits (`StringLength` matches `HasMaxLength`).

## Request flow

A typical modal save, for example adding a unit to a property:

1. A button with `data-modal-url` is clicked. `site.js` GETs that URL, and the controller returns a
   partial view, which is shown in the shared modal.
2. The form is posted with `fetch`, including the antiforgery token.
3. The controller validates the view model and applies the change through the domain.
   - Invalid: it returns the same partial with the errors, and the modal re-renders in place.
   - Valid: it saves and returns `Json(new { success = true })`.
4. `site.js` closes the modal and reloads the section named by `data-refresh-target` from
   `data-refresh-url`, usually a view component action. Several sections can refresh at once.

A redirect (for example, an expired login) navigates the whole page, and errors such as 403 show a
message in the modal. The same flow is drawn in [diagrams.md](diagrams.md), section 6.

## Application lifecycle

An application moves through `Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, and
`Withdrawn`.

- **Editable** only in `Draft` or `Returned`, and only by its applicant.
- **Submit** needs both sections complete (applicant info and residence history) and no active lease
  on the unit.
- **Review** is open only while `Submitted`. Return and Deny need a comment. Approve checks the unit's
  leases again and creates a `Lease` that starts today and runs 12 months.
- **Terminal**: `Approved`, `Denied`, and `Withdrawn` can't change again.

See [diagrams.md](diagrams.md), section 4, for the state diagram.

## Security and permissions

- **Roles:** `PropertyManager` and `Applicant`, from Identity. Each controller has
  `[Authorize(Roles = ...)]`: properties and units for property managers, residences for applicants,
  applications for both.
- **Ownership and status** are checked on the server for every action, not only hidden in the UI. An
  applicant can see and change only their own applications, and only while they're editable.
  Property manager notes are never rendered or returned to applicants.
- **Antiforgery** validation is on for every POST (`AutoValidateAntiforgeryTokenAttribute`).
- **Security headers** on every response: a Content Security Policy that allows only the app's own
  scripts and styles (no inline script or `style=""`), plus `X-Content-Type-Options`,
  `X-Frame-Options`, and `Referrer-Policy`.

## Data access

- **Code-first EF Core.** Configuration is in `AppDbContext.OnModelCreating`: lengths, decimal
  precision for rent, unique indexes (unit number per property, unit type name), and indexes for
  common filters (application status, lease dates).
- **History is kept.** Applications, leases, and status history use `DeleteBehavior.Restrict`, so a
  property or unit with applications can't be deleted. The delete actions check first and explain why
  in the confirm modal instead of failing.
- **Filtering happens in SQL.** List queries filter with `Where` before `ToListAsync`. EF can't
  translate our own C# methods, so a domain rule used in a query is written inline and kept in sync;
  for example, the available units listing repeats the active lease check from `Lease.IsActive`.
- **Time** comes from an injected `TimeProvider`, so date rules can be tested with a fixed date.
- **Startup** applies migrations, and in Development seeds demo data when the database has no users
  (`DbSeeder`).

## Testing

| Project | Covers |
|---|---|
| `tests/RentalPortal.Tests` | Domain and service rules, with no database |
| `tests/RentalPortal.Integration.Tests` | The app over HTTP: permissions, inline EF queries that repeat a domain rule, restricted deletes, modal responses, antiforgery |
| `tests/RentalPortal.E2E.Tests` | Every screen and browser-verifiable requirement, in Chromium with Playwright |

See the [README](../README.md#testing) for how to run them.
