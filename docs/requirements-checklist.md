# Requirements Checklist

Progress tracker for [requirements.md](requirements.md). IDs match the sections there (for example
**FR 3.b**). Check items off as they are done and verified in the running app.

## Roles and permissions (Challenge)

- [ ] Only two roles: Applicant and Property Manager
  - [x] Roles defined and seeded
  - [ ] Permissions enforced in every controller
  - [ ] UI shows only the actions the current role can perform
- [ ] **Applicant** can:
  - [ ] Create, fill out, and submit an application for an available unit
  - [ ] Withdraw an application
  - [ ] Correct and resubmit a returned application
- [ ] **Property Manager** can:
  - [x] Maintain properties and units
  - [ ] Review submitted applications
  - [ ] Approve an application, issuing a twelve-month lease

## Technical Requirements

### TR 1. ASP.NET Core MVC and Razor

- [x] **1.a** Uses controllers, view models, Razor views, partial views, and view components
  - [x] Controllers, view models, Razor views
  - [x] Partial views
  - [x] View components (`UnitListViewComponent` on the property Details page)
- [ ] **1.b** Modals are populated from partial views returned by controller actions
  - [x] Shared modal and script (`_Layout.cshtml`, `wwwroot/js/site.js`)
  - [x] Validation failure re-renders the same partial in the modal
  - [x] Success closes the modal and refreshes the affected part of the page
  - [x] Properties: add, edit, remove
  - [x] Units: add, edit, remove
  - [ ] Residences: add, edit, remove
  - [ ] Review modal
- [ ] **1.c** Structure and styling follow best practices

### TR 2. Backend (.NET 10)

- [x] **2.a** ASP.NET Identity for user and role management
- [x] **2.b.i** Database created and migrations applied on start
- [ ] **2.b.ii** Idempotent seeding on start, using Bogus
  - [x] Roles
  - [x] Property managers and applicants
  - [x] Properties
  - [ ] Lookups (unit types, active and inactive)
    - [x] Active unit types (Studio, Apartment, Condo, Townhouse)
    - [ ] At least one **inactive** unit type, used by an existing unit
  - [x] Units (5–8 per property)
  - [ ] Applications in **every** status (Draft, Submitted, Returned, Approved, Denied, Withdrawn)
  - [ ] Leases for approved applications
- [ ] **2.c** Unit tests for business logic
  - [x] Unit type selection rule (`UnitTypeTests`: `UnitType.IsSelectableFor`)
  - [x] Application rules (`RentalApplicationTests`): create, sections, residences, submit, withdraw,
        read-only after submit
  - [ ] Return, Approve, Deny, and resubmit
  - [ ] Active lease checks at submit and approval, and `Lease` term and coverage

### TR 3. Database

- [x] **3.a** SQL Server with EF Core code-first migrations

## Functional Requirements

### FR 1. Users

- [x] **1.a** Sign up, log in, and log out
- [x] **1.a.i** Sign-up lets the user choose Applicant or Property Manager

### FR 2. Properties and Units

- [ ] **2.a** Property manager adds, edits, and removes properties and units through modals
  - [x] Properties
  - [x] Removing a property removes its units (cascade delete)
  - [x] Units listed per property (Details page)
  - [x] Units: add, edit, remove (unit number, bedrooms, monthly rent, unit type)
  - [x] Deleting a property or unit is refused with a message when it has rental applications
  - [ ] Applicants can browse available units
  - [ ] Applicants can start an application from a unit
- [ ] **2.b** Unit Type lookup with Active and Inactive values
  - [x] Unit type stored as a lookup table, not free text
  - [x] Inactive type still displays on units that already use it (marked "inactive" in the unit list)
  - [x] Inactive type can't be selected for any other unit
  - [x] Enforced on the server, not just hidden in the UI (`UnitsController.ValidateUnitAsync`)
  - [ ] Seed data includes an inactive type (see TR 2.b.ii)
