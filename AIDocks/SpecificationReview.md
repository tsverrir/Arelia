# Arelia — Specification Review & Gap Analysis

_Review of AGENTS.md — generated during project kickoff_

This document catalogs **gaps, ambiguities, missing edge cases, and open decisions** found in the specification. Items are grouped by domain area and ranked:

- 🔴 **Blocker** — Must be resolved before implementation of that module
- 🟡 **Important** — Should be decided before or during implementation; defaulting is possible but risky
- 🟢 **Nice to clarify** — Can be deferred or defaulted safely

---

## 1. Authentication & Identity

### ✅ 1.1 — Authentication provider not specified — RESOLVED
> **Decision:** ASP.NET Core Identity with local accounts (Option A). Email confirmation deferred for MVP (`RequireConfirmedAccount = false`). External OAuth providers can be added later as an additive change.
>
> The `User` entity will be a custom `ApplicationUser` subclass of `IdentityUser`, stored in the same SQLite database via EF Core. Cookie-based authentication. The Blazor Web App template with `-au Individual` provides scaffolded register/login/logout/password-reset pages.

### ✅ 1.2 — User registration / onboarding flow — RESOLVED
> **Decision:** Admin-managed registration only. No public self-registration.
>
> **Normal operation:**
> 1. Admin creates a user account under their organization(s)
> 2. System sends a welcome email prompting the user to set their password
> 3. User clicks link, creates password, and can then log in
>
> **Email infrastructure:** Include `IEmailSender` implementation from the start (SMTP-based). Will be used for welcome emails and password reset. Not deferred — built into the initial project, even if the first real use comes during test-user onboarding.
>
> **Development phase:**
> - A seeded admin user with known credentials (e.g., `admin@arelia.dev` / `Admin123!`)
> - The login screen is shown with email and password **pre-populated** — dev just clicks **Sign In**
> - No auto-authentication bypass; the real login flow is exercised every time
>
> **Implications:**
> - The scaffolded `/register` page should be **removed or restricted** (not publicly accessible)
> - The `InviteUserToOrganization` command becomes the primary onboarding entry point
> - Organization creation remains a system-level/seed operation (not user-initiated) for now

### ✅ 1.3 — "Last selected tenant" storage — RESOLVED
> **Decision:** Browser local storage. Simple, no database field needed. Not cross-device, but acceptable — a choir member typically uses one device.

### ✅ 1.4 — FormerMember "contact choir" capability — RESOLVED
> **Decision:** Just visibility of the org's contact info (email, phone) on a page. No contact form, no in-app messaging. FormerMembers can see the info and reach out on their own.

### ✅ 1.5 + 1.6 — User deactivation model — RESOLVED
> **Decision:** Two-tier deactivation:
>
> | Level | Mechanism | Effect |
> |---|---|---|
> | **Demotion to FormerMember** | `OrganizationUser.IsActive = false` | User loses member access in that org. Sees only `PublicVisible` activities + org contact info. Can still access other orgs normally. |
> | **Banned from system** | `ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue` (ASP.NET Identity lockout) | User cannot log in at all. Locked out of every org. Used for abuse/policy violations. |
>
> **Normal case:** Per-org demotion (`OrganizationUser.IsActive = false`). The user's account remains active and they can still access other organizations they belong to.
>
> **When a Person is deactivated:** If the linked `OrganizationUser` is still active, the org-level membership should also be set to inactive (FormerMember). This is an Admin action — not automatic, but the UI should warn: _"This person has an active user account. Demote to FormerMember?"_

---

## 2. Domain Roles vs. Application Roles

