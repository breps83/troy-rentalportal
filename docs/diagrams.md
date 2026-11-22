# Diagrams

Mermaid diagrams for [requirements.md](requirements.md), with the current implementation status.
Requirement IDs (for example **FR 3.b**) match [requirements-checklist.md](requirements-checklist.md).

**Status as of 2026-11-22.** Colors used in the diagrams:

| Color | Meaning |
|---|---|
| 🟩 Green | **Done**: implemented in the app |
| 🟨 Amber | **In progress**: partly implemented (for example domain model only, no UI yet) |
| ⬜ Gray | **Not started** |

Bonus items are left out; see the Bonus section of the checklist.

---

## 1. Progress by requirement area

```mermaid
flowchart LR
    classDef done fill:#c8e6c9,stroke:#2e7d32,color:#1b3d1f
    classDef partial fill:#ffe0b2,stroke:#ef6c00,color:#4e2a00
    classDef todo fill:#eceff1,stroke:#78909c,color:#263238

    subgraph TR["Technical requirements"]
        TR1a["TR 1.a<br/>Controllers, view models,<br/>partials, view components"]:::done
        TR1b["TR 1.b<br/>Modals from partial views"]:::done
        TR2a["TR 2.a<br/>ASP.NET Identity"]:::done
        TR2b1["TR 2.b.i<br/>Migrate on start"]:::done
        TR2b2["TR 2.b.ii<br/>Idempotent Bogus seeding"]:::done
        TR2c["TR 2.c<br/>Unit tests"]:::done
        TR3["TR 3.a<br/>SQL Server, EF Core code-first"]:::done
    end

    subgraph FR["Functional requirements"]
        FR1["FR 1<br/>Sign up, log in, log out"]:::done
        FR2a["FR 2.a<br/>Properties and units"]:::done
        FR2b["FR 2.b<br/>Unit type lookup"]:::done
        FR2c["FR 2.c<br/>Leases"]:::done
        FR3["FR 3<br/>Rental application"]:::done
        FR4["FR 4<br/>Review"]:::done
        FR5["FR 5<br/>Application list"]:::done
    end

    subgraph DL["Deliverables"]
        README["README.md"]:::partial
        GH["GitHub repository"]:::todo
    end
```

| Area | Status | What's left |
|---|---|---|
| TR 1.b | Done | Modals for properties, units, residences, withdrawal, and review (`_ReviewForm`) |
| TR 2.b.ii | Done | Roles, users, unit types, properties, units, and applications in every status with leases for approvals (`DbSeeder.SeedApplicationsAsync`) |
| TR 2.c | Done | `UnitTypeTests`, `RentalApplicationTests`, `LeaseTests`, `RentalApplicationExtensionsTests`, `ApplicationSectionExtensionsTests`, `RentalApplicationServiceTests` (81 passing) |
| FR 2.a | Done | Available Units lists only units with no lease covering today (`ListingsController.Index`) |
| FR 2.b | Done | `Penthouse` is seeded as the inactive type, on some 3-bedroom units |
| FR 2.c | Done | Approval creates a twelve-month `Lease` starting on the approval date |
| FR 3 | Done | 3.e checks for an active lease at Submit (`RentalApplication.Submit`) and at approval |
| FR 4 | Done | Review modal (Approve, Return, Deny; comment required for Return and Deny) and status history for property managers |
| FR 5 | Done | `ApplicationsController.Index` filters by status and property in the database; applicants see only their own |
| README.md | In progress | Testing section written; setup instructions (demo logins, connection string) remain |

---

## 2. Roles and permissions (Challenge)

Both roles must be enforced in the controllers **and** reflected in the UI.

```mermaid
flowchart LR
    classDef done fill:#c8e6c9,stroke:#2e7d32,color:#1b3d1f
    classDef partial fill:#ffe0b2,stroke:#ef6c00,color:#4e2a00
    classDef todo fill:#eceff1,stroke:#78909c,color:#263238
    classDef actor fill:#bbdefb,stroke:#1565c0,color:#0d2a4a

    Anyone(["Anyone"]):::actor
    A(["Applicant"]):::actor
    PM(["Property Manager"]):::actor

    Anyone --> SignUp["Sign up, choosing a role"]:::done
    Anyone --> LogIn["Log in and log out"]:::done

    A --> Browse["Browse available units"]:::done
    A --> Start["Start an application"]:::done
    A --> Fill["Fill out sections"]:::done
    A --> Submit["Submit"]:::done
    A --> Withdraw["Withdraw"]:::done
    A --> Resubmit["Correct and resubmit<br/>a returned application"]:::done
    A --> OwnList["See own applications"]:::done

    PM --> Props["Add, edit, remove properties"]:::done
    PM --> Units["Add, edit, remove units"]:::done
    PM --> Review["Review: Approve, Return, Deny"]:::done
    PM --> History["See status history"]:::done
    PM --> AllList["See all applications"]:::done
```

