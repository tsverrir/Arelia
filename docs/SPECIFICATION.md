# Arelia — Multi-tenant Choir/Small Organization Management

_(Specifications — Obsidian Markdown)_

## 1. Overview

**Arelia** is a **multi-tenant** web application for managing choirs (or similar small organizations). Each tenant (Organization) manages:

- People and their role history
- Semesters, rehearsals, concerts, trips, and other activities
- Participation expectations, RSVP (Yes/No/Maybe), and attendance tracking
- Finance: membership fees, event fees, partial payments, overpayment credit, discounts, optional top-ups, income and expenses
- Reporting grouped by **semester**, **year**, or **activity**

The system is built with **Blazor Server**, **MudBlazor**, **EF Core**, and **SQLite**, hosted in a **Docker container**.

---

## 2. Tenancy Model (Multi-tenant)

### 2.1 Tenant definition

- A **Tenant** is an **Organization**.
- Every entity is scoped to exactly one `OrganizationId`.
- A user may belong to **multiple organizations** and must operate inside a selected organization context.

### 2.2 Tenant isolation rules (hard requirements)

- All reads and writes must be filtered/validated by `OrganizationId`.
- Cross-tenant access is not possible.
- Deleting tenants is out of scope (optional later); deactivation is preferred.

---

## 3. Users, Authentication & Authorization

### 3.1 Identity model

- **User**: An authentication account (ASP.NET Core Identity). Global — not scoped to any organization.
- A User may be linked to zero or more Organizations via `OrganizationUser`.
- An `OrganizationUser` may optionally reference a `Person` record in that org.
- **System Administrator**: A User holding the `"SystemAdmin"` ASP.NET Core Identity role. Manages all Organizations, user memberships across all orgs, and other System Administrators. Operates via `/system/*`. Cannot access org-internal data (people, activities, finance) for orgs they are not a member of. The initial System Administrator is seeded from environment variables (`ARELIA_ADMIN_EMAIL`, `ARELIA_ADMIN_PASSWORD`) on first startup.
- **Pending Account**: A User account created by an Invitation whose password has not yet been set (null password hash, `EmailConfirmed = false`). Cannot log in until Registration Completion.

### 3.2 Login & tenant selection

- **System Administrators** are routed to `/system/` on login (or choose their org context if also an org member).
- If a regular user belongs to **one** organization: auto-select that tenant on login.
- If a regular user belongs to **multiple** organizations: show a **tenant selector** after login.
- Store "last selected tenant" per user for convenience.

### 3.3 Role model

All roles live in a single unified pool (the `Role` entity) distinguished by a `RoleType` field.

**System Roles** — seeded automatically per organization on creation:

| Role | RoleType | Permissions |
|---|---|---|
| **Admin** | `Admin` | Hard-coded full access. RolePermission entries are not used. Cannot be edited. |
| **Board** | `Board` | Pre-seeded default permissions (see §3.4). Org admins may edit. |
| **Member** | `Member` | Pre-seeded minimal permissions (see §3.4). Org admins may edit. |

**Custom Roles** (`RoleType = Custom`) — org-defined. Admins create custom roles (e.g., Treasurer, Conductor) and assign permissions freely.

Rules:
- A Person may hold **multiple simultaneous** RoleAssignments from both system and custom roles.
- A User requires **at least one active RoleAssignment** to access an org. Zero active roles = no org access.
- There is no "former member" access state. When a person leaves, their RoleAssignments are ended with a `ToDate`. Their Person record and history are preserved.

### 3.4 Permissions

Permissions are granular capability flags. Effective permissions are the union of all permissions across all Active Roles (`Admin` role bypasses this and grants full access).

| Permission | Description |
|---|---|
| `ManagePeople` | Create/edit/deactivate persons, invite users to org, assign roles |
| `ManageActivities` | Create/edit/cancel activities, semesters, rehearsal templates |
| `ManageAttendance` | Register attendance for any person |
| `RsvpOnBehalf` | Set RSVP on behalf of another person |
| `ManageCharges` | Create/edit charges and payments |
| `ManageExpenses` | Create/edit expenses |
| `ManageDocuments` | Create/edit/delete documents |
| `ViewAttendanceReports` | Access attendance reports |
| `ViewFinanceReports` | Access finance reports |
| `ViewMembershipReports` | Access membership and role reports |
| `OrgSettings` | Edit organization settings and configuration |
| `Backups` | Trigger and manage database backups |

> `UserManagement` has been removed. User invitation and removal are covered by `ManagePeople`.

### 3.5 Authorization summary

| Action | Requirement |
|---|---|
| Create organizations | System Administrator only |
| Manage all organizations (system level) | System Administrator only |
| Org configuration | `Admin` role or `OrgSettings` |
| Create/update persons, invite users, assign roles | `Admin` role or `ManagePeople` |
| Create/update activities | `Admin` role or `ManageActivities` |
| Attendance registration | `Admin` role or `ManageAttendance` |
| Finance (charges/payments) | `Admin` role or `ManageCharges` |
| Finance (expenses) | `Admin` role or `ManageExpenses` |
| Documents | `Admin` role or `ManageDocuments` |
| Reports | `Admin` role or respective `ViewXxx` permission |
| Backups | `Admin` role or `Backups` |
| View calendar, RSVP, own finance | At least one active role (any) |
---

## 4. Domain Concepts

### 4.1 Semester

- The organization plans work in **semesters** (usually two per year).
- A **Semester is an Activity** with `ActivityType = Semester`.
- Semesters contain sub-activities (rehearsals, concerts, trips).

### 4.2 Activity types

Activities include:

- Semester
- Rehearsal
- Concert
- Trip
- Event (generic)
- Other

### 4.3 Participation model (Expectation + RSVP + Attendance)

Each person’s relationship to an activity is tracked with:

- **Expectation** (especially rehearsals):
    
    - `Expected`, `Optional`, `NotExpected`
- **RSVP** (for activities where RSVP is enabled):
    
    - `Yes`, `No`, `Maybe`, `Unanswered`
- **Attendance** (actual):
    
    - `Unknown`, `Attended`, `Absent`

> This keeps rehearsals simple while still enabling RSVP for trips/events.

---

## 5. Functional Requirements

## 5.1 Organization & Configuration

Organization creation is restricted to **System Administrators** (via `/system/`). Org Admins manage their own organization settings.

- Create and manage organization profile (System Admin creates; Org Admin edits):
    - Name, contact email, phone, timezone, default language, default currency
- Org-Admin-configurable options:
    - Default currency (e.g., DKK)
    - Default timezone
    - Visibility rules defaults (e.g. default `IsPublicFacing=false`)
    - Rehearsal template defaults (day of week, start time, duration, location)

## 5.2 People Management

Requires `Admin` role or `ManagePeople` permission.

- Create/update/deactivate Persons
- A Person can be created **without an email** (no linked User account) — for tracking people who do not use the system
- If an email is provided during Person creation, an **Invitation** is sent and a User account is created
- Track: name, email, phone
- Show lists:
    - **Active persons** — at least one active RoleAssignment
    - **Inactive persons** — Person records with no active RoleAssignments (historical, preserved)

## 5.3 Roles & Role History

Requires `Admin` role or `ManagePeople` permission.

**System Roles** (`RoleType = Admin | Board | Member`) are seeded per org and cannot be deleted. The `Admin` role permissions are hard-coded; `Board` and `Member` default permissions are editable.

**Custom Roles** (`RoleType = Custom`) are org-defined (e.g., Treasurer, Conductor, Vice President). Permissions are freely assigned.

- View and edit permissions on Board, Member, and Custom roles
- Create/rename/delete Custom roles
- Assign roles to persons via dated role periods: `FromDate`, `ToDate` (nullable)
- A person can hold multiple simultaneous role assignments
- Show current roles and role history timeline per person

## 5.4 Activities & Calendar

- Create activities with:
    - StartDateTime, EndDateTime (nullable)
    - Location (string)
    - Status: Draft / Published / Cancelled
    - ParentActivityId (nesting under semester)
    - `IsPublicFacing` (bool) — marks the activity for inclusion on a future public-facing calendar

Calendar views:

- Month/week/agenda
- Filters:
    - semester
    - activity type
    - “my activities”

## 5.5 RSVP, Capacity, Signup Deadline, Waiting List

Activities (typically concerts/trips/events) may enable RSVP:

- `RsvpEnabled = true`
- `SignupDeadline` optional
- `Capacity` optional
- `WaitingListEnabled` optional

Rules:

- If `Capacity` is not set: RSVP `Yes` does not waitlist.
- If `Capacity` is set and full:
    - RSVP `Yes` → becomes `Waitlisted` automatically _(if waiting list enabled)_
    - otherwise reject Yes with reason “Full” _(if waiting list disabled)_

Promotion:

- When a confirmed spot frees up, **Board/Admin manually promotes** the earliest waiting list entry.

## 5.6 Rehearsals — Recurrence Template + Manual Changes

### 5.6.1 Rehearsal generation

- A semester can define one or more **Rehearsal Recurrence Templates**.
- The system can **generate rehearsal activities** for a semester from the template(s).
- Generated rehearsals become ordinary `Activity` rows and can be:
    - edited individually (time, location)
    - cancelled individually
    - have attendance registered

### 5.6.2 Manual changes

- Rehearsals can always be:
    - added manually
    - edited manually
    - cancelled manually
- Cancelled rehearsals do not count toward attendance statistics.

## 5.7 Attendance tracking

- For rehearsals:
    - expectation is set per person: Expected / Optional / NotExpected
- For events:
    - RSVP supports Yes/No/Maybe
- Attendance registration:
    - Unknown → Attended/Absent
- Reports must support:
    - expected vs attended counts
    - missing attendance registration detection

## 5.8 Finance — Charges, Discounts, Optional Top-ups, Credit

### Charges

Charges represent what someone should pay (membership fee, trip fee, etc.).

Features:

- Charges can be generated per semester for all current members.
- Charges support **line items**:
    - Base fee
    - Discount (negative)
    - Optional top-up (positive, optional, selectable)
    - Adjustments

### Payments

Payments represent incoming money and may be:

- linked to a specific charge
- unallocated (no charge link)
- from unknown payer (no person link)

### Partial payments

- A charge can be paid in multiple payments.
- Charge status:
    - Open → PartiallyPaid → Paid

### Overpayment credit

- If payments exceed what is due:
    - excess becomes **credit** for that person in that organization
- Credit can later be applied to reduce new charges.

### Grouping

Income/expense can be grouped by:

- semester
- year
- activity

## 5.9 Finance — Expenses (Outgoing)

Track outgoing payments:

- conductor pay
- purchases (sheet music, venue)
- reimbursements

Expense fields include:

- payee (person or external)
- description
- category
- linked semester/activity (optional)

## 5.10 Reporting (MVP)

### Membership reports

- Current members
- Member history per semester/year
- Role history per person and per role

### Attendance reports

- Attendance rate per person per semester
- Rehearsal list with attendance
- Event RSVP summary (Yes/No/Maybe + waiting list)

### Finance reports

- Open charges and overdue list
- Income summary by semester/year/activity
- Expense summary by semester/year/category
- Net result by semester/year
- Credit balances by member