### ✅ 2.1 — Relationship between domain roles and app roles — RESOLVED
> **Decision:** Unified additive role system. The separate "app role" field on `OrganizationUser` is eliminated. There is **one role system** using `RoleAssignment`.
>
> **Core concept:**
> - **Member** = implicit baseline. If `OrganizationUser.IsActive = true`, you're a member with read access.
> - **Additional roles** = assigned via `RoleAssignment` (with FromDate/ToDate). Each grants specific permissions additively.
> - **FormerMember** = not a role. It's the state when `OrganizationUser.IsActive = false` (restricted visibility only).
>
> **System roles** (seeded per org, cannot be deleted):
>
> | Role | Unlocks |
> |---|---|
> | **Board** | Manage people, activities, attendance, RSVP on behalf of others, view membership & attendance reports |
> | **Treasurer** | Manage charges, payments, expenses, credit, view finance reports, CSV export |
> | **Conductor** | View & manage attendance, set rehearsal expectations |
> | **Admin** | Everything above + org settings, user management, backups |
>
> **Permission mapping:**
>
> | Area | Member (all) | Board | Treasurer | Conductor | Admin |
> |---|---|---|---|---|---|
> | View calendar & published activities | ✓ | ✓ | ✓ | ✓ | ✓ |
> | RSVP for self | ✓ | ✓ | ✓ | ✓ | ✓ |
> | View own finances | ✓ | ✓ | ✓ | ✓ | ✓ |
> | Select optional top-up (own charge) | ✓ | ✓ | ✓ | ✓ | ✓ |
> | Manage people | | ✓ | | | ✓ |
> | Manage activities | | ✓ | | | ✓ |
> | Manage attendance | | ✓ | | ✓ | ✓ |
> | RSVP on behalf of others | | ✓ | | | ✓ |
> | Manage charges & payments | | | ✓ | | ✓ |
> | Manage expenses & credit | | | ✓ | | ✓ |
> | View attendance reports | | ✓ | | ✓ | ✓ |
> | View finance reports | | | ✓ | | ✓ |
> | View membership reports | | ✓ | ✓ | | ✓ |
> | Org settings & config | | | | | ✓ |
> | User management | | | | | ✓ |
> | Backups | | | | | ✓ |
>
> Permissions are **additive** — a person with Board + Treasurer gets the union of both.
>
> **Custom roles** (org-created, informational only):
> - Orgs can create roles like *Section Leader*, *Librarian*, *Social Committee Chair*
> - These appear in profile and role history but have **no system permissions**
> - Tracked with the same `RoleAssignment` (FromDate/ToDate)
>
> **Data model changes from spec:**
> 1. `OrganizationUser` — remove `ApplicationRole` field. Active = Member. Inactive = FormerMember.
> 2. `Role` — add `RoleType` (System/Custom). System roles have permissions, custom roles are informational.
> 3. `RoleAssignment` — unchanged (PersonId, RoleId, FromDate, ToDate).
>
> **Authorization flow:**
> 1. Is `OrganizationUser.IsActive`? No → FormerMember (PublicVisible activities only). Yes → continue.
> 2. Get active `RoleAssignment`s (today within FromDate–ToDate)
> 3. Effective permissions = Member baseline ∪ permissions from each active role
> 4. Check required permission against the set
>
> Maps to ASP.NET Core authorization policies — one policy per permission area.
>
> **Management UI required (Admin):**
>
> 1. **Role → Permission management**
>    - View/edit which permissions each system role grants
>    - Admin can adjust the default permission mapping per org (e.g., give Conductor the ability to manage activities too)
>    - Custom roles remain informational (no permissions), but Admin could optionally promote one to permission-bearing
>    - UI: table/grid of roles × permissions with toggle switches
>
> 2. **Role assignment management**
>    - View all role assignments across people (who has what role, active/historical)
>    - Assign/remove roles to/from a person with FromDate/ToDate
>    - Quick view: "people with Board role", "people with Admin role"
>    - Accessible from both the person detail page and a dedicated role management page
>
> 3. **Role definition management**
>    - Create/rename/deactivate custom roles
>    - System roles: rename allowed, delete not allowed
>
> **Data model implication:** The role → permission mapping needs to be stored (not just hardcoded). Options:
> - A `RolePermission` join table: `RoleId` + `Permission` (enum/string)
> - Seeded with defaults, editable by Admin per org
> - This allows the permission table above to be the **default**, while each org can customize

### ✅ 2.2 — Are domain roles predefined or fully custom? — RESOLVED
> **Decision:** Both. System roles (Board, Treasurer, Conductor, Admin) are seeded per org and cannot be deleted. Orgs can additionally create custom informational roles. Distinction is via `Role.RoleType` (System vs Custom).

