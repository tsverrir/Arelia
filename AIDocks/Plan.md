# Arelia — Development Plan

_Based on decisions recorded in [SpecificationReview.md](SpecificationReview.md) and [AGENTS.md](../AGENTS.md)_

---

## Architecture Overview

| Layer | Technology |
|---|---|
| UI | Blazor Server + MudBlazor + Radzen Blazor Scheduler |
| Backend | ASP.NET Core 8, Clean Architecture (Domain / Application / Infrastructure / Web) |
| Auth | ASP.NET Core Identity (cookie-based, `ApplicationUser` extends `IdentityUser`) |
| Database | SQLite via EF Core |
| Hosting | Docker container |
| Localization | ASP.NET Core `IStringLocalizer` (English + Danish) |

### Solution Structure

```
Arelia.sln
├── src/
│   ├── Arelia.Domain/            # Entities, enums, value objects, domain events
│   ├── Arelia.Application/       # Use cases, interfaces, DTOs, validation
│   ├── Arelia.Infrastructure/    # EF Core, Identity, email, file storage, backups
│   └── Arelia.Web/               # Blazor Server app, pages, components, auth config
├── tests/
│   ├── Arelia.Domain.Tests/
│   ├── Arelia.Application.Tests/
│   └── Arelia.Infrastructure.Tests/
```

---

## Phase 1 — Project Scaffolding & Base Infrastructure

**Goal:** Runnable Blazor Server app with authentication, base entity system, and development seed data. No features yet — just the foundation everything else builds on.

### 1.1 — Create solution and project structure
- Create the solution with the four `src/` projects and three `test/` projects
- Configure project references (Domain ← Application ← Infrastructure ← Web)
- Add NuGet packages: MudBlazor, Radzen.Blazor, EF Core (SQLite), ASP.NET Core Identity

### 1.2 — Base entity and shared infrastructure
- Create `BaseEntity` with: `Id` (Guid), `OrganizationId` (Guid), `IsActive` (bool)
- Add audit fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Add `RowVersion` (concurrency token)
- Create `IAuditableEntity` interface
- Create EF Core `SaveChangesInterceptor` to auto-populate audit fields

### 1.3 — EF Core and database setup
- Create `AreliaDbContext` extending `IdentityDbContext<ApplicationUser>`
- Configure SQLite connection (file-based, Docker volume path)
- Configure global query filter for `OrganizationId` (tenant isolation)
- Configure soft delete global query filter (`IsActive = true` default)
- Initial migration

### 1.4 — Authentication and Identity
- Create `ApplicationUser` extending `IdentityUser`
- Configure ASP.NET Core Identity (cookie auth, `RequireConfirmedAccount = false`)
- Remove or restrict the scaffolded `/register` page
- Configure login page with pre-populated dev credentials (`admin@arelia.dev` / `Admin123!`)

### 1.5 — Email infrastructure
- Create `IEmailSender` implementation (SMTP-based)
- Configure SMTP settings via `appsettings.json` / environment variables
- Create a dev-mode `IEmailSender` that logs to console instead of sending

### 1.6 — Localization setup
- Configure ASP.NET Core localization middleware
- Create resource file structure for English (default) + Danish
- Add language selector component (persisted to browser/cookie)
- Create a few sample localized strings to validate the pipeline

### 1.7 — Error handling and logging
- Configure global exception handler middleware
- Create error pages (404, 500, access denied)
- Configure structured logging (Serilog or built-in)
- Create `Result<T>` pattern for application layer (success/failure without exceptions)

### 1.8 — MudBlazor layout shell
- Configure MudBlazor theme and providers
- Create the main layout: sidebar navigation, top app bar, content area
- Add placeholder for notification bell icon (no functionality yet)
- Ensure responsive layout (mobile-friendly)

### 1.9 — Seed data
- Seed the admin user (`admin@arelia.dev` / `Admin123!`)
- No organization yet — the admin will create one in Phase 2

### Phase 1 — Review & Test
- **Code review:** Solution structure, naming conventions, project references
- **Unit tests:** `BaseEntity` audit field population, `Result<T>` pattern, tenant query filter behavior, localization string resolution

---

## Phase 2 — Organizations & Multi-Tenancy

**Goal:** Admin can create an organization, the tenant context works, and users can be invited to organizations.