`PropertiesController` and `UnitsController` are restricted to `Roles.PropertyManager`,
`ResidencesController` to `Roles.Applicant`, and `ApplicationsController` checks ownership and
status on every save and on Withdraw (`Roles.Applicant` plus an owner check); `Review` is
`Roles.PropertyManager` only and accepts only Submitted applications. Its list action
filters to the applicant's own applications on the server. The navbar shows Properties only to
property managers, and Available Units and the application list to anyone logged in.

---

## 3. Domain model

Each entity's status is shown as its stereotype.

```mermaid
classDiagram
    direction LR

    class ApplicationUser {
        <<Done>>
        +string Id
        +string FirstName
        +string LastName
        +string Email
    }

    class Property {
        <<Done>>
        +int Id
        +string Name
        +string Street
        +string City
        +string State
        +string PostalCode
    }

    class Unit {
        <<Done>>
        +int Id
        +string UnitNumber
        +int Bedrooms
        +decimal MonthlyRent
    }

    class UnitType {
        <<Done>>
        +int Id
        +string Name
        +bool IsActive
        +IsSelectableFor(currentUnitTypeId) bool
    }

    class RentalApplication {
        <<Done>>
        +int Id
        +ApplicationStatus Status
        +string FirstName
        +string LastName
        +string Phone
        +string Email
        +string CurrentAddress
        +bool ApplicantInfoCompleted
        +bool ResidenceHistoryCompleted
        +UpdateApplicant()
        +AddResidence()
        +UpdateResidence()
        +RemoveResidence()
        +SetResidenceHistoryCompleted()
        +Submit()
        +Withdraw()
        +ReturnLease()
        +ApproveApplicationForLease() Lease
        +DenyLease()
    }

    class Residence {
        <<Done>>
        +int Id
        +string Address
        +string LandlordName
        +string LandlordPhone
        +DateOnly MoveInDate
        +DateOnly MoveOutDate
    }

    class ApplicationStatusHistory {
        <<Done>>
        +ApplicationStatus FromStatus
        +ApplicationStatus ToStatus
        +string ChangedById
        +DateTime ChangedAtUtc
        +string Comment
    }

    class Lease {
        <<Done>>
        +int Id
        +DateOnly StartDate
        +DateOnly EndDate
        +CreateFromApplication(application, startDate) Lease
        +IsActive(date) bool
        +Overlaps(startDate, endDate) bool
    }

    Property "1" --> "*" Unit : cascade delete
    UnitType "1" --> "*" Unit : restrict
    Unit "1" --> "*" RentalApplication : restrict
    ApplicationUser "1" --> "*" RentalApplication : applicant
    RentalApplication "1" --> "*" Residence : cascade delete
    RentalApplication "1" --> "*" ApplicationStatusHistory : status history
    RentalApplication "1" --> "0..1" Lease : approval creates, restrict
    Unit "1" --> "*" Lease : restrict
```

Delete rules:
- Removing a **property** removes its units. Because applications use `Restrict`, the property and
  unit delete actions refuse when any application exists, with a message in the confirm modal.
- Removing an **application** isn't a feature; its residences and history would cascade with it.

---

## 4. Application status lifecycle (FR 4.b)

All states can be reached with today's `RentalApplication`. Approved, Denied, and Withdrawn are
terminal.

```mermaid
stateDiagram-v2
    classDef done fill:#c8e6c9,stroke:#2e7d32,color:#1b3d1f
    classDef todo fill:#eceff1,stroke:#78909c,color:#263238

    [*] --> Draft : Start application
    Draft --> Submitted : Submit (both sections saved, no active lease)
    Draft --> Withdrawn : Withdraw
    Submitted --> Withdrawn : Withdraw
    Submitted --> Returned : Return (comment required)
    Submitted --> Denied : Deny (comment required)
    Submitted --> Approved : Approve (no conflicting lease)
    Returned --> Submitted : Resubmit
    Returned --> Withdrawn : Withdraw
    Approved --> [*]
    Denied --> [*]
    Withdrawn --> [*]

    class Draft, Submitted, Withdrawn, Returned, Approved, Denied done
```

| Transition | Status |
|---|---|
| Start → Draft | Done (`NewApplication`) |
| Draft → Submitted | Done (`Submit`; rejects an active lease, FR 3.e) |
| Draft / Submitted / Returned → Withdrawn | Done (`Withdraw`) |
| Submitted → Returned, Denied | Done (`ReturnLease`, `DenyLease`; comment required) |
| Submitted → Approved | Done (`ApproveApplicationForLease`; rejects an active or overlapping lease) |
| Returned → Submitted | Done (`Submit`; same active lease check as Draft → Submitted) |

