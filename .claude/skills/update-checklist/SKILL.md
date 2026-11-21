---
name: update-checklist
description: Update docs/requirements-checklist.md (and the status in docs/diagrams.md) to match what the code actually implements. Use when the user asks to update the requirements checklist or progress, or after finishing a feature.
---

# Update the requirements checklist

Bring `docs/requirements-checklist.md` in line with the code, and keep `docs/diagrams.md` consistent
with it. The source of truth for *what is required* is `docs/requirements.md`; the source of truth for
*what is done* is the code. Never tick an item based on a plan, a conversation, or a TODO.

## 1. Survey what changed

- Read the current checklist first, so you know what's already ticked.
- Find what changed since it was last updated:
  - `git log --oneline` and `git status --short` (include untracked files)
  - `git log -1 --format=%cd -- docs/requirements-checklist.md` for when the checklist last changed
- Look at the relevant code, not just file names:
  - `src/RentalPortal.Web/Controllers` — actions, `[Authorize]` roles, server-side checks
  - `src/RentalPortal.Web/Domain` — rules and status transitions
  - `src/RentalPortal.Web/Data` — `AppDbContext` mappings, `Migrations`, `DbSeeder`
  - `src/RentalPortal.Web/Views` and `ViewComponents` — pages, partials, modals
  - `tests/RentalPortal.Tests` — what the tests cover

## 2. Tick only what the code does

- **Tick** an item only when the code implements it. If it only exists in the domain or database
  (no page or controller yet), tick a sub-item for that layer and leave the item itself unchecked.
- **Split partial items** into sub-items rather than ticking them early, for example:
  ```markdown
  - [ ] **Submit** is available from the Summary only after both sections are saved
    - [x] Rule in the domain (`CanSubmit`, `Submit`)
    - [ ] Summary page and button
  ```
- **A parent stays unchecked** until every sub-item under it is ticked.
- **Name the code** that satisfies an item in backticks (class, method, or file) so it can be found in
  the interview.
- **Keep IDs and wording** from `docs/requirements.md` (TR 1.b, FR 3.d, ...). Don't rename or
  renumber existing items, and don't add requirements the spec doesn't contain. Optional or demo-only
  items must say so, for example "(optional, for the demo)".
- **Never untick** something without checking the code; if a feature was removed or broken, untick it
  and say why in the report.

## 3. Recount

Count ticked and total checkboxes, required (everything above `## Bonus`) and bonus separately:

```bash
f=docs/requirements-checklist.md
req=$(sed -n '1,/^## Bonus/p' $f)
bonus=$(sed -n '/^## Bonus/,/^## Deliverables/p' $f)
echo "required: $(echo "$req" | grep -c '\- \[x\]') / $(echo "$req" | grep -c '\- \[[ x]\]')"
echo "bonus: $(echo "$bonus" | grep -c '\- \[x\]') / $(echo "$bonus" | grep -c '\- \[[ x]\]')"
```

## 4. Keep docs/diagrams.md in sync

`docs/diagrams.md` shows the same status with colors (`done`, `partial`, `todo` classes). Update it
whenever a status changes:

- the "Status as of" date
- section 1 node classes and its "What's left" table
- section 2 (roles), section 3 (`<<Done>>` / `<<In progress>>` / `<<Not started>>` stereotypes),
  section 4 (state classes and the transition table), and the "done" / "not started" labels on
  sections 5–7
- anything the two documents disagree on; fix the one that doesn't match the code

If any Mermaid block changed, check that it still parses before finishing.

## 5. Report

Reply with:

- the new count, for example "41 of 108 required items"
- what was ticked or split, grouped by requirement ID
- anything you noticed that the checklist can't capture: bugs, spec gaps, or items that look done
  in the UI but aren't enforced on the server
- any place where the checklist and diagrams disagreed, and which one you changed
