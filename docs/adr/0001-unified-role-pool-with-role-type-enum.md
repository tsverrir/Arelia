# Unified role pool with a RoleType enum for system roles

The system needs to enforce both hard-coded access control (Admin always has full access) and flexible org-configurable roles (custom roles, editable Board/Member defaults). We chose a single unified role pool — all roles, fixed and custom, live in the same `Role` entity — distinguished by a `RoleType` enum (`Admin`, `Board`, `Member`, `Custom`).

## Considered options

- **Fixed application roles only** — Admin, Board, Member hard-coded as an enum on OrganizationUser. Simple, but doesn't support org-defined roles or role history tracking.
- **Fully flexible permission roles** — all roles org-defined, no concept of "system roles". Would require every new org to configure access control from scratch, with no safety guarantees.
- **Hybrid with separate tables** — a separate AppRole assignment for access control alongside the domain role system. Two mechanisms to maintain, confusing mental model.

## Consequences

- `Role` gains a `RoleType` field. System roles (`Admin`, `Board`, `Member`) are seeded per org on creation.
- `Admin` role: permissions are hard-coded in the authorization layer; `RolePermission` entries are not used.
- `Board` and `Member` roles: shipped with default `RolePermission` entries that org admins may edit.
- `Custom` roles: no default permissions; org admins define them freely.
- A person can hold multiple simultaneous `RoleAssignment`s across both system and custom roles.
- `OrganizationUser.IsActive` is removed; access is determined solely by whether the person has at least one active `RoleAssignment`.