Exports:

- CSV export for finance and attendance reports.

## 5.11 Backups

- Users with Backups permission (or Admin role) can trigger a database backup
- Backups stored in a docker volume
- Backup list with timestamps
- Restore instructions documented (manual restore is acceptable initially)

---

## 5.12 User Registration & Invitation

**Self-service registration is disabled.** The `/Account/Register` page shows an "invitation only" message to anyone who navigates to it directly.

### Invite paths (Org Admin or user with `ManagePeople`)

Two paths are available when inviting a person to an org:

**Path A — Person-first:** Select an existing Person in the org (one without a linked User), then enter their email.
- The system auto-detects whether a User account already exists for that email.
- If **new email**: Pending Account created + invitation email sent → `/Account/AcceptInvitation`
- If **existing User**: OrganizationUser linked + notification email sent (no confirmation required)

**Path B — Email-first:** Enter an email directly without selecting an existing Person.
- The system auto-detects whether a User account already exists.
- If **new email**: Pending Account + new Person record created + invitation email sent
- If **existing User**: new Person created, linked to that User + notification email sent

In both paths a single initial role is selected (default: `Member`).
A Person can also be created with no email at all — no User account, no email, Member role assigned.

### System Administrator flow (`/system/`)

System Admins can invite users to any org using the same two paths above.  
Additionally, System Admins can **directly assign** an existing User to an org:
- Silent (no email sent)
- Always creates a minimal Person record (name sourced from the User account)
- Default role: `Member`

### Invitation email

- **New User email**: org name, inviter name, brief blurb, "Set your password" link → `/Account/AcceptInvitation` (token valid for **7 days**)
- **Existing User email**: org name, "you've been added" notification, no action required

### Registration Completion (`/Account/AcceptInvitation`)

- Accessible only via a valid invite link (contains a password-reset token)
- User sets their password; email is confirmed; user is signed in automatically
- Expired links show a clear error with a self-service **"Resend invitation"** button
  - Self-service resend works only if the account is still Pending (null password hash)
  - Admins (`ManagePeople`) can also resend from the Person's profile page

## 6. Data Model (Entities)

### Shared fields (all entities)

- `Id`
- `OrganizationId`
- `IsActive`

---

## 6.1 Organization

- `Name`
- `ContactEmail`
- `ContactPhone`
- `DefaultCurrencyCode`
- `DefaultPublicVisible` (bool)
- `DefaultRehearsalDay` (DayOfWeek, nullable)
- `DefaultRehearsalStartTime` (nullable)
- `DefaultRehearsalDurationMinutes` (nullable)
- `DefaultRehearsalLocation` (nullable)
- `Timezone` (IANA timezone string, e.g. `"Europe/Copenhagen"`)
- `DefaultLanguage` (e.g. `"en"`, `"da"`, `"is"`)
- `IsActive` (bool) — orgs can be deactivated by System Administrators

---

## 6.2 User (Auth identity)

Stored in ASP.NET Core Identity (`ApplicationUser`).

- `Email` / `UserName`
- `EmailConfirmed` (bool) — `false` for Pending Accounts
- `PasswordHash` — `null` for Pending Accounts (awaiting Registration Completion)
- `PreferredLanguage` (nullable string) — overrides org default language
- Holds zero or more ASP.NET Core Identity roles (e.g. `"SystemAdmin"`)

---

## 6.3 OrganizationUser (joins User ↔ Organization)

Represents a user’s membership in an organization and their access role.

- `UserId`
- `OrganizationId`

- `PersonId` (nullable) — link to Person within this org
- `IsActive`

---

## 6.4 Person

- `OrganizationId`
- `Name`
- `Email`
- `PhoneNumber`

Derived/calculated:

- `CurrentRoles`

Relationships:

- 1 Person → many RoleAssignments
- 1 Person → many ActivityParticipants
- 1 Person → many Charges (payer)
- 1 Person → many Payments
- 1 Person → many Credit entries/balance

---

## 6.5 Role

- `OrganizationId`
- `Name`
- `RoleType` — `Admin | Board | Member | Custom`
- `IsActive`

System Roles (`Admin`, `Board`, `Member`) are seeded per org and cannot be deleted. `Admin` permissions are hard-coded; `Board` and `Member` ship with editable defaults via `RolePermission` entries. Custom roles start with no permissions.

---

## 6.6 RoleAssignment (Role period)

- `OrganizationId`
- `PersonId`
- `RoleId`
- `FromDate`
- `ToDate` (nullable)

---

## 6.7 Activity

- `OrganizationId`
- `ActivityType` (Semester, Rehearsal, Concert, Trip, Event, Other)
- `Name`
- `Description`
- `StartDateTime`
- `EndDateTime` (nullable)
- `Location` (string)
- `Status` (Draft, Published, Cancelled)
- `ParentActivityId` (nullable)
- `IsPublicFacing` (bool)

RSVP/Signup fields (optional):

- `RsvpEnabled` (bool)
- `SignupDeadline` (nullable)
- `Capacity` (nullable)
- `WaitingListEnabled` (bool)

---

## 6.8 RehearsalRecurrenceTemplate

Defines how to generate rehearsals for a semester.

- `OrganizationId`
- `SemesterActivityId`
- `DayOfWeek` (enum)
- `StartTime` (time)
- `DurationMinutes`
- `StartDate` (date)
- `EndDate` (date)
- `Location` (string, optional default)
- `ExpectationDefault` (Expected/Optional, default for members)
- `GenerateExclusions` (optional later; start as manual exclusions)

> Generation creates Activity records of type `Rehearsal`.

