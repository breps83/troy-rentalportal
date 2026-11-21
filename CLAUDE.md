# CLAUDE.md

Guidance for Claude Code when working in this repository. Claude is used to review work, help with
plans and requirement tracking, and assist with UI and small pieces of logic.

## Project

ASP.NET Core MVC (.NET 10) rental portal: property managers maintain properties and units and
review rental applications; applicants apply for available units. Server-rendered Razor views,
partial views, and view components, with no single-page application framework.

- Spec: `docs/requirements.md` (requirement IDs such as FR 3.b and TR 1.b)
- Progress: `docs/requirements-checklist.md`, with status diagrams in `docs/diagrams.md`
- App: `src/RentalPortal.Web`. Tests: `tests/RentalPortal.Tests` (xUnit)
- Database: SQL Server LocalDB (`RentalPortalDb`), EF Core code-first. The app applies
  migrations and reseeds on startup (`Data/DbSeeder.cs`), so the dev database is disposable.

## How to work in this repo

- Explain what you're about to change and why. Keep changes small and focused on what was asked.
- Ask before design changes, new dependencies, or changes across many files.
- Don't rewrite code I'm in the middle of; point out problems and let me decide.
- Before any destructive command (dropping a database, deleting files, resetting git), state
  exactly what it will affect and wait for confirmation.
- Verify claims by building, running tests, or reading the code. Say plainly what wasn't verified.
- Only tick checklist items the code actually implements (see `.claude/skills/update-checklist`).

## Conventions

- **Language**: American English in code, comments, docs, UI text, and seed data (US spelling and
  US housing terms).
- **Naming** is enforced in the build via `.editorconfig`: PascalCase for types, methods,
  properties, and constants; `_camelCase` for private fields; camelCase for parameters and locals.
  Keep lines within 120 characters.
- **Business rules live in the domain** (for example `RentalApplication`, `UnitType`), throw
  `RuleException` when broken, and get unit tests. Controllers load data, call the domain, and save.
- **Modals** (`wwwroot/js/site.js`): GET returns a partial view; POST returns the same partial with
  validation errors, or `Json(new { success = true })` on success. Triggers use `data-modal-url`,
  `data-refresh-url`, and `data-refresh-target`.
- **Permissions** are enforced in controllers (`[Authorize(Roles = ...)]` plus ownership and status
  checks), not just hidden in the UI.
- **Validation** limits must match the database: `StringLength` matches `HasMaxLength`, and decimal
  ranges use `Range(typeof(decimal), "min", "max")`, never `Range(int, int)`.
- **EF Core queries** can't call our own C# methods (they can't be translated to SQL); write the
  condition inline and keep it in sync with the domain method.
- **Deletes**: records with history (applications, leases) use `DeleteBehavior.Restrict`; delete
  actions check first and show a message in the confirm modal instead of failing.

## Commands

```bash
dotnet build RentalPortal.slnx
dotnet test RentalPortal.slnx
dotnet ef migrations add <Name> --project src/RentalPortal.Web
dotnet ef migrations has-pending-model-changes --project src/RentalPortal.Web
```

When the app is running, it locks `bin/`; to check that the code compiles, build to another
folder instead: `dotnet build src/RentalPortal.Web -o <temp folder>`.
