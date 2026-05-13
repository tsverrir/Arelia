# Arelia

A multi-tenant web application for managing choirs and small organizations. Each tenant is an Organization whose members use the system to track people, activities, attendance, and finances.

## Language

### Identity & Access

**User**:
An authentication account (ASP.NET Core Identity). Global — not scoped to any organization.
_Avoid_: Account, login, principal

**System Administrator**:
A User holding the `"SystemAdmin"` ASP.NET Core Identity role. Operates at the platform level — creates and edits Organizations, manages user memberships across all orgs, and promotes/demotes other System Administrators. Has no access to org-internal data (people, activities, finance). Seeded from environment variables on first startup.
_Avoid_: Super admin, global admin, platform admin

**Org Admin**:
A User with the `Admin` System Role in a specific Organization. Manages that org's settings, users, and roles. Distinct from a System Administrator — their authority stops at the org boundary.
_Avoid_: Administrator (ambiguous between org and system level)

**Direct Assignment**:
A System Administrator action that silently adds an existing User to an Organization without sending an email. Always creates a minimal Person record (using the User's name from their account) and assigns the `Member` role by default. Distinct from an **Invitation**, which triggers email and is available to Org Admins.
_Avoid_: Silent invite, force-add

**Person**:
An individual tracked within a specific organization. Org-scoped. Can exist without a linked User (e.g. a member who does not use the system).
_Avoid_: Member (overloaded — see Role), contact

**OrganizationUser**:
The join between a User and an Organization. Every OrganizationUser must reference exactly one Person in that org — there is no User-in-org without a Person record. Created when a User is invited or directly assigned to an org.
_Avoid_: Membership (overloaded with dues/finance)

**Invitation**:
The act of adding a User (new or existing) to an Organization, triggered by a System Administrator or a user with `ManagePeople`. Two paths exist: **Person-first** (select an existing Person in the org, enter their email — the system detects whether a User account already exists) and **Email-first** (enter a new email — system creates both a Pending Account and a new Person). In both paths, a single initial role is selected (default: `Member`). New users receive a "set your password" email; existing users receive a notification. No confirmation from the recipient is required.
_Avoid_: Registration, signup, self-registration

**Pending Account**:
A User account created by an Invitation whose password has not yet been set (null password hash, `EmailConfirmed = false`). A Pending Account can only be activated by following the invite link or requesting a resend. Pending Accounts cannot log in.
_Avoid_: Unconfirmed account, inactive user

**Suspension**:
The act of ending all of a Person's active RoleAssignments with `ToDate = today`, leaving their OrganizationUser intact. A suspended Person has no Active Roles and therefore no access to the org. Reinstatement is done by assigning new roles through the normal role assignment UI — there is no dedicated "reinstate" action.
_Avoid_: Deactivation, ban, freeze

**Removal**:
The act of deleting a User's OrganizationUser link from an org, ending all their active RoleAssignments. The Person record and all associated history (attendance, charges, role history) are preserved. The Person becomes unlinked — visible in the People list without a User account — and can be re-invited later.
_Avoid_: Delete user, kick, remove member


The act of an invited user setting their password for the first time via the invite link, activating their Pending Account. Completed on a dedicated page (`/Account/AcceptInvitation`), separate from the password-reset flow. After completion the user is signed in automatically.
_Avoid_: Account activation, email confirmation, registration

### Roles & Permissions

**Role**:
A named set of permissions within an organization. Every Role is either a System Role or a Custom Role, identified by a `RoleType` field.
_Avoid_: Group, privilege level

**System Role**:
A role with a fixed `RoleType` value (`Admin`, `Board`, or `Member`). Seeded automatically for every new organization. `Admin` has hard-coded full access. `Board` and `Member` come with editable default permissions.
_Avoid_: Built-in role, default role

**Custom Role**:
An org-defined role with `RoleType = Custom`. Permissions are freely assigned by org admins.
_Avoid_: User-defined role, extra role

**RoleAssignment**:
A dated assignment of a Role to a Person (`FromDate`, optional `ToDate`). A Person can hold multiple simultaneous RoleAssignments.
_Avoid_: Role membership, permission grant

**Active Role**:
A RoleAssignment where `FromDate ≤ now` and (`ToDate` is null or `ToDate ≥ now`).

**Permission**:
A granular capability flag. Permissions are aggregated across all of a user's Active Roles when determining what they can do. `Admin` Role short-circuits this — granting full access regardless of individual Permission entries. `ManagePeople` covers person data, invitations, role assignment, and user removal. `UserManagement` has been removed from the enum.
_Avoid_: Right, privilege, claim

**Effective Permissions**:
The union of all `Permission` values from the Active Roles held by a Person in a given organization. An `Admin` Role short-circuits this — granting full access regardless of individual Permission entries.

## Relationships

- A **User** may be linked to zero or more **Organizations** via **OrganizationUser**
- An **OrganizationUser** references exactly one **Person** in that org (required, not optional)
- A **Person** belongs to exactly one **Organization**
- A **Person** may hold multiple simultaneous **RoleAssignments**
- A **Role** has zero or more **RolePermissions** mapping to **Permission** values
- A **User** can access an org if and only if they have at least one **Active Role** (via OrganizationUser → Person → RoleAssignment)
- **Effective Permissions** = union of all Permissions across all Active Roles (Admin bypasses this)
- A **System Administrator** has full access to all **Organizations** and their user memberships, but not their internal data
- A **System Administrator** may also hold **OrganizationUser** memberships and act as an **Org Admin** in specific orgs simultaneously

## Example dialogue

> **Dev:** "If a conductor leaves the choir, do we delete their user account?"
> **Domain expert:** "No — we end their **RoleAssignments** with a `ToDate`. Once they have no Active Roles, they can no longer access the organization. Their **Person** record and history are preserved."

> **Dev:** "Can someone be both an Admin and have a custom 'Conductor' role?"
> **Domain expert:** "Yes. All roles come from the same pool. A person holds one **RoleAssignment** per **Role**. The Admin role grants full access; the Conductor role might grant nothing extra — it's just an organizational label."

> **Dev:** "What's the difference between a User and a Person?"
> **Domain expert:** "A **User** is the login account — it's global. A **Person** is someone we track inside a specific org: their name, contact info, role history, attendance. You can have a **Person** with no **User** (a member who doesn't use the app), or a **User** linked to different **Persons** in different orgs."

## Flagged ambiguities

- "Member" was used to mean both the `Member` System Role and "a person who belongs to the organization" — resolved: **Member** as a bare noun refers to the System Role; a person belonging to an org is a **Person** with an **OrganizationUser** link.
- "FormerMember" from the original spec has been removed — when a person leaves, their RoleAssignments are ended. With no Active Roles they lose org access entirely. There is no retained-access state for former members.
- "Invite" vs "Create user" vs "Register" — resolved: the canonical terms are **Invitation** (admin-initiated, triggers email), **Registration Completion** (user-initiated, completes a Pending Account), and **Direct Assignment** (System Admin only, silent). There is no self-service registration.
- "Admin" is overloaded — resolved: **Org Admin** refers to the `Admin` System Role within an org; **System Administrator** refers to the platform-level Identity role. Never use bare "Admin" to refer to either without qualification in design discussions.
- "UserManagement permission" — resolved: removed. Inviting/removing users is covered by `ManagePeople`; role assignment is also covered by `ManagePeople`; org-level settings are covered by `OrgSettings`.
- `OrganizationUser.PersonId` was nullable in code — resolved: non-nullable. Every OrganizationUser must reference a Person. Removing a User from an org leaves the Person record intact (unlinked).
- "Deactivate user" was ambiguous — resolved as two distinct actions: **Suspension** (end all RoleAssignments, OrganizationUser kept) and **Removal** (delete OrganizationUser, Person kept).