---

## 6.9 ActivityParticipant

Represents a person’s participation for an activity.

- `OrganizationId`
- `ActivityId`
- `PersonId`

Expectation:

- `ExpectationStatus` (Expected, Optional, NotExpected)

RSVP:

- `RsvpStatus` (Unanswered, Yes, No, Maybe)

Signup state (needed for waiting list):

- `SignupStatus` (None, Confirmed, Waitlisted, Declined)

Attendance:

- `AttendanceStatus` (Unknown, Attended, Absent)

Other:

- `WaitlistPosition` (nullable integer)
- `Note` (optional)

---

## 6.10 Charge

- `OrganizationId`
- `ChargeType` (MemberFee, AnnualFee, EventFee, Other)
- `PayerPersonId` (nullable but typically set for member fees)
- `ActivityId` (nullable)
- `Name`
- `Description`
- `CurrencyCode`
- `DateCreated`
- `DueDate`
- `Status` (Open, PartiallyPaid, Paid, Cancelled)

Calculated:

- `TotalAmountDue` (sum of selected ChargeLines)
- `TotalPaid` (sum of linked Payments + applied credit)
- `Outstanding` (= Due - Paid)

---

## 6.11 ChargeLine (supports discount + optional top-up)

- `OrganizationId`
- `ChargeId`
- `LineType` (Base, Discount, TopUp, Adjustment)
- `Description`
- `Amount` (decimal; discounts are negative)
- `IsOptional` (bool)
- `IsSelected` (bool)

---

## 6.12 Payment

- `OrganizationId`
- `PayerPersonId` (nullable)
- `ChargeId` (nullable)
- `PaymentDate`
- `Amount`
- `CurrencyCode`
- `Description` (free text)

---

## 6.13 CreditBalance

- `OrganizationId`
- `PersonId`
- `CurrencyCode`
- `BalanceAmount`

---

## 6.14 Expense

- `OrganizationId`
- `PayeePersonId` (nullable)
- `PayeeName` (nullable)
- `ActivityId` (nullable)
- `ExpenseDate`
- `Amount`
- `CurrencyCode`
- `Category` (string or enum)
- `Description`

---

## 6.15 Currency

- `OrganizationId` _(or global table; choose one implementation)_
- `Code` (DKK, EUR…)
- `Name`
- `IsActive`

---

## 7. Key Workflows (Use Cases)

## 7.1 Tenant selection / System Admin routing

1. User logs in
2. If the user is a **System Administrator**:
    - Route to `/system/` (or offer choice if also an org member)
3. Else if the user has **one** org membership: auto-select that org
4. Else if the user has **multiple** memberships: show the tenant selector
5. Else if the user has **no** org memberships: show an "awaiting access" message
6. Load all data scoped to the selected tenant

## 7.2 Create semester + generate rehearsals

1. User with ManageActivities (or Admin) creates `Activity (Semester)`
2. User with ManageActivities (or Admin) defines one or more `RehearsalRecurrenceTemplate`
3. User with ManageActivities (or Admin) clicks “Generate rehearsals”
4. System generates concrete `Activity (Rehearsal)` instances
5. User with ManageActivities (or Admin) manually edits/cancels individual rehearsals as needed

## 7.3 RSVP with waiting list

1. Member opens activity
2. Sets RSVP (Yes/No/Maybe)
3. If RSVP Yes and capacity full:
    - set `SignupStatus=Waitlisted`, assign WaitlistPosition
4. If a spot frees:
    - User with ManageActivities or Admin role chooses “Promote from waiting list”
    - system marks selected waitlisted participant as Confirmed

## 7.4 Membership fee generation with discounts/top-ups

1. Board/Admin selects semester
2. System creates charges for current members:
    - Base fee line (selected)
    - Optional top-up line (not selected)
    - Discount line (if applicable)
3. Member may choose to enable top-up (if you want member-driven; otherwise board sets it)

## 7.5 Payments and credit

- Payments can be recorded and linked to charges.
- If payment exceeds outstanding amount:
    - excess adds to credit balance
- Credit can be applied to new charges.

## 7.6 Invite a user to an organization

### Path A — Person-first (existing Person, no User yet)
1. Admin (System Admin or user with `ManagePeople`) selects an existing Person in the org (one with no linked User)
2. Enters the Person's email + selects initial role (default: `Member`)
3. System checks if a User with that email exists:
   - **New email**: create Pending Account → generate password-reset token (7-day expiry) → send invitation email
   - **Existing User**: link to existing User → send notification email
4. Create `OrganizationUser` with `PersonId` pointing to the selected Person; create `RoleAssignment`

### Path B — Email-first (new Person + User)
1. Admin opens the invite form, enters email + first/last name + initial role (default: `Member`)
2. System checks if a User with that email exists:
   - **New email**: create Pending Account → generate token → send invitation email
   - **Existing User**: link to existing User → send notification email
3. Create new `Person` record + `OrganizationUser` + `RoleAssignment`

## 7.7 Complete Registration (accept invitation)

1. Invited user clicks the link in the email → `/Account/AcceptInvitation`
2. If token is valid: user enters and confirms their password → password saved, `EmailConfirmed = true` → user signed in
3. If token is **expired**: error page shown with "Resend invitation" button
   - Resend only works for Pending Accounts (null password hash)
   - Self-service resend: available to the invited user from the expired-link page
   - Admin resend: available to any user with `ManagePeople` from the Person's profile

## 7.8 Suspend a user from an organization

1. Admin (System Admin or user with `ManagePeople`) chooses "Suspend" on a Person's profile
2. All active `RoleAssignment` records for that Person are ended with `ToDate = today`
3. The `OrganizationUser` link is preserved — the User still exists in the org but has no access
4. **Reinstatement**: admin assigns new roles via the normal role assignment UI (no special "reinstate" action)

