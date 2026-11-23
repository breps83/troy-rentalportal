# Working with Claude Code

This repository is set up for [Claude Code](https://docs.claude.com/en/docs/claude-code). Claude is
used to review work, help with plans and requirement tracking, and assist with UI and small pieces of
logic.

- **[CLAUDE.md](../CLAUDE.md)** at the root holds the project instructions Claude reads every session:
  the project layout, how to work in the repo, the code conventions, and the build and test commands.
- **`.claude/skills`** holds the project skills listed below. Each one is a `SKILL.md` file with
  step-by-step instructions for a task in this repo.

## Project docs Claude works from

- [requirements.md](requirements.md): the spec as provided. Claude reads it but never edits it.
- [requirements-checklist.md](requirements-checklist.md): progress against each requirement ID,
  kept in step with the code by `/update-checklist`.
- [diagrams.md](diagrams.md): Mermaid diagrams of the same status, plus roles, the domain model,
  the application status lifecycle, and the page and modal flows.
- [interview-prep.md](interview-prep.md): what to expect in the technical interview and the
  pre-interview checklist.
- [e2e-tests.md](e2e-tests.md): how the E2E tests work and how to write one. `/e2e-tests` follows it.
- [e2e-coverage.md](e2e-coverage.md): which E2E test verifies each checklist item, kept up to date
  by `/e2e-tests`.
- [ef-commands.md](ef-commands.md): EF Core CLI commands for migrations and the database, and
  pitfalls to avoid.

## Project skills

These are checked into `.claude/skills`, so anyone who clones the repository has them. Run one by
typing its name as a slash command, or just describe the task and Claude picks the matching skill.

| Skill | What it does |
|---|---|
| `/run-e2e-tests` | Runs the Playwright E2E tests with annotated videos and traces, opens the report, and summarizes the results by requirement. |
| `/e2e-tests` | Writes, updates, or extends the E2E tests in `tests/RentalPortal.E2E.Tests` when a feature or view changes, and keeps [e2e-coverage.md](e2e-coverage.md) in sync. |
| `/update-checklist` | Updates [requirements-checklist.md](requirements-checklist.md) and the status in [diagrams.md](diagrams.md) to match what the code actually implements. It ticks an item only when the code does it, never because of a plan or a TODO. |

## Built-in skills

These come with Claude Code and work in any repository. The ones below are used on this project; type
`/` in Claude Code to see the full list for your version.

| Skill | What it does |
|---|---|
| `/code-review` | Reviews the current changes, or a branch or pull request, for bugs and for broken `CLAUDE.md` rules. Pass `--fix` to apply the findings. |
| `/security-review` | Reviews the pending changes on the current branch for security issues. |
| `/init` | Creates or refreshes a `CLAUDE.md` from the codebase. |
