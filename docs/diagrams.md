# Diagrams

Mermaid diagrams for [requirements.md](requirements.md), with the current implementation status.
Requirement IDs (for example **FR 3.b**) match [requirements-checklist.md](requirements-checklist.md).

**Status as of 2026-11-21.** Colors used in the diagrams:

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
        TR1b["TR 1.b<br/>Modals from partial views"]:::partial
        TR2a["TR 2.a<br/>ASP.NET Identity"]:::done
        TR2b1["TR 2.b.i<br/>Migrate on start"]:::done
        TR2b2["TR 2.b.ii<br/>Idempotent Bogus seeding"]:::partial
        TR2c["TR 2.c<br/>Unit tests"]:::partial
        TR3["TR 3.a<br/>SQL Server, EF Core code-first"]:::done
    end

    subgraph FR["Functional requirements"]
        FR1["FR 1<br/>Sign up, log in, log out"]:::done
        FR2a["FR 2.a<br/>Properties and units"]:::partial
        FR2b["FR 2.b<br/>Unit type lookup"]:::done
        FR2c["FR 2.c<br/>Leases"]:::todo
        FR3["FR 3<br/>Rental application"]:::partial
        FR4["FR 4<br/>Review"]:::todo
        FR5["FR 5<br/>Application list"]:::todo
    end

    subgraph DL["Deliverables"]
        README["README.md"]:::todo
        GH["GitHub repository"]:::todo
    end
```

| Area | Status | What's left |
|---|---|---|
| TR 1.b | In progress | Modals done for properties, units, and residences; the review modal remains |
| TR 2.b.ii | In progress | Roles, users, unit types, properties, units seeded; applications in every status and leases remain |
| TR 2.c | In progress | `UnitTypeTests`, `RentalApplicationTests`, `RentalApplicationExtensionsTests`, `ApplicationSectionExtensionsTests`, `RentalApplicationServiceTests` (44 passing); review, lease, and submit-with-lease tests remain |
| FR 2.a | In progress | Manager side done and applicants can start an application; Available Units lists every unit until leases exist |
| FR 2.b | Done | `Penthouse` is seeded as the inactive type, on some 3-bedroom units |
| FR 3 | In progress | 3.a–3.d done (single-page form, residence modals, Submit, server-side edit checks); the active lease check (3.e) remains |

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

    A --> Browse["Browse available units"]:::partial
    A --> Start["Start an application"]:::done
    A --> Fill["Fill out sections"]:::done
    A --> Submit["Submit"]:::partial
    A --> Withdraw["Withdraw"]:::partial
    A --> Resubmit["Correct and resubmit<br/>a returned application"]:::todo
    A --> OwnList["See own applications"]:::todo

    PM --> Props["Add, edit, remove properties"]:::done
    PM --> Units["Add, edit, remove units"]:::done
    PM --> Review["Review: Approve, Return, Deny"]:::todo
    PM --> History["See status history"]:::todo
    PM --> AllList["See all applications"]:::todo
```

"Withdraw" is amber because the rule exists in `RentalApplication` and is unit tested, but there's
no controller action or button for it yet. "Submit" is amber because it works from the Summary but
doesn't check for an active lease yet (FR 3.e). "Browse available units" is amber because the page
exists (`ListingsController`) but can't filter out leased units until `Lease` exists.
`PropertiesController` and `UnitsController` are restricted to `Roles.PropertyManager`,
`ResidencesController` to `Roles.Applicant`, and `ApplicationsController` checks ownership and
status on every save; the navbar shows Properties only to property managers, and Available Units to
anyone logged in.

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
        <<In progress>>
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
        +Return() planned
        +Approve() planned
        +Deny() planned
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
        <<In progress>>
        +ApplicationStatus FromStatus
        +ApplicationStatus ToStatus
        +string ChangedById
        +DateTime ChangedAtUtc
        +string Comment
    }

    class Lease {
        <<Not started>>
        +int Id
        +DateOnly StartDate
        +DateOnly EndDate
        +Covers(date) bool
    }

    Property "1" --> "*" Unit : cascade delete
    UnitType "1" --> "*" Unit : restrict
    Unit "1" --> "*" RentalApplication : restrict
    ApplicationUser "1" --> "*" RentalApplication : applicant
    RentalApplication "1" --> "*" Residence : cascade delete
    RentalApplication "1" --> "*" ApplicationStatusHistory : status history
    RentalApplication "1" --> "0..1" Lease : approval creates
    Unit "1" --> "*" Lease : planned
```

Delete rules:
- Removing a **property** removes its units. Because applications use `Restrict`, the property and
  unit delete actions refuse when any application exists, with a message in the confirm modal.
- Removing an **application** isn't a feature; its residences and history would cascade with it.

---

## 4. Application status lifecycle (FR 4.b)

Green states can be reached with today's `RentalApplication`; gray states need Return, Approve, or
Deny. Approved, Denied, and Withdrawn are terminal.

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

    class Draft, Submitted, Withdrawn done
    class Returned, Approved, Denied todo
```

| Transition | Status |
|---|---|
| Start → Draft | Done (`NewApplication`) |
| Draft → Submitted | Done, except the active lease check (FR 3.e) |
| Draft / Submitted / Returned → Withdrawn | Done (`Withdraw`) |
| Submitted → Returned, Denied, Approved | Not started |
| Returned → Submitted | Supported by `Submit` already; needs `Return` to reach Returned |

Every transition is recorded in `StatusHistory` with who, when, and an optional comment (FR 4.c).

---

## 5. Application page flow (FR 3.b)

One page, one view model, one form posting to one action. The button clicked decides what happens.
**Done** (`ApplicationsController.Details`, `Details.cshtml`), except the active lease check at
Submit (FR 3.e), shown in amber.

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
    Sum -- Submit --> V3{"Both sections saved<br/>and no active lease?"}:::partial
    V3 -- No --> Sum
    V3 -- Yes --> Done(["Submitted"]):::done
```

The controller rejects posts that aren't allowed (FR 3.d), not just hides the buttons
(`RejectEditActionResult`).

---

## 6. Modal request flow (TR 1.b) — done

How every modal in the app works: properties, units, and residences today, review later.
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

## 7. Review and approval (FR 4.a, FR 2.c, FR 3.e) — not started

The planned flow for approving an application. The serializable transaction makes sure two managers
approving at the same moment can't create two leases for the same unit.

```mermaid
sequenceDiagram
    autonumber
    actor PM as Property Manager
    participant Ctrl as Review action
    participant App as RentalApplication
    participant DB as Database

    PM->>Ctrl: Open review modal for a Submitted application
    Ctrl-->>PM: Review form: outcome, comment, lease start

    PM->>Ctrl: POST outcome
    alt Return or Deny
        Ctrl->>App: Return or Deny with comment
        Note right of App: Comment required
        Ctrl->>DB: Save status and history
    else Approve
        Ctrl->>DB: Begin serializable transaction
        Ctrl->>DB: Any lease overlapping the new term?
        DB-->>Ctrl: Yes or no
        Ctrl->>App: Approve(start, hasConflictingLease)
        alt Conflict
            App-->>Ctrl: RuleException
            Ctrl-->>PM: Error in the modal, nothing saved
        else No conflict
            App-->>Ctrl: New lease, 12-month term
            Ctrl->>DB: Save status, history, and lease, then commit
            Ctrl-->>PM: JSON success, modal closes
        end
    end
```

Other open applications for the same unit are left as they are (FR 3.e). They can still be
submitted only while the unit has no active lease.