## 7.9 Remove a user from an organization

1. Admin (System Admin or user with `ManagePeople`) chooses "Remove from org" — presented with a destructive-action warning
2. All active `RoleAssignment` records are ended
3. The `OrganizationUser` record is deleted
4. The `Person` record is preserved with full history intact (appears in People list as unlinked)
5. The Person can be re-invited later via Path A (Person-first invite)
---

## 8. Open Items / Final Tiny Clarifications (only what’s still needed)

Since **Q1=C** (recurrence + manual), to fully define the recurrence template we need your “default rehearsal pattern”:

1. Typical rehearsal **day of week**?
2. Typical start time and duration? (e.g., 19:00, 150 minutes)
3. Does the choir rehearse during school holidays, or do you usually skip them?
    - If you prefer simplicity: we **generate all weekly rehearsals** and you manually cancel exceptions.

Reply with something like:

Plain Text

Rehearsals: Tue 19:00–21:30, usually skip: (none / specific weeks / school holidays)  

Show more lines

--- End of specification document.

# Arelia.Application — Application-layer map (Commands/Queries per module)

This is a **CQRS-style** map for `Arelia.Application`, designed to keep UI (`Arelia.Web`) thin and keep EF Core concerns in `Arelia.Infrastructure`. It assumes **multi-tenant** (`OrganizationId` required everywhere), **Blazor Server**, and the role model you defined (Admin/Board/Member/FormerMember).

---

## 0) Conventions (recommended)

### Naming

- **Commands**: imperative, write side effects
    - `CreateX`, `UpdateX`, `DeactivateX`, `AssignX`, `GenerateX`, `RecordX`, `ApplyX`, `PromoteX`
- **Queries**: read-only
    - `GetX`, `ListX`, `SearchX`, `GetXSummary`, `GetXReport`

### Request/Response shape

- Commands return `Result` / `Result<T>` (success + domain error codes)
- Queries return DTOs and `PagedResult<T>` where relevant

### Tenant & user context (cross-cutting)

All handlers should require:

- `ICurrentTenant` → `OrganizationId`
- `ICurrentUser` → `UserId` + application role within tenant
- `IClock` → current time
- `IAuthorizationService` (or a simpler policy checker) for role checks

### Persistence boundary

- Application layer depends on **interfaces**:
    - `IPersonRepository`, `IActivityRepository`, `IChargeRepository`, etc.
    - `IUnitOfWork` to commit
- Infrastructure implements repositories with EF Core

### Validation approach

- Validate command input early (required fields, date ranges)
- Validate invariants inside domain/application (e.g., cannot promote from waiting list if no capacity)

---

## 1) Suggested folder structure inside `Arelia.Application`

```
Arelia.Application/
  Abstractions/
    Auth/
    Tenancy/
    Persistence/
    Time/
    Results/
  Organizations/
    Commands/
    Queries/
    Dtos/
  Users/
    Commands/
    Queries/
    Dtos/
  People/
    Commands/
    Queries/
    Dtos/
  Roles/
    Commands/
    Queries/
    Dtos/
  Activities/
    Commands/
    Queries/
    Dtos/
    Recurrence/
      Commands/
      Queries/
      Dtos/
    Participation/
      Commands/
      Queries/
      Dtos/
  Finance/
    Charges/
      Commands/
      Queries/
      Dtos/
    Payments/
      Commands/
      Queries/
      Dtos/
    Credit/
      Commands/
      Queries/
      Dtos/
    Expenses/
      Commands/
      Queries/
      Dtos/
  Reporting/
    Queries/
    Dtos/
  Backups/
    Commands/
    Queries/
    Dtos/
```

---

## 2) Module: Organizations (Tenancy)

### Commands

- `CreateOrganization`
    - Creates a new tenant (Organization) and initial Admin membership
    - **Auth**: system-level (depends on your onboarding model)
- `UpdateOrganizationProfile`
    - Update name/description/contact info
    - **Auth**: Admin
- `UpdateOrganizationSettings`
    - Default currency, defaults (e.g., default `PublicVisible=false`)
    - **Auth**: Admin
- `DeactivateOrganization`
    - Soft-deactivate tenant (optional)
    - **Auth**: Admin

### Queries

- `GetOrganizationById`
- `GetOrganizationSettings`
- `ListUserOrganizations`
    - For tenant selector after login (Q3 = Yes)

---

## 3) Module: Users (Membership in tenant + roles)

> This module manages the **OrganizationUser** join and role assignment at the application security level (Admin/Board/Member/FormerMember), not choir roles.

### Commands

- `InviteUserToOrganization`
    - Adds user email / user id to tenant and assigns app role
    - Optionally links to a Person
    - **Auth**: Admin
- `UpdateOrganizationUserRole`
    - Change app role (e.g., Member → FormerMember)
    - **Auth**: Admin
- `LinkOrganizationUserToPerson`
    - Associate account with `PersonId`
    - **Auth**: Admin/Board (depending on your preference)
- `DeactivateOrganizationUser`
    - Remove access (soft)
    - **Auth**: Admin

### Queries

- `GetOrganizationUser`
- `ListOrganizationUsers`
    - Admin view: who has access
- `GetMyOrganizationMembership`
    - Returns current user’s role in current tenant + linked person id

---

## 4) Module: People

### Commands

- `CreatePerson`
    - **Auth**: Board/Admin
- `UpdatePerson`
    - **Auth**: Board/Admin