Every transition is recorded in `StatusHistory` with who, when, and an optional comment (FR 4.c).

---

## 5. Application page flow (FR 3.b)

One page, one view model, one form posting to one action. The button clicked decides what happens.
**Done** (`ApplicationsController.Details`, `Details.cshtml`, and `RentalApplication.Submit` for the
active lease check, FR 3.e).

```mermaid
flowchart TD
    classDef done fill:#c8e6c9,stroke:#2e7d32,color:#1b3d1f
    classDef partial fill:#ffe0b2,stroke:#ef6c00,color:#4e2a00
    classDef decision fill:#fff8e1,stroke:#f9a825,color:#3e2f00

    Open(["Open application"]):::done --> Editable{"Editable?<br/>Draft or Returned,<br/>decided on the server"}:::decision
    Editable -- No --> ReadOnly["All sections read-only"]:::done
    Editable -- Yes --> S1["Section 1<br/>Applicant Information"]:::done

    S1 -- Continue --> V1{"Valid?"}:::decision
    V1 -- No --> S1
    V1 -- Yes, save --> S2["Section 2<br/>Residence History<br/>(residences via modal)"]:::done

    S2 -- Back, no save --> S1
    S2 -- Continue --> V2{"At least one residence?"}:::decision
    V2 -- No --> S2
    V2 -- Yes, save --> Sum["Summary<br/>read-only view of both sections"]:::done

    Sum -- Back --> S2
    Sum -- Submit --> V3{"Both sections saved<br/>and no active lease?"}:::decision
    V3 -- No --> Sum
    V3 -- Yes --> Done(["Submitted"]):::done
```

The controller rejects posts that aren't allowed (FR 3.d), not just hides the buttons
(`RejectEditActionResult`).

---

## 6. Modal request flow (TR 1.b) — done

How every modal in the app works: properties, units, residences, withdrawal, and review. The withdraw confirmation returns JSON with `redirectUrl`, so the page navigates to the
application list instead of refreshing a target.
Implemented in `wwwroot/js/site.js` and the `Create`/`Edit`/`Delete` actions.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Page as Page and site.js
    participant Ctrl as Controller
    participant DB as Database

    User->>Page: Click a button with data-modal-url
    Page->>Ctrl: GET form (fetch)
    Ctrl-->>Page: Partial view
    Page->>User: Show modal

    User->>Page: Save
    Page->>Ctrl: POST form with antiforgery token
    alt Validation fails
        Ctrl-->>Page: Same partial with errors
        Page->>User: Re-render in place
    else Valid
        Ctrl->>DB: SaveChangesAsync
        Ctrl-->>Page: JSON success
        Page->>User: Close modal
        Page->>Ctrl: GET data-refresh-url
        Ctrl-->>Page: List partial or view component
        Page->>User: Replace data-refresh-target
    else Redirect, such as an expired login
        Page->>User: Navigate to the redirect URL
    else Error status
        Page->>User: Error message in the modal
    end
```

---

## 7. Review and approval (FR 4.a, FR 2.c, FR 3.e) — done

`ApplicationsController.Review` and `RentalApplicationService.ReviewApplicationAsync`. The lease
starts on the approval date. There is no transaction around the lease check, so two managers
approving at the same moment could still both pass it.

```mermaid
sequenceDiagram
    autonumber
    actor PM as Property Manager
    participant Ctrl as Review action and service
    participant App as RentalApplication
    participant DB as Database

    PM->>Ctrl: Open review modal for a Submitted application
    Ctrl-->>PM: Review form: decision and comment

    PM->>Ctrl: POST decision
    alt Return or Deny
        Ctrl->>App: ReturnLease or DenyLease with comment
        Note right of App: Comment required
        Ctrl->>DB: Save status and history
    else Approve
        Ctrl->>DB: Load the unit's leases
        DB-->>Ctrl: Leases
        Ctrl->>App: ApproveApplicationForLease(today, unitLeases)
        alt Active or overlapping lease
            App-->>Ctrl: RuleException
            Ctrl-->>PM: Error in the modal, nothing saved
        else No conflict
            App-->>Ctrl: New lease, 12-month term
            Ctrl->>DB: Save status, history, and lease
            Ctrl-->>PM: JSON success, modal closes, status panel refreshes
        end
    end
```

Other open applications for the same unit are left as they are (FR 3.e); once the lease is active,
Submit rejects them (`Submit`).