- [ ] **2.c** Leases
  - [ ] Approval creates a lease with a start date and a twelve-month term
  - [ ] A unit whose lease term covers today is not available

### FR 3. Rental Application

- [x] Domain model and database: `RentalApplication`, `Residence`, `ApplicationStatusHistory`,
      `AddApplication` migration
- [ ] **3.a** Application is for one unit, with two sections and a summary
  - [ ] Applicant Information: name, phone, email, current address
  - [ ] Residence History: prior residences with address, landlord name and phone, move-in and
        move-out dates
  - [ ] Summary: read-only view of both sections, with Submit
- [ ] **3.b** Single page, one section shown at a time
  - [ ] One view model drives the page
  - [ ] Each section renders through its own partial view or view component
  - [ ] One form posts to one action; the clicked button decides what happens
  - [ ] **Continue** validates, saves only when valid, then shows the next section or the Summary
  - [ ] **Continue** re-renders the section with errors when invalid
  - [ ] **Back** returns to the previous section without saving
  - [ ] **Submit** is available from the Summary only after both sections are saved
    - [x] Rule in the domain (`CanSubmit`, `Submit`)
    - [ ] Summary page and button
- [ ] **3.c** Residences are added, edited, and removed through a modal
- [ ] **3.d** Editable or read-only is a server-side decision
  - [ ] Same section partial renders both modes
  - [ ] Editable only for the applicant, and only in Draft or Returned status
    - [x] Status rule in the domain (`IsEditable`; edit methods throw otherwise)
    - [ ] Ownership check (only the applicant who owns it)
  - [ ] Controllers reject posts that aren't allowed
- [ ] **3.e** Active lease check
  - [ ] Submit is rejected with an error when the unit has an active lease
  - [ ] Approval is rejected with an error when the unit has an active lease (prevents a second lease)
  - [ ] Other open applications for the unit are left unchanged

### FR 4. Review

- [ ] **4.a** Review modal for a submitted application
  - [ ] Outcomes: Approve, Return, Deny
  - [ ] Comment required for Return and Deny
- [ ] **4.b** Statuses: Draft, Submitted, Returned, Approved, Denied, Withdrawn
  - [x] All six statuses defined (`ApplicationStatus`)
  - [x] Approved, Denied, and Withdrawn are terminal (`IsTerminal`)
  - [x] Withdraw (Draft, Submitted, Returned to Withdrawn)
  - [ ] Return, Approve, Deny transitions
- [ ] **4.c** Property managers see a history of status changes and review outcomes (who, when,
      comment)
  - [x] Every status change is recorded with who, when, and comment (`StatusHistory`)
  - [ ] History shown on the application page, to property managers only

### FR 5. Application List

- [ ] **5.a** Application list
  - [ ] Filter by status and property
  - [ ] Filtering done in the database, not in memory
  - [ ] Applicants see only their own applications
  - [ ] Property managers see all applications

## Bonus (optional)

- [ ] **B1** Application list paging and sorting in the database
  - [ ] Reusable grid view component
  - [ ] JSON endpoint returning the page of rows and the filtered total
  - [ ] Endpoint documented with OpenAPI
- [ ] **B2** Review queue: claim (Under Review) and release
- [ ] **B3** Property manager notes, never rendered or returned to applicants
- [ ] **B4** Save sections with validation errors
  - [ ] Summary lists everything blocking submission
  - [ ] Submit stays blocked while any error remains
  - [ ] Rules defined once per section, errors returned to their fields
- [ ] **B5** Multiple applicants per application
  - [ ] Any applicant on it can view and edit; ownership checks apply to all
  - [ ] Saves to different sections don't interfere
  - [ ] A stale save to the same section is rejected with a reload message

## Deliverables

- [ ] `README.md` with any non-standard setup instructions (demo logins, connection string, etc.)
- [ ] Code pushed to a GitHub repository
- [ ] Link sent in reply to the assessment email

## Pre-interview

See the checklist in [interview-prep.md](interview-prep.md#pre-interview-checklist).