- `DeactivatePerson`
    - Soft-deactivate (historical records remain)
    - **Auth**: Board/Admin
- `MergePeople` _(optional, later)_
    - For duplicates (nice-to-have)

### Queries

- `GetPersonDetails`
    - Includes current roles, contact data, membership status
- `ListPeople`
    - Filters:
        - current members only
        - former members
        - role filters
- `SearchPeople`
    - by name/email/phone
- `GetPersonTimeline`
    - role history + participation summaries (optional)

---

## 5) Module: Roles (domain roles + role assignments)

### Commands

- `CreateRole`
    - **Auth**: Admin (or Board if you want)
- `UpdateRole`
    - **Auth**: Admin
- `DeactivateRole`
    - **Auth**: Admin
- `AssignRoleToPerson`
    - Create RoleAssignment (From/To)
    - **Auth**: Board/Admin
- `UpdateRoleAssignment`
    - change dates
    - **Auth**: Board/Admin
- `EndRoleAssignment`
    - set ToDate
    - **Auth**: Board/Admin

### Queries

- `ListRoles`
- `GetRoleDetails`
- `ListRoleAssignments`
    - by person / by role / date range
- `GetCurrentRolesForPerson`
    - computed snapshot

---

## 6) Module: Activities (including Semesters)

### Commands (core)

- `CreateActivity`
    - Type: Semester/Rehearsal/Concert/Trip/Event/Other
    - **Auth**: Board/Admin
- `UpdateActivity`
    - **Auth**: Board/Admin
- `CancelActivity`
    - sets Status=Cancelled
    - **Auth**: Board/Admin
- `PublishActivity`
    - Draft → Published
    - **Auth**: Board/Admin
- `SetActivityPublicVisibility`
    - Controls FormerMember access (Q2 = C)
    - **Auth**: Board/Admin (or Admin only, your call)
- `SetActivityCapacityAndSignupRules`
    - Capacity, signup deadline, waiting list enabled
    - **Auth**: Board/Admin

### Queries (core)

- `GetActivityDetails`
    - includes participant summary, RSVP counts, attendance counts, waiting list status
- `ListActivities`
    - filters:
        - date range
        - semester
        - type
        - my activities
- `GetCalendarView`
    - returns calendar items for month/week/agenda
    - applies visibility rules by app role:
        - Member sees all published tenant activities
        - FormerMember sees only Published + PublicVisible

### Commands (semester-specific)

- `CreateSemester`
    - convenience wrapper around CreateActivity(ActivityType=Semester)
- `CloseSemester` _(optional)_
    - marks semester as archived/closed for reporting

### Queries (semester-specific)

- `ListSemesters`
- `GetSemesterSummary`
    - rehearsal count, event count, attendance summary, finance summary

---

## 7) Module: Rehearsal Recurrence (Q1 = C)

### Commands

- `CreateRehearsalRecurrenceTemplate`
    - day-of-week, start time, duration, date range, default location
    - **Auth**: Board/Admin
- `UpdateRehearsalRecurrenceTemplate`
    - **Auth**: Board/Admin
- `DeleteRehearsalRecurrenceTemplate`
    - **Auth**: Board/Admin
- `GenerateRehearsalsForSemester`
    - Generates Rehearsal activities under the semester, skipping duplicates
    - **Auth**: Board/Admin
- `RegenerateRehearsalsForSemester` _(optional, later)_
    - Typically dangerous; if added, must not overwrite manually edited rehearsals by default

### Queries

- `ListRehearsalRecurrenceTemplates`
- `PreviewGeneratedRehearsals`
    - returns dates/times that would be generated before committing

---

## 8) Module: Participation, RSVP, Attendance (Activities.Participation)

### Commands (participation setup)

- `InitializeParticipantsForActivity`
    - Create ActivityParticipant rows for expected group:
        - all current members / role-based subset / manual list
    - **Auth**: Board/Admin
- `SetParticipationExpectation`
    - Expected / Optional / NotExpected (especially rehearsals)
    - **Auth**: Board/Admin

### Commands (RSVP)

- `SetMyRsvp`
    - Member sets Yes/No/Maybe before deadline
    - **Auth**: Member (linked person required)
- `SetRsvpForPerson`
    - Board/Admin can set RSVP for someone (phone call etc.)
    - **Auth**: Board/Admin

### Commands (waiting list mechanics) (Q4 = Yes, manual promotion)

- `JoinActivityWaitlist`
    - Typically triggered automatically when RSVP Yes and capacity is full & waiting list enabled
    - Sets `SignupStatus=Waitlisted` and assigns position
- `PromoteFromWaitlist`
    - Board/Admin manually promotes the earliest (or selected) waitlisted participant
    - **Auth**: Board/Admin
- `RemoveFromWaitlist`
    - Board/Admin removes (or member changes RSVP away from Yes)
    - **Auth**: Board/Admin (and Member for self if you allow)

### Commands (attendance)

- `RecordAttendanceForActivity`
    - Batch update statuses for participants
    - **Auth**: Board/Admin
- `RecordAttendanceForPerson`
    - Single-person change (quick edit)
    - **Auth**: Board/Admin
- `ResetAttendanceForActivity`
    - sets all to Unknown (optional)
    - **Auth**: Board/Admin

### Queries

- `GetActivityParticipationSummary`
    - counts: Expected/Optional, RSVP Yes/No/Maybe, Attended/Absent, waiting list count
- `ListParticipantsForActivity`
    - includes expectation + RSVP + attendance + waitlist position
- `GetMyParticipation`
    - “my upcoming activities”, my RSVP, my attendance history
- `ListMissingAttendanceRegistrations`
    - rehearsals in date range where attendance still Unknown for any expected participant