### 2.1 — Organization entity
- Create `Organization` entity with settings fields:
  - `Name`, `DefaultCurrencyCode` ("DKK"), `DefaultPublicVisible` (false)
  - `DefaultRehearsalDay` (Thursday), `DefaultRehearsalStartTime` (19:00), `DefaultRehearsalDurationMinutes` (150)
  - `DefaultRehearsalLocation` ("Stavangergade 10"), `Timezone` ("Europe/Copenhagen")
- EF Core configuration and migration

### 2.2 — OrganizationUser entity
- Create `OrganizationUser`: `UserId`, `OrganizationId`, `PersonId?`, `IsActive`
- This is the link between a user and an organization
- Active = Member, Inactive = FormerMember

### 2.3 — Tenant context service
- Create `ITenantContext` service that provides the current `OrganizationId`
- Populate from the authenticated user's selected org
- Integrate with EF Core global query filter

### 2.4 — Tenant selection flow
- After login: if user belongs to one org → auto-select; if multiple → show selector
- Store last selected tenant in browser local storage (§1.3)
- Tenant switcher in the app header for multi-org users

### 2.5 — Create Organization workflow
- Admin creates a new organization (§13.3)
- On creation: seed system roles, default expense categories, apply default settings
- Creating user automatically becomes Admin for the new org

### 2.6 — Invite User to Organization
- Admin invites a user by email
- If user account exists → create `OrganizationUser` link
- If user account does not exist → create `ApplicationUser` + send welcome email with password setup link
- Restrict public registration — invitation only (§1.2)

### 2.7 — User management (Admin)
- List users in the current org
- Activate / deactivate `OrganizationUser` (demotion to FormerMember)
- Ban user from system (set `LockoutEnd` — §1.5)
- Show warning when deactivating a person with an active user account

### Phase 2 — Review & Test
- **Code review:** Tenant isolation correctness, organization creation flow
- **Unit tests:** Tenant query filter (cross-tenant blocked), organization creation seeds roles/categories, invitation flow (existing user vs new user), deactivation logic

---

## Phase 3 — Roles & Permissions

**Goal:** The full role and permission system works. Admin can assign roles, customize permissions, and authorization policies enforce access.

### 3.1 — Role entity
- Create `Role`: `Name`, `RoleType` (System/Custom), `OrganizationId`, `IsActive`
- Seed system roles per org: Board, Treasurer, Conductor, Admin

### 3.2 — RoleAssignment entity
- Create `RoleAssignment`: `PersonId`, `RoleId`, `FromDate`, `ToDate`
- Active assignment = today falls within FromDate–ToDate

### 3.3 — RolePermission entity
- Create `RolePermission`: `RoleId`, `Permission` (string/enum)
- Define `Permission` enum: `ManagePeople`, `ManageActivities`, `ManageAttendance`, `RsvpOnBehalf`, `ManageCharges`, `ManageExpenses`, `ViewAttendanceReports`, `ViewFinanceReports`, `ViewMembershipReports`, `OrgSettings`, `UserManagement`, `Backups`
- Seed default permission mappings per system role (from §2.1 table)

### 3.4 — Authorization policies
- Create one ASP.NET Core authorization policy per permission
- Create `PermissionAuthorizationHandler` that:
  1. Checks `OrganizationUser.IsActive`
  2. Gets active `RoleAssignment`s for the current user/org
  3. Computes effective permissions (Member baseline ∪ role permissions)
  4. Checks required permission against the set
- Apply `[Authorize(Policy = "...")]` or equivalent Blazor mechanisms

### 3.5 — Role management UI (Admin)
- **Role definition management:** Create/rename/deactivate custom roles; system roles: rename allowed, delete blocked
- **Role → Permission management:** Table/grid of roles × permissions with toggle switches
- **Role assignment management:** Assign/remove roles to/from a person with FromDate/ToDate; view active and historical assignments

### Phase 3 — Review & Test
- **Code review:** Authorization flow, permission seeding, policy enforcement
- **Unit tests:** Permission computation (additive), authorization handler (active member, FormerMember, multi-role), date-range logic for active assignments, system role protection (cannot delete)

---

## Phase 4 — People Management

**Goal:** Board/Admin can manage the member directory. Person records exist and are linked to user accounts.

### 4.1 — Person entity
- Create `Person`: `FirstName`, `LastName`, `Email`, `Phone`, `VoiceGroup` (enum: Soprano/Alto/Tenor/Bass/null), `Notes`, `OrganizationId`, `IsActive`
- EF Core configuration and migration

### 4.2 — Person CRUD
- Create/edit/deactivate person records
- Link/unlink a Person to an `OrganizationUser` (§3.1 of spec)
- Deactivation warnings when linked user account exists (§1.6)
- Authorization: Board or Admin required

