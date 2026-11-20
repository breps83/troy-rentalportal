# Requirements

Technical assessment specification for the Rental Portal. The wording is kept from the original
spec. Only the formatting and numbering are cleaned up, so items can be referenced as, for example,
**FR 3.b** or **TR 1.b**.

## Overview

- This technical assessment is to demonstrate proficiency in ASP.NET Core MVC and Razor to create a
  full-stack web application.
- The ideal applicant should possess a good understanding of ASP.NET Core MVC, Entity Framework Core,
  database design, and server-rendered front-end development with Razor views, partial views, and
  view components. Use any UI or JavaScript libraries desired; **do not use a single-page application
  framework**.
- Complete everything in the requirements sections. Items in the [Bonus](#bonus-optional) section are
  optional, and the evaluation is not negatively affected by skipping them.
- Completing this assessment is the first step, not the technical interview. If the submission moves
  forward, the solution is presented, debugged, and walked through live. See
  [interview-prep.md](interview-prep.md).

## Challenge

Create a full-stack web application for a property management company that accepts rental
applications for its apartments. A property manager maintains properties and their units. An
applicant applies for a unit and, if approved, is issued a lease.

There are only two roles in this system: **Applicant** and **Property Manager**. There are certain
actions each role can perform; ensure the permissions in the **controllers and the UI** reflect this.

- **a. Applicant** can create, fill out, submit, and withdraw applications for available units, and
  correct and resubmit an application that was returned.
- **b. Property Manager** maintains properties and units and reviews submitted applications. An
  approved application issues a twelve-month lease for the unit.

## Technical Requirements (TR)

### TR 1. ASP.NET Core MVC and Razor

- **a.** Use controllers, view models, Razor views, partial views, and view components. **Both
  partial views and view components are required.**
- **b.** Populate modals from partial views returned by controller actions. When the submitted form
  fails validation, return the same partial with the validation messages so the modal re-renders in
  place; when it succeeds, close the modal and refresh the affected part of the page.
- **c.** Structure and style the web application to your liking using best practices.

### TR 2. Backend (.NET 10)

- **a.** Use ASP.NET Identity for user/role management.
- **b.** On start:
  - **i.** The database is created and migrations are applied.
  - **ii.** The database is seeded **idempotently** with lookups, property managers, applicants,
    properties, units, and applications **in every status**.
    - Seed the data using **Bogus** for .NET.
- **c.** Add unit tests for business logic.

### TR 3. Database

- **a.** SQL Server / SQL Server Express, using Entity Framework Core with code-first migrations.

## Functional Requirements (FR)

### FR 1. Users

- **a.** Users can sign up, log in, and log out.
  - **i.** For convenience, sign-up lets the user specify whether they are an Applicant or a
    Property Manager.

### FR 2. Properties and Units

- **a.** A property manager can add, edit, and remove properties and their units (unit number,
  bedrooms, monthly rent, unit type) **through modals**. Applicants can browse available units and
  start an application for one.
- **b.** Unit Type is a lookup with Active and Inactive values. An inactive value still displays on a
  unit that already uses it but cannot be selected for any other unit. **Enforce this on the
  server.**
- **c.** Approval creates a lease for the unit with a start date and a twelve-month term. A unit whose
  lease term covers today is not available.

### FR 3. Rental Application

- **a.** An application is for one unit and has two sections and a summary:
  - **i. Applicant Information:** name, phone, email, current address.
  - **ii. Residence History:** a list of prior residences, each with an address, landlord name and
    phone, and move-in and move-out dates.
  - **iii. Summary:** read-only view of both sections, and Submit.
- **b.** The application is a **single page** that shows one section at a time, not a separate page
  per section. **One view model** drives it, and each section renders through its own partial view or
  view component. **One form posts to one action**, and the button clicked determines what happens:
  - **i. Continue** validates the current section, persists it only when it is valid, and shows the
    next section (or the Summary after the last one). When the section is invalid it re-renders with
    the errors.
  - **ii. Back** returns to the previous section without saving.
  - **iii. Submit** is available from the Summary only once both sections have been saved.
- **c.** Residences are added, edited, and removed through a modal.
- **d.** The same section partial renders editable or read-only based on a **server-side decision**.
  An applicant can edit while the application is in **Draft** or **Returned** status; otherwise every
  section is read-only. **Controllers reject posts that are not allowed.**
- **e.** At submit, and again at approval, reject the action with an error when the unit has an active
  lease. Other open applications for that unit are left as they are. The approval check prevents a
  second lease.

### FR 4. Review

- **a.** A property manager opens a submitted application and completes it through a **review modal**
  with an outcome of **Approve**, **Return**, or **Deny** and a comment (**required for Return and
  Deny**).
- **b.** Application statuses are **Draft, Submitted, Returned, Approved, Denied, Withdrawn**.
  Approved, Denied, and Withdrawn are **terminal**.
- **c.** The application page shows property managers a history of status changes and review outcomes
  (who, when, comment).

### FR 5. Application List

- **a.** A list of applications filtered by status and property, with the **filtering done in the
  database**, not in memory. Applicants see their own applications; property managers see all of
  them.

## Bonus (Optional)

1. Add paging and sorting to the application list, done in the database, then extract the list into a
   reusable **grid view component** driven by a **JSON endpoint** that returns the page of rows and
   the filtered total, documented with **OpenAPI**.
2. Add a **review queue**. A property manager claims a submitted application (**Under Review**) before
   completing it, and can release it back to the queue.
3. Add **property manager notes** that are visible and editable only to property managers and are
   never rendered or returned to an applicant.
4. Allow a section to be **saved even when it fails validation**, showing the errors. The Summary lists
   everything still blocking submission, and Submit stays blocked while any error remains. Define
   rules once per section and return errors to the field they belong to.
5. Allow **more than one applicant** on an application. Any applicant on it can view and edit it.
   Ownership checks apply to all of them.
   - **a.** With two applicants editing at once, saves to different sections must not interfere with
     each other. When both save the same section, the second save is rejected as stale with a message
     to reload rather than silently overwriting. No real-time synchronisation is expected.

## Considerations

- Elements not specifically detailed are left to your discretion; use sound judgment.
- Approach this like any other development task: code should be clean, robust, and ready for a
  production environment.
- Follow the standards, conventions, and best practices of the frameworks used.
- Integrate thoughtful design principles, and be ready to explain the rationale behind design
  decisions and the specifics of the implementation.
- The exercise is not timed and fast turnaround is not rewarded, but it must be returned within
  **7 days**. If more time is needed, communicate a requested delivery date; missing it may result in
  disqualification.

## Deliverables

- Reply to the assessment email with a link to the GitHub repository.
- Include a **README.md** with any setup or installation instructions that are not standard for the
  technology used.