### ✅ 2.3 — Can a person hold the same domain role twice simultaneously? — RESOLVED
> **Decision:** Covered by §2.1. Permissions are computed from "does an active assignment exist?" — overlapping periods for the same role are redundant but harmless (no double permissions). No validation rule needed. The UI may show a warning if a duplicate is detected, but it won't block it.

---

## 3. People Management

### ✅ 3.1 — Person name structure — RESOLVED
> **Decision:** Split into `FirstName` + `LastName`. Display as `"FirstName LastName"`, sort by `LastName`. Search matches against both fields.

### ✅ 3.2 — Person fields — what else? — RESOLVED
> **Decision:** Keep the default set from the spec: `FirstName`, `LastName` (§3.1), `Email`, `Phone`, `IsActive`, plus `VoiceGroup` (§3.3). No additional fields (address, date of birth, emergency contact) for MVP. Add a `Notes` (string?, nullable) field for free-form comments. Additional fields can be added later as needed.

### ✅ 3.3 — Voice group / section tracking — RESOLVED
> **Decision:** Dedicated nullable `VoiceGroup` field on `Person` (enum: `Soprano`, `Alto`, `Tenor`, `Bass`, or `null`). Not a role — it's a property of the person, not a responsibility. One value at a time; simple to query, filter, and report on. Can be extended later (e.g., Soprano1/Soprano2) by adding enum values.

### ✅ 3.4 — New member joins mid-semester — RESOLVED
> **Decision:** All active members are automatically participants in all rehearsals. No explicit `ActivityParticipant` record needed for rehearsals — active membership (`OrganizationUser.IsActive = true`) implies participation.
>
> Members announce **absence** (opt-out) rather than being **added** (opt-in). A new member joining mid-semester is automatically part of all future rehearsals with no manual action.
>
> **Implication for the data model:** Rehearsal attendance works as an absence/exception model:
> - No need to generate `ActivityParticipant` records for every member × every rehearsal
> - RSVP for rehearsals = announcing absence (RSVP No) or confirming presence (RSVP Yes, which is the default assumption)
> - Attendance tracking records who was actually present/absent, with "present" as the assumed default
>
> **Note:** This applies to **rehearsals** specifically. Other activity types (concerts, trips, events) may still use explicit participant lists with opt-in signup, depending on the activity configuration.

