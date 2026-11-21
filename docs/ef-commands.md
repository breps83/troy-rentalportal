# EF Core Commands

Common Entity Framework Core CLI commands for this solution. Run them from the repository root; each
command points at the web project with `--project src/RentalPortal.Web`.

You usually don't need to apply migrations by hand: on startup the app runs `MigrateAsync` and then
reseeds (`Data/DbSeeder.cs`). The dev database (`RentalPortalDb` on `(localdb)\mssqllocaldb`) can be
dropped and recreated at any time.

## Setup

```bash
# Install the EF Core CLI (once per machine)
dotnet tool install --global dotnet-ef

# Update it
dotnet tool update --global dotnet-ef

# Check the version
dotnet ef --version
```

## Check before changing anything

```bash
# Which database the commands will use (provider, database name, server)
dotnet ef dbcontext info --project src/RentalPortal.Web

# List migrations; ones not yet applied to the database show "(Pending)"
dotnet ef migrations list --project src/RentalPortal.Web

# Does the model have changes that no migration covers yet?
dotnet ef migrations has-pending-model-changes --project src/RentalPortal.Web
```

## Migrations

```bash
# Add a migration for the current model changes (PascalCase name describing the change)
dotnet ef migrations add AddLeases --project src/RentalPortal.Web

# Remove the most recent migration (only if it hasn't been applied to a database yet)
dotnet ef migrations remove --project src/RentalPortal.Web

# Generate the SQL for all migrations, without touching a database
dotnet ef migrations script --project src/RentalPortal.Web --output migrations.sql

# SQL for one range of migrations (from, to)
dotnet ef migrations script AddUnits AddApplication --project src/RentalPortal.Web

# Idempotent script: safe to run against a database at any migration
dotnet ef migrations script --idempotent --project src/RentalPortal.Web --output migrations.sql
```

**Notes:**
- Review the generated migration before applying it: column types, lengths, nullability, and
  `onDelete` behavior.
- `migrations remove` on a migration that's already applied fails. Roll the database back first
  (see below), or drop the database.
- New migrations live in `src/RentalPortal.Web/Data/Migrations`. `.editorconfig` marks that folder as
  generated code, so style rules don't apply to it.

## Database

```bash
# Apply all pending migrations (the app also does this on startup)
dotnet ef database update --project src/RentalPortal.Web

# Roll back (or forward) to a specific migration
dotnet ef database update AddUnits --project src/RentalPortal.Web

# Roll back every migration (empties the schema, keeps the database)
dotnet ef database update 0 --project src/RentalPortal.Web

# Drop the database (asks for confirmation; add --force to skip it)
dotnet ef database drop --project src/RentalPortal.Web
```

**Before dropping:** run `dotnet ef dbcontext info` and confirm the database name. Don't rely on
`--connection` to point `drop` at a different database; in this project it has dropped
`RentalPortalDb` even with `--connection` set to another database.

### Reset the dev database

When seeding changes (new seed data only appears in an empty database):

```bash
dotnet ef database drop --project src/RentalPortal.Web
```

Then start the app. It recreates the database, applies every migration, and reseeds.

## Pitfalls

- **The app is running:** commands that build fail because the running app locks `bin/`. Stop the
  app first, or use `--no-build` only when the last build is current.
- **`--no-build` right after `migrations add`:** `migrations add` builds *before* it writes the new
  migration, so that build doesn't include it. A following `database update --no-build` reports
  "pending model changes". Run without `--no-build`.
- **"Pending model changes" at startup:** the model changed without a migration. Run
  `migrations has-pending-model-changes`, then add a migration.
- **Tool and runtime versions:** keep the `dotnet-ef` tool close to the EF Core package version in
  `RentalPortal.Web.csproj` (currently 10.0.x). A newer tool usually works but can warn or behave
  differently.