---

## 9) Module: Finance — Charges (invoice-like) + ChargeLines

### Commands

- `CreateCharge`
    - manual charge for person or activity
    - **Auth**: Board/Admin
- `UpdateCharge`
    - name/description/due date/status
    - **Auth**: Board/Admin
- `CancelCharge`
    - **Auth**: Board/Admin
- `AddChargeLine`
    - Base / Discount (negative) / TopUp / Adjustment
    - **Auth**: Board/Admin
- `UpdateChargeLine`
    - **Auth**: Board/Admin
- `RemoveChargeLine`
    - **Auth**: Board/Admin
- `SelectOptionalChargeLine`
    - enables member top-up contribution
    - **Auth**: Member (for own charge) and Board/Admin for anyone
- `GenerateMembershipFeesForSemester`
    - Creates charges for all current members
    - Adds default base line + optional top-up line + discounts where configured
    - **Auth**: Board/Admin
- `ApplyDiscountToMemberFee`
    - adds/updates discount line
    - **Auth**: Board/Admin

### Queries

- `GetChargeDetails`
    - includes lines, paid amounts, outstanding, status
- `ListCharges`
    - filters:
        - open/overdue/paid
        - by person
        - by semester/activity
- `GetMemberFinancialOverview`
    - my charges, payments, credit balance (Member view)
- `GetOutstandingChargesSummary`
    - for dashboards and semester reports

---

## 10) Module: Finance — Payments (incoming)

### Commands

- `RecordPayment`
    - payment may be linked to charge or unallocated
    - If linked and amount exceeds outstanding: create credit for excess
    - **Auth**: Board/Admin
- `LinkPaymentToCharge`
    - allocate unallocated payment to charge
    - **Auth**: Board/Admin
- `UnlinkPaymentFromCharge`
    - de-allocate (may affect credit)
    - **Auth**: Board/Admin
- `DeletePayment` _(optional)_
    - likely restricted; prefer “reversal payment” later

### Queries

- `GetPaymentDetails`
- `ListPayments`
    - by date range, by person, by charge, by semester/activity

---

## 11) Module: Finance — Credit (overpayment credit)

### Commands

- `ApplyCreditToCharge`
    - Uses available credit to reduce outstanding
    - **Auth**: Board/Admin
- `AdjustCreditBalance` _(Admin-only, optional)_
    - manual correction with reason

### Queries

- `GetCreditBalanceForPerson`
- `ListCreditBalances`
    - e.g., treasurer view
- `GetCreditLedgerForPerson` _(optional)_
    - if you track credit entries rather than just a balance

---

## 12) Module: Finance — Expenses (outgoing)

### Commands

- `RecordExpense`
    - payee person or external name
    - optional link to activity/semester
    - **Auth**: Board/Admin
- `UpdateExpense`
    - **Auth**: Board/Admin
- `DeleteExpense` _(optional)_
    - prefer reversal later, but okay for MVP if restricted

### Queries

- `GetExpenseDetails`
- `ListExpenses`
    - by date range, semester, category, activity

---

## 13) Module: Reporting (read models only)

> Reporting should be implemented as **Queries only** in `Arelia.Application.Reporting`, using projections/DTOs that are shaped for UI.

### Membership reports

- `GetCurrentMembersReport`
- `GetMembershipChangesBySemesterReport`
- `GetRoleHistoryReport`
    - who held role when

### Attendance reports

- `GetAttendanceByPersonForSemesterReport`
- `GetRehearsalAttendanceRollReport`
- `GetEventRsvpSummaryReport`
- `GetNoShowReport`
    - for selected activity types/date ranges

### Finance reports

- `GetIncomeExpenseSummaryBySemesterReport`
- `GetIncomeExpenseSummaryByYearReport`
- `GetIncomeExpenseSummaryByActivityReport`
- `GetOutstandingChargesReport`
- `GetCreditBalancesReport`
- `ExportFinanceCsv`
- `ExportAttendanceCsv`

---

## 14) Module: Backups / Admin tools

### Commands

- `CreateDatabaseBackup`
    - creates a timestamped backup in backup volume
    - **Auth**: Admin
- `PruneBackups` _(optional, later)_
    - retention policy

### Queries

- `ListBackups`
    - **Auth**: Admin
- `GetBackupDetails`
    - file size, timestamp, etc.

---

## 15) Cross-cutting policies & invariants (should live in Application)

These are rules many handlers should enforce consistently:

### Tenancy

- Every command/query must verify the entity’s `OrganizationId` matches `ICurrentTenant.OrganizationId`.

### Authorization

- Central policy checks:
    - Admin-only: tenant settings, backups, user role management (if you choose)
    - Board/Admin: data registrations
    - Member: read + RSVP for self + optional top-up selection for self

### Finance invariants

- Currency consistency:
    - payment currency must match charge currency when linked
- Charge status calculation:
    - Paid if outstanding <= 0
    - PartiallyPaid if total paid > 0 and outstanding > 0
- Credit behavior:
    - Overpayment automatically adds to credit

### RSVP & waiting list

- RSVP changes after `SignupDeadline`:
    - define behavior: reject change vs allow Board override (recommended: Member blocked after deadline, Board can override)

### Attendance

- Cancelled rehearsals:
    - excluded from attendance statistics and missing-attendance lists

---

## 16) Optional: “MVP cut” (if you want to ship early)

If you want a _very small first release_, keep only:

- Organizations + tenant selection
- People + roles + role assignments
- Activities + calendar + participants
- Attendance (Unknown/Attended/Absent)
- Charges + payments + credit
- Expenses
- Basic reports (attendance per semester, open charges, income/expense by semester)