### 4.3 — Person list and search
- Paginated (if needed — <60 members probably doesn't need pagination) list with sorting by last name
- Filter by: voice group, active/inactive
- Search by first name or last name

### 4.4 — Person detail page
- View all person details
- View role assignments (active + historical)
- Quick role assignment from the person page
- View linked user account status

### Phase 4 — Review & Test
- **Code review:** Person entity, CRUD operations, authorization
- **Unit tests:** Person creation validation, voice group enum, deactivation with linked user warning, search/filter logic

---

## Phase 5 — Activities & Calendar

**Goal:** Semesters and activities can be created and viewed. Calendar views work. Activity detail pages show relevant information.

### 5.1 — Activity entity
- Create `Activity`: `Name`, `Description`, `ActivityType` (enum: Semester/Rehearsal/Concert/Trip/Social/Other), `StartDateTime`, `EndDateTime`, `Location`, `ParentActivityId?`, `WorkYear` (computed), `IsPublicVisible`, `MaxCapacity?`, `SignupDeadline?`, `OrganizationId`, `IsActive`
- Nesting validation: max one level (§4.5)
- Semester overlap validation (§4.3)

### 5.2 — Semester management
- Create/edit semesters (date range, no overlap with other semesters)
- List semesters by work year

### 5.3 — Activity CRUD
- Create activities under a semester or standalone
- Edit/deactivate activities
- WorkYear auto-computed from `StartDateTime.Year`
- Authorization: Board or Admin required

### 5.4 — Agenda/list view (primary calendar)
- Chronological list of upcoming activities grouped by month
- Built with MudBlazor components
- RSVP buttons inline (functionality in Phase 7)
- "My activities" filter (§4.6)
- Filter by work year

### 5.5 — Month calendar view (secondary)
- Integrate Radzen Blazor Scheduler (`RadzenScheduler`)
- Month/week/day views
- Multi-day activities render as spanning bars
- Click-through to activity detail page

### 5.6 — Activity detail page
- Activity name, type, date/time, location, description
- Parent semester info (if applicable)
- Placeholder sections for: RSVP, participants, attendance (populated in Phase 7)

### Phase 5 — Review & Test
- **Code review:** Activity entity, nesting/overlap validation, calendar integration
- **Unit tests:** Semester overlap detection, nesting depth validation, WorkYear computation, activity type enum

---

## Phase 6 — Rehearsal Generation

**Goal:** Board/Admin can define recurrence templates and generate rehearsals for a semester.

### 6.1 — RehearsalRecurrenceTemplate entity
- Create `RehearsalRecurrenceTemplate`: `SemesterId`, `DayOfWeek`, `StartTime`, `DurationMinutes`, `Location`, `StartDate`, `EndDate`
- Validation: template dates must fall within semester range (§6.2)
- Validation: no overlapping templates in the same semester (§6.1)

### 6.2 — Rehearsal generation logic
- `GenerateRehearsalsForSemester` command
- Iterate template date range, create `Activity` (type=Rehearsal) for each matching day
- Overlap detection: if a generated rehearsal conflicts with an existing activity on the same date, warn user with conflict list and option to cancel (§6.3)
- Idempotent: skip dates where a rehearsal already exists at the same start time

### 6.3 — Template management UI
- Create/edit/delete templates within a semester
- Show preview of dates that would be generated
- Generate button with conflict warning dialog

### Phase 6 — Review & Test
- **Code review:** Generation logic, overlap detection, template validation
- **Unit tests:** Date iteration logic, overlap detection, idempotency, template-within-semester validation, conflict warning scenarios

---

## Phase 7 — RSVP & Attendance

**Goal:** Members can RSVP for activities. Rehearsals use the absence model. Waiting list works for capacity-limited events. Attendance can be recorded.

### 7.1 — ActivityParticipant entity
- Create `ActivityParticipant`: `ActivityId`, `PersonId`, `RsvpStatus` (enum: Unanswered/Yes/No/Maybe), `SignupStatus` (enum: None/Confirmed/Waitlisted), `WaitlistPosition?`, `RsvpTimestamp`
- For non-rehearsal activities only (rehearsals use implicit participation — §3.4)

### 7.2 — RSVP for rehearsals (absence model)
- All active members are implicit participants
- RSVP = announcing absence (RSVP No) or confirming presence (default assumption = Yes)
- Only create an `ActivityParticipant` record when a member explicitly RSVPs No or Maybe
- No participant record = assumed present

### 7.3 — RSVP for other activities (opt-in model)
- RSVP-ing auto-creates `ActivityParticipant` if none exists (§5.3)
- Capacity management: if `MaxCapacity` is set and spots are full → waitlist
- SignupDeadline enforcement: block after deadline, Board can override (§5.4)

### 7.4 — Waiting list logic
- Back of the list: RSVP Yes → No → Yes again = back of waitlist (§5.2)
- Spot release: RSVP Yes→No = `SignupStatus` auto-changes to None (§5.1)
- Manual promotion: Board/Admin promotes from waitlist via `PromoteFromWaitlist`
- UI indicator: "1 spot available, 3 on waitlist"

### 7.5 — RSVP UI
- RSVP buttons on activity detail page and in agenda list
- RSVP on behalf of others (Board/Admin — §2.1)
- Show current RSVP status, waitlist position if applicable
- Deadline indicator (past deadline = blocked for members)

### 7.6 — Attendance tracking
- `AttendanceRecord` entity: `ActivityId`, `PersonId`, `Status` (enum: Present/Absent/Excused), `RecordedByUserId`, `RecordedAt`
- For rehearsals: Board/Conductor marks attendance after the rehearsal
- Default assumption for rehearsals: present unless RSVP'd No or marked absent
- Authorization: Board, Conductor, or Admin

### 7.7 — Attendance UI
- Activity detail page: attendance grid for Board/Conductor
- Quick mark: present/absent/excused per member
- Summary: count of present/absent/excused

### Phase 7 — Review & Test
- **Code review:** RSVP dual model (absence vs opt-in), waitlist logic, attendance recording
- **Unit tests:** Waitlist ordering, spot release flow, RSVP Yes→No→Yes (back of list), deadline enforcement, capacity check, attendance default assumptions for rehearsals

---

## Phase 8 — Finance: Charges & Payments

**Goal:** Treasurer can generate membership fees, manage charges, and record payments.

### 8.1 — Charge and ChargeLine entities
- Create `Charge`: `PersonId`, `SemesterId?`, `Description`, `DueDate`, `Status` (enum: Open/PartiallyPaid/Paid/Overpaid), `CurrencyCode`, `OrganizationId`
- Create `ChargeLine`: `ChargeId`, `Description`, `Amount`, `LineType` (enum: Base/TopUp/Discount/Other), `IsSelected` (for optional lines)
- Status auto-computed from sum of selected lines vs payments

### 8.2 — Generate membership fees
- `GenerateMembershipFeesForSemester(SemesterId, BaseFeeAmount, TopUpAmount?, DueDate)`
- Creates one `Charge` per active member with:
  - Base line (selected by default)
  - TopUp line (optional, not selected by default)
- Member can toggle own top-up via `SelectOptionalChargeLine`

### 8.3 — Manual charge line management
- Add/edit/remove charge lines (including discount lines with negative amounts)
- Recalculate charge status after line changes
- Warning when editing charges with existing payments (§7.3)
- Overpaid handling: UI warns Treasurer, suggest adding credit

### 8.4 — Payment entity
- Create `Payment`: `ChargeId?`, `PayerPersonId?`, `PayerDescription`, `Amount`, `PaymentDate`, `PaymentMethod?`, `Reference?`, `CurrencyCode`, `OriginalAmount?`, `OriginalCurrencyCode?`, `OrganizationId`
- Validation: if `PayerPersonId` is null, `PayerDescription` is required (§7.5)
- `CurrencyCode` auto-populated from org default

### 8.5 — Payment recording UI
- Record payment against a charge (or as unallocated)
- Payment method and reference (free-text, optional)
- Original currency fields for foreign payments
- Charge status updates automatically

### 8.6 — Finance overview (Treasurer/Admin)
- List of charges per member, per semester
- Outstanding and overdue charges (`DueDate < today && Status != Paid` — §9.2)
- Payment history per charge

### Phase 8 — Review & Test
- **Code review:** Charge status computation, fee generation, payment flow
- **Unit tests:** Charge status calculation (all scenarios: open, partial, paid, overpaid), fee generation for active members, discount line (negative amount), top-up toggle, payment validation (payer required), overdue detection

---

## Phase 9 — Finance: Credit & Expenses

**Goal:** Credit ledger tracks member overpayments/prepayments. Expenses can be recorded with categories and receipt attachments.

### 9.1 — CreditBalance and CreditTransaction entities
- Create `CreditBalance`: `PersonId`, `BalanceAmount` (denormalized running total), `OrganizationId`
- Create `CreditTransaction`: `PersonId`, `Amount`, `Reason`, `Timestamp`, `PaymentId?`, `ChargeId?`, `OrganizationId`
- Balance = sum of all transactions (recomputable)

### 9.2 — Credit operations
- Add credit (from overpayment, manual adjustment)
- Deduct credit (apply to charge)
- View credit ledger per person
- Frozen on member departure (§3.5) — no deductions, Treasurer can manually refund

### 9.3 — Expense entity
- Create `Expense`: `Description`, `Amount`, `ExpenseDate`, `Category` (string, references `ExpenseCategory`), `CurrencyCode`, `OrganizationId`

### 9.4 — ExpenseCategory lookup
- Create `ExpenseCategory`: `Name` (string, ALL CAPS), `OrganizationId`
- Normalization on save: uppercase, trim, collapse spaces (§8.1)
- Seed defaults per org: SHEET MUSIC, VENUE RENTAL, INSTRUMENT MAINTENANCE, TRAVEL, REFRESHMENTS, MARKETING, INSURANCE, OTHER

### 9.5 — Expense attachments
- Create `ExpenseAttachment`: `ExpenseId`, `FileName`, `ContentType`, `FilePath`, `UploadedAt`
- File storage on disk (configurable path, Docker volume)
- Upload and download — no preview, no versioning (§8.2)

### 9.6 — Expense UI
- Create/edit/deactivate expenses
- Category combobox with autocomplete + on-the-fly creation
- Attach/download receipt files
- Expense list with filtering by category, date range

### Phase 9 — Review & Test
- **Code review:** Credit ledger integrity, expense category normalization, file storage
- **Unit tests:** Credit balance computation from transactions, category normalization ("  sheet   music  " → "SHEET MUSIC"), expense attachment metadata, credit freeze on departed member

---

## Phase 10 — Notifications

**Goal:** Members receive in-app and email notifications for important events.

### 10.1 — Notification entity
- Create `Notification`: `RecipientUserId`, `OrganizationId`, `Type` (enum), `Title`, `Message`, `IsRead`, `CreatedAt`, `LinkUrl?`
- Notification types: WaitlistPromotion, RsvpReminder, PaymentReminder, ScheduleChange, NewCharge, Welcome

### 10.2 — INotificationService
- Interface with method: `SendAsync(recipientUserId, type, title, message, linkUrl?)`
- In-app implementation: insert `Notification` record
- Email implementation: send via `IEmailSender` (for important types)
- Configurable per type: in-app only, email only, both

### 10.3 — Domain event integration
- Wire notification creation to domain events:
  - Waitlist promotion → notify promoted member
  - Charge created → notify charged member
  - Activity time/location changed → notify participants
  - RSVP deadline approaching → notify unanswered members
  - Payment recorded → notify member (confirmation)

### 10.4 — Notification UI
- Bell icon in app header with unread count
- Notification dropdown/panel: list of recent notifications
- Mark as read (individual + mark all)
- Click notification → navigate to `LinkUrl`

### Phase 10 — Review & Test
- **Code review:** Notification flow, domain event wiring, email sending
- **Unit tests:** Notification creation from domain events, unread count calculation, mark-as-read logic, email vs in-app routing per type

---

## Phase 11 — Reporting & Export

**Goal:** Treasurer, Board, and Conductor can view reports. Year-end reconciliation works. CSV export available.

### 11.1 — Attendance reports
- Attendance summary per activity / per semester
- Per-member attendance rate
- Filter by voice group, date range
- Authorization: Board + Conductor + Admin

### 11.2 — Finance reports
- Income summary: charges paid, grouped by semester/category
- Expense summary: grouped by category
- Outstanding and overdue charge list
- Per-member financial overview
- Authorization: Treasurer + Admin

### 11.3 — Membership reports
- Active member count (total + by voice group)
- Member join/leave history
- Role assignment overview
- Authorization: Board + Treasurer + Admin

### 11.4 — Year-end reconciliation (§13.2)
- Annual financial summary: total income, total expenses, net balance by category
- Open charges review with write-off or carry-forward options
- Credit balance summary
- Verification checklist: all charges resolved? unallocated payments? balanced?
- Authorization: Treasurer + Admin

### 11.5 — CSV export
- Export any report/table to CSV
- UTF-8 with BOM, semicolon delimiter, ISO 8601 dates, comma decimal separator (§9.3)
- Authorization: same as the underlying report

### 11.6 — Member self-service views
- "My finances" page: own charges, payments, credit balance
- "My attendance" page: own attendance history
- No export for members (only Treasurer/Admin can export)

### Phase 11 — Review & Test
- **Code review:** Report queries, CSV generation, authorization on reports
- **Unit tests:** CSV formatting (encoding, delimiter, date format), overdue calculation, attendance rate computation, year-end summary totals

---

## Phase 12 — Audit & Backups

**Goal:** Sensitive operations are logged. Admin can create and manage database backups.

### 12.1 — AuditLogEntry entity
- Create `AuditLogEntry`: `Timestamp`, `UserId`, `OrganizationId`, `Action`, `EntityType`, `EntityId`, `Details` (JSON)
- Populated via EF Core `SaveChangesInterceptor` or domain events
- Captures: financial operations, role changes, user management, data deletions

### 12.2 — Audit log UI (Admin)
- Read-only list of audit log entries
- Filter by: user, action type, entity type, date range
- Authorization: Admin only

### 12.3 — Backup system
- Manual trigger: Admin clicks "Create Backup" button
- Implementation: copy SQLite file to backup directory with timestamp filename
- List existing backups (filename, size, date)
- Download backup file
- Restore: out of scope for UI (manual file replacement documented)

### 12.4 — Backup retention
- After each backup creation, prune to keep last 30 (§10.3)
- Simple FIFO deletion of oldest

### Phase 12 — Review & Test
- **Code review:** Audit interceptor, backup file handling, retention logic
- **Unit tests:** Audit entry creation on save, FIFO retention (>30 deletes oldest), backup filename format

---

## Phase 13 — Polish & Hardening

**Goal:** Production readiness. Responsive design, error handling, performance, security.

### 13.1 — Responsive design review
- Test all key member flows on mobile (RSVP, calendar, finances)
- Fix layout issues with MudBlazor responsive breakpoints
- Verify Radzen Scheduler works on small screens

### 13.2 — Concurrency conflict handling
- Implement friendly UI for `DbUpdateConcurrencyException`
- "This record was modified by another user. Reload and try again?" dialog
- Test with concurrent edits

### 13.3 — FormerMember experience
- Verify FormerMembers see only `PublicVisible` activities + org contact info
- Verify no access to member-only features
- Contact info page (§1.4)

### 13.4 — Security hardening
- Verify tenant isolation (no cross-org data leaks)
- Verify authorization policies on all pages/endpoints
- CSRF protection (Blazor Server handles this by default)
- Rate limiting on login
- Input validation and sanitization

### 13.5 — Performance review
- Verify query performance at expected scale (<60 members, <100 rehearsals/year)
- Check for N+1 queries in EF Core
- Verify SQLite WAL mode for concurrent reads

### 13.6 — Docker setup
- Dockerfile for the Blazor Server app
- Docker Compose with volume mounts for: SQLite database, backups, expense attachments
- Environment variable configuration for SMTP, paths, etc.
- Health check endpoint

### Phase 13 — Review & Test
- **Code review:** Full security audit, performance review, Docker configuration
- **Integration tests:** End-to-end tenant isolation, FormerMember access restrictions, login flow, concurrent edit handling

---

## Phase Summary

| Phase | Focus | Key Deliverables |
|---|---|---|
| **1** | Foundation | Solution structure, base entity, auth, localization, email, error handling |
| **2** | Organizations | Multi-tenancy, org creation, user invitation, tenant selection |
| **3** | Roles & Permissions | Role system, permission mapping, authorization policies, management UI |
| **4** | People | Person CRUD, voice groups, search, person↔user linking |
| **5** | Activities & Calendar | Semesters, activities, agenda view, month calendar, detail pages |
| **6** | Rehearsal Generation | Recurrence templates, generation logic, conflict detection |
| **7** | RSVP & Attendance | Dual RSVP model, waiting list, attendance tracking |
| **8** | Charges & Payments | Fee generation, charge management, payment recording |
| **9** | Credit & Expenses | Credit ledger, expense categories, receipt attachments |
| **10** | Notifications | In-app + email notifications, domain event wiring |
| **11** | Reporting & Export | Attendance/finance/membership reports, year-end reconciliation, CSV |
| **12** | Audit & Backups | Audit log, manual backups, retention |
| **13** | Polish | Responsive design, security, performance, Docker |

---

_Each phase ends with a code review and unit testing of all testable units produced in that phase. No phase should begin until the previous phase's tests pass._