### ✅ 3.5 — Member departure workflow — RESOLVED
> **Decision:**
> - Outstanding charges: **still owed** (financial obligation doesn't vanish on departure)
> - Credit balance: **frozen** (stays on record; can be refunded manually by Treasurer)
> - Future participations: **removed** from upcoming activities
> - Access: Admin sets `OrganizationUser.IsActive = false` → FormerMember (per §1.5)

---

## 4. Activities & Calendar

### ✅ 4.1 — Timezone handling — RESOLVED
> **Decision:** Use `DateTime` (no offset) for all activity times. The organization's timezone is stored on `Organization.Timezone` (already defined in §11.1 as `"Europe/Copenhagen"`). All times are interpreted in the org's timezone. Simpler than `DateTimeOffset` everywhere and sufficient since all members of a choir are local.

### ✅ 4.2 — Multi-day activities — RESOLVED
> **Decision:** Activities can span multiple days. Attendance works the same as single-day (one attendance record per person per activity, not per day).
>
> **Calendar display:** Multi-day activities appear as a spanning bar across the days they cover (similar to how Google Calendar or Outlook render multi-day events). On day/list views, they appear on the first day with a visual indicator showing the end date.

### ✅ 4.3 — Semester overlap — RESOLVED
> **Decision:** No overlap allowed. Semesters within the same organization must have non-overlapping date ranges. Validation rule enforced on create/edit.

### ✅ 4.4 — Activities without a parent semester — RESOLVED
> **Decision:** Activities can exist outside semesters. `ParentActivityId` is nullable — standalone activities (org-wide events, social gatherings, etc.) don't need a parent.
>
> **Work year concept:** All activities belong to a **work year** (integer, e.g., `2025`). This provides organizational grouping independent of semesters.
> - Determined by the activity's `StartDateTime` year
> - If an event crosses into the next calendar year (e.g., Dec 31 → Jan 1), it's registered under the year it **started** in
> - Semesters should not normally cross years
> - UI can filter/group activities by work year
>
> **Data model:** Add `WorkYear` (int, computed from `StartDateTime.Year`) on `Activity`. Could be a stored computed column or set on save.

### ✅ 4.5 — Nesting depth — RESOLVED
> **Decision:** One level only. Semester → Activity. No deeper nesting (no sub-events). If multiple things happen on the same day, register them as separate sibling activities under the semester (or as standalone activities).

### ✅ 4.6 — "My activities" filter definition — RESOLVED
> **Decision:** Two rules based on activity type:
> - **Rehearsals:** Every rehearsal while I am an active member (per §3.4 — implicit participation)
> - **Other activities:** Activities where my RSVP is Yes

### ✅ 4.7 — Calendar component choice — RESOLVED
> **Decision:** Two-view approach:
>
> **1. Agenda/list view (primary)** — built with MudBlazor components (no extra library). Chronological list of upcoming activities grouped by month, with RSVP buttons inline. This is the default member view.
>
> **2. Month overview (secondary)** — using **Radzen Blazor Scheduler** (`RadzenScheduler`, MIT license, free). Provides month/week/day views out of the box. One extra NuGet package, zero licensing cost. Used only for the calendar view — everything else stays MudBlazor.
>
> **Click-through:** Clicking any activity (in either view) navigates to an **activity detail page** showing:
> - Activity name, type, date/time, location, description
> - RSVP status + action buttons (for the current member)
> - Participant list / attendance summary (for Board/Conductor)
> - Capacity + waitlist info (if applicable)
>
> Multi-day activities render as spanning bars in the month view.

---

## 5. RSVP & Waiting List

### ✅ 5.1 — RSVP status change after confirmation: spot release — RESOLVED
> **Decision:** Automatic release + manual promotion.
>
> When a confirmed member changes RSVP from Yes → No (or Maybe):
> 1. `RsvpStatus` changes to No/Maybe
> 2. `SignupStatus` changes from `Confirmed` → `None` **automatically**
> 3. The freed spot is **not auto-filled** — Board/Admin sees an indicator (e.g., "1 spot available, 3 on waitlist") and manually promotes via `PromoteFromWaitlist`

### ✅ 5.2 — RSVP Yes → No → Yes again — RESOLVED
> **Decision:** Back of the waiting list. If a member gives up their confirmed spot (RSVP Yes → No) and later RSVPs Yes again when capacity is full, they go to the **back of the waiting list**. No spot reclaiming — they gave it up voluntarily.

### ✅ 5.3 — RSVP for non-participants — RESOLVED
> **Decision:** Depends on activity type:
> - **Rehearsals:** No concept of "non-participant" — all active members are implicit participants (per §3.4). RSVP = announcing absence.
> - **Other activities (concerts, trips, events):** RSVP-ing automatically creates the `ActivityParticipant` record if one doesn't exist. No need for a separate "join" step before RSVP-ing.

### ✅ 5.4 — SignupDeadline behavior — RESOLVED
> **Decision:** Members blocked after deadline, Board can override. Unanswered RSVPs stay `Unanswered` — they are not auto-resolved. Board needs visibility into who hasn't responded.

### ✅ 5.5 — Notifications — RESOLVED
> **Decision:** Covered by §12.1. Notifications are a core feature — both in-app and email. Waitlist promotions, RSVP reminders, and payment reminders are all included in the initial notification type set.

---

## 6. Rehearsal Generation

### ✅ 6.1 — Overlapping templates — RESOLVED
> **Decision:** Warn and block. If a new template would generate rehearsals on the same day/time as an existing template in the same semester, the UI warns and blocks creation. Prevents confusion from silently skipped duplicates.

### ✅ 6.2 — Template date range vs. semester date range — RESOLVED
> **Decision:** Hard-enforce. Template `StartDate`/`EndDate` must fall within the parent semester's date range. If the user needs rehearsals beyond the current range, they can edit the semester's end date first, then adjust the template.

### ✅ 6.3 — Regeneration after template edit — RESOLVED
> **Decision:** Changes to rehearsal times should normally be made as edits on individual rehearsal records — not by editing the template and regenerating.
>
> During generation, if any generated rehearsal would overlap with an existing rehearsal in the same semester (same date), the user is warned with a list of conflicts and given the option to cancel. No silent creation of duplicates.
>
> `RegenerateRehearsalsForSemester` is deferred — not needed for MVP given the individual-edit approach.

### ✅ 6.4 — `GenerateExclusions` structure — RESOLVED
> **Decision:** Deferred. Not needed for MVP. Rehearsals are generated on a recurring basis and individual rehearsals can be added or deleted manually. If a holiday falls on a rehearsal day, Board simply deletes that generated rehearsal. A formal exclusion system can be added later if the manual approach becomes tedious.

---

## 7. Finance — Charges & Payments

### ✅ 7.1 — How are discounts determined? — RESOLVED
> **Decision:** Manual discounts for MVP. `GenerateMembershipFeesForSemester` creates charges with a base fee line and an optional top-up line only. Discounts are added manually by Board/Treasurer per charge after generation via `AddChargeLine` with `LineType = Discount`.
>
> Reasoning: Discount rules vary too much (student, senior, family, hardship, multi-choir) to automate on first try. The `ChargeLine` model already supports negative discount lines — a Board member simply adds one.
>
> Later, if patterns emerge, a `DiscountRule` entity can be introduced to automate.

### ✅ 7.2 — Top-up amount configuration — RESOLVED
> **Decision:** Configurable per semester via generation parameters. When Board/Admin runs `GenerateMembershipFeesForSemester`, they provide:
>
> ```
> GenerateMembershipFeesForSemester(SemesterId, BaseFeeAmount, TopUpAmount?, DueDate)
> ```
>
> Each generated charge gets:
> 1. A `Base` line (selected by default) for the base amount
> 2. A `TopUp` line (optional, **not selected** by default) for the top-up amount
>
> Members can toggle their own top-up line via `SelectOptionalChargeLine`. No org-level or global config needed — amounts are provided at generation time, giving flexibility per semester.

### ✅ 7.3 — Charge editing after partial payment — RESOLVED
> **Decision:** Allow it. Charge status auto-recalculates when lines change. If the total decreases below the amount already paid, the charge shows as `Overpaid` and the UI warns the Treasurer. Overpayment can be resolved by adding credit to the member's ledger.

### ✅ 7.4 — Multiple currencies within one org — RESOLVED
> **Decision:** Single currency per org. All charges, payments, and expenses are recorded in the org's `DefaultCurrencyCode`. No exchange rate handling.
>
> For payments originating in a different currency (e.g., foreign bank transfer), add optional fields on `Payment`:
> - `OriginalAmount` (decimal?) — the amount in the source currency
> - `OriginalCurrencyCode` (string?) — the source currency code
>
> These are informational only — the authoritative amount is always in the org's currency. The per-record `CurrencyCode` is auto-populated from `Organization.DefaultCurrencyCode`.

### ✅ 7.5 — Payment without a person — RESOLVED
> **Decision:** `PayerPersonId` remains nullable. Payments can come from external sources (grants, sponsorships, donations, municipal funding). For payer-less payments, add a required `PayerDescription` field (string):
> - If `PayerPersonId` is set → `PayerDescription` is optional (auto-populated from person name)
> - If `PayerPersonId` is null → `PayerDescription` is required (e.g., "Municipality arts grant 2025", "Donation from local business")
>
> This keeps the door open for non-member payments while ensuring every payment has a traceable origin.

### ✅ 7.6 — Credit balance: ledger vs. running total — RESOLVED
> **Decision:** Ledger entries. A `CreditTransaction` entity records each addition/deduction with amount, reason, timestamp, and linked entity (e.g., PaymentId, ChargeId). `CreditBalance.BalanceAmount` is a denormalized running total, recomputable from the ledger. Financial data requires an audit trail.

### ✅ 7.7 — Payment method tracking — RESOLVED
> **Decision:** Add two optional string fields on `Payment`:
> - `PaymentMethod` (string?) — e.g., "Bank transfer", "MobilePay", "Cash", "Card"
> - `Reference` (string?) — e.g., bank transaction ID, MobilePay reference, receipt number
>
> Both are free-text, optional, and useful for reconciliation.

### ✅ 7.8 — Outstanding charges when member leaves — RESOLVED
> **Decision:** Answered by §3.5 — charges are **still owed**. Credit balance is **frozen**.

---

## 8. Finance — Expenses

### ✅ 8.1 — Expense category: string or enum? — RESOLVED
> **Decision:** String field with an `ExpenseCategory` lookup table per org. Seeded with defaults, user-extensible.
>
> **UI behavior:**
> - Combobox/autocomplete: user can select from existing categories or type a new one
> - New categories are created on-the-fly from the same input field
> - No separate "manage categories" page needed (though Admin could have one later)
>
> **Normalization rules (enforced on save):**
> - Stored as **ALL CAPS** (case-insensitive matching)
> - Leading and trailing whitespace **stripped**
> - Multiple consecutive spaces **collapsed to single space**
> - Example: `"  sheet   music  "` → `"SHEET MUSIC"`
>
> **Default seeded categories** (per org):
> `SHEET MUSIC`, `VENUE RENTAL`, `INSTRUMENT MAINTENANCE`, `TRAVEL`, `REFRESHMENTS`, `MARKETING`, `INSURANCE`, `OTHER`

### ✅ 8.2 — Receipt/document attachment — RESOLVED
> **Decision:** Include simple document attachment on expenses. One or more files (images, PDFs) can be attached to an `Expense` record.
>
> **Implementation:** Store files on disk (configurable path, Docker volume-friendly). An `ExpenseAttachment` entity with `ExpenseId`, `FileName`, `ContentType`, `FilePath`, `UploadedAt`. Keep it simple — no preview, no versioning. Just upload and download.

---

## 9. Reporting & Export

### ✅ 9.1 — Report access control — RESOLVED
> **Decision:** Answered by the permission table in §2.1:
> - Finance reports → Treasurer + Admin
> - Attendance reports → Board + Conductor + Admin
> - Membership reports → Board + Treasurer + Admin
> - Members see only their own data (own finances, own attendance history)

### ✅ 9.2 — "Overdue" definition — RESOLVED
> **Decision:** Simple rule: `DueDate < today && Status != Paid`. No grace period.

### ✅ 9.3 — CSV export details — RESOLVED
> **Decision:** Use generally accepted defaults:
> - **Encoding:** UTF-8 with BOM (for Excel compatibility)
> - **Date format:** ISO 8601 (`yyyy-MM-dd`)
> - **Delimiter:** Semicolon (`;`) — standard for European locales where comma is the decimal separator
> - **Columns:** All visible columns from the corresponding report/table, with a header row
> - **Decimal separator:** Comma (`,`) — matches DKK locale
>
> No specific bank import format targeted. Can be adjusted later if needed.

---

## 10. Backups

### ✅ 10.1 — Backup scope — RESOLVED
> **Decision:** Full-database backup is acceptable. SQLite = one file for all tenants. Backups are the entire database. Fine for a self-hosted system with <10 orgs.

### ✅ 10.2 — Automated backups — RESOLVED
> **Decision:** Manual (Admin-triggered) backups only for MVP. No automated scheduling. Admin clicks a button, system copies the SQLite file to the backup directory with a timestamp. Automated backups (cron/scheduled task) can be added later.

### ✅ 10.3 — Backup retention — RESOLVED
> **Decision:** Keep last 30 backups. When a new backup is created and the count exceeds 30, the oldest backup is deleted. Simple FIFO. `PruneBackups` runs automatically after each backup creation.

---

## 11. Data Model Gaps

### ✅ 11.1 — Organization settings entity missing — RESOLVED
> **Decision:** Add settings directly as columns on `Organization`. No separate entity needed — the setting count is small and well-defined.
>
> Additional fields on `Organization`:
>
> | Field | Type | Default |
> |---|---|---|
> | `DefaultCurrencyCode` | string | `"DKK"` |
> | `DefaultPublicVisible` | bool | `false` |
> | `DefaultRehearsalDay` | DayOfWeek? | `Thursday` |
> | `DefaultRehearsalStartTime` | TimeOnly? | `19:00` |
> | `DefaultRehearsalDurationMinutes` | int? | `150` |
> | `DefaultRehearsalLocation` | string? | `"Stavangergade 10"` |
> | `Timezone` | string | `"Europe/Copenhagen"` |
>
> If the settings list grows significantly later, extract to a separate entity.

### ✅ 11.2 — User entity undefined — RESOLVED
> **Decision:** `ApplicationUser` extends `IdentityUser` (see §1.1). Authorization is driven by `OrganizationUser.IsActive` + active `RoleAssignment`s (see §2.1). No separate `ApplicationRole` field needed on `OrganizationUser`.

### ✅ 11.3 — Audit fields missing from all entities — RESOLVED
> **Decision:** Yes, add globally via a base entity or EF Core `SaveChanges` interceptor. All entities get:
> - `CreatedAt` (DateTime, set on insert)
> - `CreatedBy` (string/UserId, set on insert)
> - `UpdatedAt` (DateTime?, set on update)
> - `UpdatedBy` (string/UserId?, set on update)
>
> Populated automatically — no manual setting needed in business logic.

### ✅ 11.4 — Soft delete strategy — RESOLVED
> **Decision:** Default to soft delete (`IsActive = false`) for all entities. Commands named `DeleteX` perform deactivation, not hard deletion. Data is preserved for audit and reporting. The naming stays as `Delete` in the UI/API — the implementation is soft.

### ✅ 11.5 — Concurrency control — RESOLVED
> **Decision:** Add `RowVersion` (concurrency token) to all entities via the base entity. EF Core handles optimistic concurrency automatically — if two users edit the same record, the second save gets a `DbUpdateConcurrencyException` which the UI handles with a conflict message.

### ✅ 11.6 — Currency entity scope — RESOLVED
> **Decision:** No separate `Currency` entity. Just the `DefaultCurrencyCode` string on `Organization` (already in §11.1). Currency codes are well-known ISO 4217 strings — no need to maintain a table for them. If multi-currency is ever needed, a `Currency` entity can be added then.

---

## 12. Cross-Cutting Concerns Not Addressed

### ✅ 12.1 — No notification/email system — RESOLVED
> **Decision:** Notifications are a core feature, not deferred. Both **in-app** and **email** channels.
>
> **Architecture:**
> - `INotificationService` interface with two implementations: in-app (database-backed) + email (via existing `IEmailSender`)
> - `Notification` entity: `RecipientUserId`, `Type`, `Title`, `Message`, `IsRead`, `CreatedAt`, `LinkUrl?`
> - Notifications are created by domain events (e.g., RSVP change, waitlist promotion, payment recorded)
> - Email is sent in addition to in-app for important events (configurable per notification type)
>
> **Notification types (initial set):**
> - Waitlist promotion ("You've been confirmed for X")
> - RSVP reminder (before deadline)
> - Payment reminder (charge overdue)
> - Schedule change (activity time/date/location changed)
> - New charge created
> - Welcome / password setup
>
> **UI:** Bell icon with unread count in the app header. Notification dropdown/panel with mark-as-read.

### ✅ 12.2 — No audit logging — RESOLVED
> **Decision:** Include an audit log. An `AuditLogEntry` entity records sensitive operations:
> - `Timestamp`, `UserId`, `OrganizationId`
> - `Action` (string — e.g., "PaymentCreated", "RoleAssigned", "ChargeDeleted")
> - `EntityType`, `EntityId` — what was affected
> - `Details` (JSON string) — before/after snapshot or relevant context
>
> Populated automatically via EF Core interceptor or domain events. Viewable by Admin in a read-only audit log page. Complements the per-entity audit fields (§11.3) with a centralized, searchable history.

### ✅ 12.3 — No data import capability — RESOLVED
> **Decision:** Deferred. No CSV/spreadsheet import for MVP. Data is entered manually or via seed data. Can be added later as needed.

### ✅ 12.4 — Localization / language — RESOLVED
> **Decision:** Multi-language (i18n). Use ASP.NET Core's built-in localization (`IStringLocalizer`). Start with **English** as the default language. **Danish** as the second language. Additional languages can be added by providing resource files.
>
> User selects their preferred language in their profile or browser settings. Org-level default language on `Organization` (optional, for seeded data/emails).

### ✅ 12.5 — Mobile responsiveness — RESOLVED
> **Decision:** The app should be responsive and usable on mobile devices. MudBlazor provides responsive layouts out of the box. No specific mobile-first requirements, but key member flows (RSVP, view calendar, view own finances) must work well on phone screens. No native mobile app.

### ✅ 12.6 — Expected scale — RESOLVED
> **Decision:**
> - Members per org: **< 60**
> - Total orgs: **< 10**
> - Rehearsals per org per year: **< 100**
>
> At this scale, SQLite is more than sufficient. No pagination needed for member lists. Simple queries will perform well. If responsiveness becomes an issue at larger scale in the future, the system can be migrated to a different database (PostgreSQL, SQL Server) — EF Core makes this straightforward.

---

## 13. Workflow Gaps

### ✅ 13.1 — End-of-semester process — RESOLVED
> **Decision:** No formal close process for MVP. Semesters have a date range — once it passes, the semester is historical. Reports (attendance, finance) can be run for any semester at any time. No data freezing or archiving. `CloseSemester` remains deferred.

### ✅ 13.2 — Year-end financial reconciliation — RESOLVED
> **Decision:** Required. A year-end reconciliation process that includes:
> - **Annual financial summary report** — total income (charges paid), total expenses, net balance, grouped by category
> - **Open charges review** — list of unpaid/partially paid charges from the year, with option to write off or carry forward
> - **Credit balance carry-forward** — credit balances automatically carry into the new year (they're ledger-based, so this is inherent)
> - **Verification checklist** — UI page showing: all charges resolved? any unallocated payments? expense categories balanced?
>
> This is a **report + review workflow**, not a hard close. The Treasurer runs the summary, reviews open items, and takes action on anything outstanding. No data is frozen — corrections can still be made after year-end.

### ✅ 13.3 — Organization creation workflow — RESOLVED
> **Decision:** Any user with an **Admin** role in at least one org can create a new organization. Upon creation:
> 1. The creating user automatically becomes Admin for the new org (new `OrganizationUser` + `RoleAssignment` with Admin role)
> 2. System roles (Board, Treasurer, Conductor, Admin) are seeded for the new org
> 3. Default expense categories are seeded
> 4. Default org settings are applied (currency, timezone, etc.)
>
> **Permissions do not transfer** — being Admin in Org A does not grant any access to Org B. Each org's roles are independent.
>
> For the initial system bootstrap, the seeded admin user (§1.2) creates the first organization.

---

## Summary: Top Priority Decisions Needed

| # | Topic | Priority | Section |
|---|-------|----------|---------|
| ~~1~~ | ~~Authentication provider choice~~ | ✅ | §1.1 |
| ~~2~~ | ~~User registration / invitation flow~~ | ✅ | §1.2 |
| ~~3~~ | ~~Domain roles ↔ App roles relationship~~ | ✅ | §2.1 |
| ~~4~~ | ~~Organization settings entity~~ | ✅ | §11.1 |
| ~~5~~ | ~~Discount determination rules~~ | ✅ | §7.1 |
| ~~6~~ | ~~Top-up amount configuration~~ | ✅ | §7.2 |
| ~~7~~ | ~~RSVP Yes→No spot release behavior~~ | ✅ | §5.1 |
| ~~8~~ | ~~Timezone handling decision~~ | ✅ | §4.1 |
| ~~9~~ | ~~Voice group / section tracking~~ | ✅ | §3.3 |
| ~~10~~ | ~~Credit ledger vs running total~~ | ✅ | §7.6 |
| ~~11~~ | ~~Audit fields on entities~~ | ✅ | §11.3 |
| ~~12~~ | ~~Notification system scope~~ | ✅ | §12.1 |
| ~~13~~ | ~~New member mid-semester workflow~~ | ✅ | §3.4 |
| ~~14~~ | ~~Member departure workflow~~ | ✅ | §3.5 |
| ~~15~~ | ~~Expense category type~~ | ✅ | §8.1 |

---

_This review is based solely on the contents of AGENTS.md. Items may already have been discussed or decided outside this document._