Everything else (capacity/wait list, recurrence previews, CSV export, backup UI) can follow.

---

## Rehearsal Scheduling (Recurrence + Manual) — Final Rules (Q1=C)

### RehearsalRecurrenceTemplate defaults

Each **Semester** may have one or more templates. A template contains the “intent” to create rehearsals.

**Default template (auto-created when a Semester is created — optional UX choice):**

- `DayOfWeek = Thursday`
- `StartTime = 19:00`
- `DurationMinutes = 150`
- `Location = "Stavangergade 10"`
- `StartDate = Semester.StartDate (date portion)`
- `EndDate = Semester.EndDate (date portion, or last day of semester)`
- `ExpectationDefault = Expected`
- `GenerateMode = GenerateAllWeeks`
- `Timezone = Organization default (or server local)` _(recommended to store explicitly)_

> Note: You decided “Generate all weeks and cancel exceptions manually”, so we do **not** implement holiday calendars or automatic exclusions initially.

---

## Generate Rehearsals For Semester — Command Behavior

### Command

`GenerateRehearsalsForSemester(SemesterActivityId, TemplateId, Options)`

### Options (recommended)

- `AutoInitializeParticipants` (default: **true**)  
    Create participant rows for current members immediately.
- `DefaultExpectationForMembers` (default: **Expected**)  
    For rehearsals, members typically are expected unless manually changed.

### Generated rehearsal Activity fields

For each generated rehearsal occurrence:

- `ActivityType = Rehearsal`
- `ParentActivityId = SemesterActivityId`
- `Name = "Rehearsal"` _(or "Rehearsal – {VoiceGroup}" if extended later)_
- `StartDateTime = occurrenceDate + 19:00`
- `EndDateTime = StartDateTime + 150 minutes` → 21:30
- `Location = Stavangergade 10` _(template default; editable later)_
- `Status = Draft` _(recommended default; can be published later in batch)_
- `IsPublicFacing = false` _(default; rehearsals usually not public)_

---

## Occurrence Generation Rules (Deterministic & Idempotent)

### Date range

- Generate occurrences from:
    - `Template.StartDate` to `Template.EndDate` (inclusive), and
    - within the semester boundaries if you enforce them (recommended)

### Weekly schedule

- Generate an occurrence for **every Thursday** in the date range.

### Idempotency / duplicate detection (important)

The command must be safe to run multiple times. We define “duplicate” as:

- An existing **Rehearsal activity** in the same semester (same `ParentActivityId`)
- With the **same StartDateTime** (same local time + date)
- And **Status != Cancelled** _(cancelled rehearsals stay cancelled; we do not re-add automatically)_

**Rule**

- If a duplicate exists → **skip generation** for that date.
- If the only existing rehearsal is `Cancelled` → **do not recreate** automatically (manual decision).

> This matches your preference: “generate all weeks and cancel exceptions manually.” A cancelled rehearsal is an exception you intentionally made, so the system should not undo it.

### Manual edits preserved

Once a rehearsal is generated, it becomes a normal Activity and can be edited freely.

- The generator **never overwrites** existing rehearsal details.
- Edits such as:
    - time change
    - location change
    - renamed rehearsal
    - cancellation  
        are always respected.

---

## Participant Initialization Rules (Recommended default: enabled)

When `AutoInitializeParticipants = true`:

1. Determine the expected population for the rehearsal:
    
    - **All current members** on the rehearsal date  
        (i.e., persons with an active `RoleAssignment` for `Role=Member` on that date)
2. Create `ActivityParticipant` rows (if missing) with:
    
    - `ExpectationStatus = Expected` _(default)_
    - `AttendanceStatus = Unknown`
    - `RsvpStatus = Unanswered` _(RSVP usually irrelevant for rehearsals; keep for uniformity)_
    - `SignupStatus = None` _(rehearsals don’t use capacity/waitlist by default)_

**Idempotency for participants**

- If an `ActivityParticipant` already exists for `(ActivityId, PersonId)` → do nothing.

> This gives you a clean rehearsal roster immediately, and attendance marking becomes quick.

---

## Publish Strategy (Practical UX)

Because rehearsals may be generated long-term:

- Keep them `Draft` initially (safe)
- Provide a bulk action:
    - `PublishActivitiesForSemester(semesterId, type=Rehearsal, fromDate=...)`

This avoids accidentally exposing unfinished schedules.

---

# Application-layer map updates (delta)

You already have these commands/queries listed. With the default pattern specified, here are the recommended enhancements:

## Activities.Recurrence — Commands

- `CreateRehearsalRecurrenceTemplate`
    - Default values (Thursday, 19:00, 150 mins, Stavangergade 10)
- `GenerateRehearsalsForSemester`
    - Must be **idempotent**
    - Must preserve manual edits
    - Should support `AutoInitializeParticipants=true`

## Activities.Recurrence — Queries

- `PreviewGeneratedRehearsals`
    - For each computed occurrence, return:
        - `OccurrenceDate`
        - `StartDateTime`
        - `EndDateTime`
        - `Status`: `WillCreate | AlreadyExists | CancelledExists | OutOfRange`
    - Helps board/admin validate before generating.

## Activities.Participation — Commands (optional convenience)

- `InitializeParticipantsForActivity`
    - Still useful for events and concerts
- _(New optional helper)_ `InitializeParticipantsForGeneratedRehearsals`
    - Only needed if you ever want to initialize participants after generation.

---

# Small recommendation (non-blocking): store timezone

Even if you only operate locally, storing an organization-level timezone prevents edge-case confusion (DST changes, server moved, etc.). You can default it to your local timezone and never show it unless needed.

---


