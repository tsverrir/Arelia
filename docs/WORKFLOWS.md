# Arelia — Workflow Reference

This document describes all significant user-facing workflows in the Arelia system. It is the authoritative reference for how features behave end-to-end. See [CONTEXT.md](../CONTEXT.md) for term definitions and [SPECIFICATION.md](SPECIFICATION.md) for the full feature specification.

---

## Contents

- [1. Login & Routing](#1-login--routing)
- [2. User Management](#2-user-management)
  - [2.1 Create a Person (no User account)](#21-create-a-person-no-user-account)
  - [2.2 Invite a User — Person-first](#22-invite-a-user--person-first)
  - [2.3 Invite a User — Email-first](#23-invite-a-user--email-first)
  - [2.4 Direct Assignment (System Admin only)](#24-direct-assignment-system-admin-only)
  - [2.5 Complete Registration (accept invitation)](#25-complete-registration-accept-invitation)
  - [2.6 Resend an expired invitation](#26-resend-an-expired-invitation)
  - [2.7 Suspend a User from an org](#27-suspend-a-user-from-an-org)
  - [2.8 Reinstate a suspended User](#28-reinstate-a-suspended-user)
  - [2.9 Remove a User from an org](#29-remove-a-user-from-an-org)
  - [2.10 Delete a Person](#210-delete-a-person)
- [3. Organization Management (System Admin)](#3-organization-management-system-admin)
  - [3.1 Create an Organization](#31-create-an-organization)
  - [3.2 Edit an Organization](#32-edit-an-organization)
  - [3.3 Promote / demote a System Administrator](#33-promote--demote-a-system-administrator)
- [4. Roles & Permissions](#4-roles--permissions)
  - [4.1 Assign a role to a Person](#41-assign-a-role-to-a-person)
  - [4.2 End a role assignment](#42-end-a-role-assignment)
  - [4.3 Create a Custom Role](#43-create-a-custom-role)
  - [4.4 Edit role permissions](#44-edit-role-permissions)
- [5. Activities & Rehearsals](#5-activities--rehearsals)
  - [5.1 Create a Semester and generate rehearsals](#51-create-a-semester-and-generate-rehearsals)
  - [5.2 Edit or cancel an individual rehearsal](#52-edit-or-cancel-an-individual-rehearsal)
- [6. RSVP & Waiting List](#6-rsvp--waiting-list)
  - [6.1 RSVP to an activity](#61-rsvp-to-an-activity)
  - [6.2 Promote from waiting list](#62-promote-from-waiting-list)
- [7. Finance](#7-finance)
  - [7.1 Generate membership fees for a semester](#71-generate-membership-fees-for-a-semester)
  - [7.2 Record a payment](#72-record-a-payment)
  - [7.3 Apply credit to a charge](#73-apply-credit-to-a-charge)

---

## 1. Login & Routing

**Actors:** Any User  
**Entry point:** `/Account/Login`

1. User enters email + password.
2. On success, the system checks the User's context:
   - **System Administrator** → route to `/system/` (if also an org member, offer a choice)
   - **One org membership** → auto-select that org, route to `/admin/` or org home
   - **Multiple memberships** → show the tenant selector
   - **No memberships** → show an "awaiting access" message
3. All subsequent data is scoped to the selected Organization.

> Pending Accounts (null password hash) cannot log in. They must complete Registration first.

---

## 2. User Management

### 2.1 Create a Person (no User account)

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** People list → "Add person"

1. Admin enters first name, last name, and optional fields (email, phone, voice group, notes).
2. If no email is provided: Person record created, `Member` role assigned, done.
3. If an email is provided but no invite is sent: Person record created with email stored; no User account created, no email sent.
4. Person appears in the People list without a linked User account.

> To link this Person to a User account later, use **2.2 Invite a User — Person-first**.

---

### 2.2 Invite a User — Person-first

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → "Invite to system"  
**Precondition:** The Person has no linked User account.

1. Admin opens the invite form on an existing Person's profile.
2. Enters the Person's email address and selects an initial role (default: `Member`).
3. System checks whether a User with that email already exists:

   **New email (no existing User):**
   - Create a Pending Account (null password hash, `EmailConfirmed = false`)
   - Generate a password-reset token (valid 7 days)
   - Create `OrganizationUser` linking the User to this Person
   - Assign `RoleAssignment` with the selected role
   - Send **invitation email** (includes org name, inviter name, "Set your password" link → `/Account/AcceptInvitation`)

   **Existing User:**
   - Create `OrganizationUser` linking the User to this Person
   - Assign `RoleAssignment` with the selected role
   - Send **notification email** (org name, "you've been added" — no action required)

---

### 2.3 Invite a User — Email-first

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** People list → "Invite person"

1. Admin enters email, first name, last name, and selects initial role (default: `Member`).
2. System checks whether a User with that email already exists:

   **New email:**
   - Create a Pending Account
   - Create a new Person record
   - Create `OrganizationUser` linking User ↔ Person
   - Assign `RoleAssignment`
   - Send **invitation email**

   **Existing User:**
   - Create a new Person record
   - Create `OrganizationUser` linking the existing User ↔ new Person
   - Assign `RoleAssignment`
   - Send **notification email**

---

### 2.4 Direct Assignment (System Admin only)

**Actors:** System Admin  
**Entry point:** `/system/` → Organization → "Add existing user"  
**Precondition:** The User already has an account on the platform.

1. System Admin searches for an existing User by email.
2. Selects the target Organization and initial role (default: `Member`).
3. System:
   - Creates a minimal Person record (first/last name sourced from the User's account)
   - Creates `OrganizationUser` linking User ↔ Person
   - Assigns `RoleAssignment` with the selected role
   - **No email is sent.**

---

### 2.5 Complete Registration (accept invitation)

**Actors:** Invited User (Pending Account)  
**Entry point:** Invitation email link → `/Account/AcceptInvitation?token=…`

1. User clicks the link in their invitation email.
2. If the token is **valid**:
   - User enters and confirms a password
   - Password saved, `EmailConfirmed = true`
   - User is signed in automatically and routed per workflow **1. Login & Routing**
3. If the token is **expired**: see **2.6 Resend an expired invitation**.

---

### 2.6 Resend an expired invitation

**Actors:** Invited User (self-service) or User with `ManagePeople` / System Admin  
**Precondition:** The account is still Pending (null password hash).

**Self-service (from expired link page):**
1. User lands on `/Account/AcceptInvitation` with an expired token — error message shown.
2. User clicks "Resend invitation."
3. System generates a new token and sends a fresh invitation email to the same address.

**Admin-initiated (from Person profile):**
1. Admin opens a Person's profile — a "Pending invitation" badge is shown.
2. Admin clicks "Resend invitation."
3. System generates a new token and sends a fresh invitation email.

> Resend is only possible while the account is still Pending. If the User has already set a password, no resend is available.

---

### 2.7 Suspend a User from an org

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → "Suspend"

1. Admin chooses "Suspend" — a confirmation prompt is shown.
2. All active `RoleAssignment` records for that Person are ended (`ToDate = today`).
3. The `OrganizationUser` link is preserved — the User's account remains but they have no Active Roles and therefore no access to the org.
4. The Person remains visible in the People list with a "Suspended" indicator (no active roles).

> To restore access, see **2.8 Reinstate a suspended User**.

---

### 2.8 Reinstate a suspended User

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → role assignment UI

1. Admin opens the suspended Person's profile.
2. Assigns one or more roles using the normal role assignment UI (e.g. `Board`, `Member`, Custom roles).
3. The new `RoleAssignment` records become active immediately.
4. The User can log in and access the org again.

> There is no dedicated "reinstate" button — reinstatement is simply adding new role assignments.

---

### 2.9 Remove a User from an org

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → "Remove from organization"

1. Admin chooses "Remove from organization" — a **destructive-action warning** is shown.
2. Admin confirms.
3. All active `RoleAssignment` records for that Person are ended.
4. The `OrganizationUser` record is deleted.
5. The `Person` record is **preserved** with full history intact (attendance, charges, role history).
6. The Person appears in the People list as unlinked (no User account).

> To re-invite the person later, use **2.2 Invite a User — Person-first**.

---

### 2.10 Delete a Person

**Actors:** Org Admin, System Admin  
**Entry point:** Person's profile → "Delete person"

1. Admin chooses "Delete person" — a **destructive-action warning** is shown (this action cannot be undone).
2. Admin confirms.
3. The `Person` record is permanently soft-deleted (`IsDeleted = true`).
4. All linked records (RoleAssignments, attendance, charges) are effectively hidden.

> This is distinct from **2.9 Remove a User** — deleting a Person is an explicit, irreversible administrative action. Removal only severs the login link.

---

## 3. Organization Management (System Admin)

### 3.1 Create an Organization

**Actors:** System Admin  
**Entry point:** `/system/organizations` → "New organization"

1. System Admin enters org name, contact email, timezone, default language, and optional defaults (rehearsal day, currency, etc.).
2. System creates the Organization.
3. System seeds three System Roles: `Admin`, `Board`, `Member` (with default permissions for Board and Member).

---

### 3.2 Edit an Organization

**Actors:** System Admin (all fields), Org Admin (org-level settings via `OrgSettings` permission)  
**Entry point:** `/system/organizations/{id}` or `/admin/settings`

1. Admin edits the desired fields.
2. Changes saved immediately.

---

### 3.3 Promote / demote a System Administrator

**Actors:** System Admin  
**Entry point:** `/system/users`

1. System Admin searches for a User by email.
2. Chooses "Promote to System Admin" or "Demote from System Admin."
3. System adds or removes the `"SystemAdmin"` ASP.NET Core Identity role.

> A System Admin cannot demote themselves.

---

## 4. Roles & Permissions

### 4.1 Assign a role to a Person

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → Roles tab → "Assign role"

1. Admin selects a Role from the org's role list.
2. Sets `FromDate` (defaults to today) and optional `ToDate`.
3. A `RoleAssignment` record is created.
4. The Person's Effective Permissions are updated immediately.

---

### 4.2 End a role assignment

**Actors:** User with `ManagePeople`, Org Admin, System Admin  
**Entry point:** Person's profile → Roles tab → active assignment → "End"

1. Admin sets `ToDate = today` (or a custom past/future date) on an active `RoleAssignment`.
2. Once `ToDate` has passed, the role is no longer Active — permissions are updated accordingly.

---

### 4.3 Create a Custom Role

**Actors:** Org Admin (with `OrgSettings`), System Admin  
**Entry point:** `/admin/roles` → "New role"

1. Admin enters a name for the Custom Role.
2. Role is created with `RoleType = Custom` and no permissions.
3. Admin assigns permissions via **4.4 Edit role permissions**.

---

### 4.4 Edit role permissions

**Actors:** Org Admin (with `OrgSettings`), System Admin  
**Entry point:** `/admin/roles/{id}`

1. Admin toggles permission flags on a `Board`, `Member`, or `Custom` role.
2. Changes apply immediately to all Persons holding an active RoleAssignment for that role.

> The `Admin` System Role's permissions are hard-coded and cannot be edited.

---

## 5. Activities & Rehearsals

### 5.1 Create a Semester and generate rehearsals

**Actors:** User with `ManageActivities`, Org Admin, System Admin  
**Entry point:** Activities → "New semester"

1. Admin creates a `Semester` Activity with start/end dates.
2. Admin defines one or more `RehearsalRecurrenceTemplate` entries (day of week, time, location, duration).
3. Admin clicks "Generate rehearsals."
4. System generates concrete `Rehearsal` Activity instances according to the templates, skipping excluded dates.
5. Admin manually edits or cancels individual rehearsals as needed.

---

### 5.2 Edit or cancel an individual rehearsal

**Actors:** User with `ManageActivities`, Org Admin, System Admin  
**Entry point:** Activities → select rehearsal

1. Admin edits fields (date, time, location, notes) or marks the rehearsal as cancelled.
2. Changes apply to that instance only — the recurrence template is unaffected.

---

## 6. RSVP & Waiting List

### 6.1 RSVP to an activity

**Actors:** Any org member with an Active Role  
**Entry point:** Activity page → RSVP

1. Member sets RSVP to `Yes`, `No`, or `Maybe`.
2. If `Yes` and capacity is not full: `SignupStatus = Confirmed`.
3. If `Yes` and capacity is full: `SignupStatus = Waitlisted`, position assigned.

---

### 6.2 Promote from waiting list

**Actors:** User with `ManageActivities`, Org Admin  
**Entry point:** Activity page → Waiting list

1. A confirmed spot becomes available (cancellation or capacity increase).
2. Admin selects a waitlisted participant and promotes them.
3. `SignupStatus` → `Confirmed`, waiting list positions updated.

---

## 7. Finance

### 7.1 Generate membership fees for a semester

**Actors:** User with `ManageCharges`, Org Admin  
**Entry point:** Finance → "Generate charges"

1. Admin selects a semester.
2. System creates a `Charge` per active member, including:
   - Base fee `ChargeLine`
   - Optional top-up `ChargeLine` (unselected by default)
   - Discount `ChargeLine` (if applicable)
3. Admin reviews and confirms.

---

### 7.2 Record a payment

**Actors:** User with `ManageCharges`, Org Admin  
**Entry point:** Finance → Person → "Record payment"

1. Admin enters payment amount, date, and method.
2. Payment is linked to outstanding Charges.
3. If payment exceeds the outstanding balance: excess is added to the Person's `CreditBalance`.

---

### 7.3 Apply credit to a charge

**Actors:** User with `ManageCharges`, Org Admin  
**Entry point:** Finance → Person → Charges

1. Admin selects a Charge and chooses "Apply credit."
2. System deducts from `CreditBalance` up to the outstanding amount.
3. Charge is marked partially or fully paid.
