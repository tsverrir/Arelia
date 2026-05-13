# Arelia — User Manual

This manual covers the main workflows in Arelia, illustrated with screenshots. Arelia is an invitation-only system — new users must be invited by an administrator before they can access the platform.

---

## Table of Contents

1. [Signing In](#1-signing-in)
2. [Registration is Invitation-Only](#2-registration-is-invitation-only)
3. [System Administrator — Managing Organisations](#3-system-administrator--managing-organisations)
4. [System Administrator — Managing Users](#4-system-administrator--managing-users)
5. [Organisation Admin — Managing People](#5-organisation-admin--managing-people)
6. [Organisation Admin — Inviting a New User by Email](#6-organisation-admin--inviting-a-new-user-by-email)
7. [Organisation Admin — Adding a Person Without a User Account](#7-organisation-admin--adding-a-person-without-a-user-account)
8. [Person Detail — Viewing Pending Invitation Status](#8-person-detail--viewing-pending-invitation-status)
9. [Person Detail — Resending an Invitation](#9-person-detail--resending-an-invitation)
10. [Accepting an Invitation — Setting Your Password](#10-accepting-an-invitation--setting-your-password)
11. [Account Already Active](#11-account-already-active)
12. [Invalid or Expired Invitation Link](#12-invalid-or-expired-invitation-link)
13. [Person Detail — Active User Account](#13-person-detail--active-user-account)
14. [Suspending a User](#14-suspending-a-user)
15. [Removing a Person from an Organisation](#15-removing-a-person-from-an-organisation)
16. [People List — After Operations](#16-people-list--after-operations)
17. [Person Without a Linked User Account](#17-person-without-a-linked-user-account)
18. [Role Management — Permission Matrix](#18-role-management--permission-matrix)
19. [Role Detail — Custom Role](#19-role-detail--custom-role)
20. [Role Detail — Admin Role (Read-Only)](#20-role-detail--admin-role-read-only)

---

## 1. Signing In

Navigate to the Arelia login page. Enter your email address and password and click **Log In**.

![Login page](screenshots/wf-01-login-page.png)

---

## 2. Registration is Invitation-Only

Attempting to navigate to the registration page directly will show an "invitation-only" message. New accounts can only be created by a System Administrator or Organisation Administrator sending an invitation email.

![Registration disabled](screenshots/wf-02-register-disabled.png)

---

## 3. System Administrator — Managing Organisations

System Administrators have access to the **System Admin** section in the navigation sidebar. The Organisations page lists all organisations across the system with their member count, status, and creation date.

Click **+ New Organisation** to create a new organisation.

![System Admin — Organisations list](screenshots/wf-03-system-organizations.png)

### Creating a New Organisation

Fill in the organisation name in the dialog and click **Create**.

![Create organisation dialog](screenshots/wf-04-create-organisation-dialog.png)

---

## 4. System Administrator — Managing Users

The **Users** page under System Admin lists all user accounts in the system. The **System Admin** column shows which accounts have elevated system-level privileges.

![System Admin — Users list](screenshots/wf-05-system-users.png)

> **Note:** The System Users page available in this earlier screenshot (`wf-05`) is from an earlier session. The updated System Admin Users list (post-implementation) is shown below:

![System Admin — Users list (updated)](screenshots/wf-30-system-users.png)

---

## 5. Organisation Admin — Managing People

The **People** list shows all members of the current organisation. Each person shows their name and status.

![People list](screenshots/wf-06-people-list.png)

---

## 6. Organisation Admin — Inviting a New User by Email

From the People list, click **+ Add Person**. In the dialog, fill in the person's details and provide their email address. When an email is provided, an invitation email will be sent to that address.

![Add person dialog (by email)](screenshots/wf-09-add-by-email-dialog.png)

After submitting, a confirmation appears and the invitation email is sent in the background.

![Invite sent confirmation](screenshots/wf-10-invite-sent.png)

---

## 7. Organisation Admin — Adding a Person Without a User Account

A person can be added to the organisation **without** a user account by leaving the email field blank in the Add Person dialog. This is useful for keeping attendance records for members who do not use the system directly.

![Add person dialog (no email)](screenshots/wf-07-add-person-dialog.png)

After saving, the person appears in the list without any associated user account or invitation status.

![Person added without email](screenshots/wf-08-add-person-no-email.png)

---

## 8. Person Detail — Viewing Pending Invitation Status

Opening the detail page of a person who has been invited but has not yet completed registration shows a **Pending** chip next to their name. The page also displays **Resend Invitation** and other user account action buttons.

![Person detail — Pending](screenshots/wf-11-person-detail-pending.png)

---

## 9. Person Detail — Resending an Invitation

Click **Resend Invitation** on a person's detail page to send a new invitation email. This generates a fresh 7-day token and delivers a new invite link to the person's email address.

![Resend invitation](screenshots/wf-12-resend-invitation.png)

---

## 10. Accepting an Invitation — Setting Your Password

When a user clicks the invitation link in their email, they are taken to the **Accept Invitation** page. They must set a password that meets the security requirements (minimum 8 characters, including uppercase, number, and special character).

![Accept invitation page](screenshots/wf-13-accept-invitation-page.png)

After filling in the password fields and clicking **Set Password & Sign In**, the user is automatically signed in and redirected to the dashboard.

![Password form filled](screenshots/wf-14-accept-invitation-filled.png)

![Dashboard after successful invitation acceptance](screenshots/wf-15-after-accept-invitation.png)

---

## 11. Account Already Active

If a user has already completed registration and clicks an old invitation link, the page shows an **Account Already Active** message with a link to the login page.

![Account already active](screenshots/wf-16-account-already-active.png)

---

## 12. Invalid or Expired Invitation Link

If the invitation link has expired (links are valid for 7 days) or is otherwise invalid, the page shows an **Invalid or Expired Link** message. The user can enter their email address to request a new invitation link, or contact their organisation administrator.

![Invalid or expired link](screenshots/wf-17-invalid-invite-link.png)

---

## 13. Person Detail — Active User Account

Once a user has completed registration, their person detail page shows a **Has linked user account** indicator with an **Active** badge. The page provides two management actions:

- **Suspend User** — Ends all active role assignments. The person record is preserved; the user can still log in but has no active roles.
- **Remove from Org** — Permanently removes the person's organisation membership. The person record and role history are preserved.

![Person detail — active account](screenshots/wf-18-person-detail-active.png)

---

## 14. Suspending a User

Click **Suspend User** on the person detail page. A confirmation dialog appears explaining the effect of the suspension.

![Suspend user confirmation dialog](screenshots/wf-19-suspend-user-dialog.png)

After confirming, all active role assignments are ended. The person remains in the organisation but no longer has any active roles.

![After suspend](screenshots/wf-20-after-suspend.png)

---

## 15. Removing a Person from an Organisation

Click **Remove from Org** to remove a person's organisation membership entirely. A confirmation dialog with a destructive warning appears.

![Remove from org confirmation dialog](screenshots/wf-21-remove-from-org-dialog.png)

After confirming:
- The person's `OrganizationUser` record is deleted
- The **Person** record is **preserved** (for historical attendance and financial records)
- Role assignment history is preserved with an end date
- The **Invite to System** button reappears (the person can be re-invited in the future)

![After remove from org](screenshots/wf-22-after-remove-from-org.png)

---

## 16. People List — After Operations

The People list continues to show persons after they have been removed from the organisation, because the person record is never deleted. This preserves historical data.

![People list after operations](screenshots/wf-23-people-list-after-ops.png)

---

## 17. Person Without a Linked User Account

A person added without an email address has no user account and shows only the basic profile fields and role assignments. No invite, suspend, or remove user buttons are shown because there is no associated user account.

![Person without email](screenshots/wf-24-person-no-email-detail.png)

---

## 18. Role Management — Permission Matrix

The **Roles and Permissions** page shows all roles for the current organisation in a permission matrix.

- **Admin** (red) — always has full access; permissions cannot be edited
- **Board** (orange) — system role; permissions are editable
- **Member** (blue) — system role; permissions are editable
- **Custom roles** (e.g. Treasurer, Conductor) — fully managed by the organisation

![Role management — permission matrix](screenshots/wf-25-role-management.png)

The bottom of the page shows all roles with their type badge and the number of active assignments.

![Role management — all roles list](screenshots/wf-26-role-management-bottom.png)

---

## 19. Role Detail — Custom Role

Clicking a custom role opens its detail page. You can:
- **Rename** the role using the Role Name field
- **Edit permissions** using the checkboxes
- **Delete** the role (only if it has no active assignments)

The right panel shows who currently has this role assigned.

![Role detail — Treasurer (custom)](screenshots/wf-27-role-detail-custom.png)

---

## 20. Role Detail — Admin Role (Read-Only)

> ⚠️ **Known issue (ISSUE-010):** The Admin role detail page currently shows editable fields and a Delete button. These should be disabled for system roles. This will be fixed in a future update. Do **not** rename or delete the Admin, Board, or Member roles.

![Role detail — Admin](screenshots/wf-28-role-detail-admin.png)

---

## System Admin Reference

### System Admin — Organisations

The system-level Organisations page (`/system/organizations`) is accessible only to System Administrators. It lists all organisations across the entire system and provides the ability to create new organisations.

![System Admin — Organisations](screenshots/wf-29-system-orgs.png)

### System Admin — Users

The system-level Users page (`/system/users`) lists all user accounts across all organisations. The **System Admin** column indicates which users have system-level privileges.

![System Admin — Users](screenshots/wf-30-system-users.png)

---

## Quick Reference — User States

| State | Description | Actions available |
|---|---|---|
| **No user account** | Person exists, no email or no invitation sent | Invite to System (if email present) |
| **Pending** | Invitation sent, user has not set password yet | Resend Invitation, Suspend, Remove from Org |
| **Active** | User has set password and is a member | Suspend, Remove from Org |
| **Suspended** | All role assignments ended; user can log in but has no active roles | Remove from Org |
| **Removed** | OrganizationUser deleted; person record preserved | Re-invite (if email present) |

---

## Quick Reference — Role Types

| Type | Editable | Deletable | Description |
|---|---|---|---|
| **Admin** | No | No | Full access to all features; auto-assigned to org administrators |
| **Board** | Permissions only | No | System-seeded board member role |
| **Member** | Permissions only | No | System-seeded general member role |
| **Custom** | Yes | Yes (if no active assignments) | Organisation-defined roles (e.g. Treasurer, Conductor) |
