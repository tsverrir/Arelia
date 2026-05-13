# System Administrator as an ASP.NET Core Identity role, scoped to /system/*

The system needs a platform-level administrator who can create organizations, manage user memberships across all orgs, and promote other system admins — without being bound to any specific organization. We chose to represent this as an ASP.NET Core Identity role (`"SystemAdmin"`) rather than a domain entity or a flag on the User record. Organization creation is restricted to System Administrators only.

## Considered options

- **Flag on User (`IsSystemAdmin`)** — simple, but a plain boolean cannot be enforced by ASP.NET Core's built-in authorization policies without custom middleware.
- **Domain entity (`SystemRole` / `SystemRoleAssignment`)** — introduces a parallel role structure outside the org-scoped model, adding complexity for no benefit since the system admin concept is purely a platform-level concern.
- **ASP.NET Core Identity role (`"SystemAdmin"`)** — fits the existing identity stack. Enforced with `[Authorize(Roles = "SystemAdmin")]`. Managed via `UserManager<ApplicationUser>`. Seeded from environment variables on first startup.

## Consequences

- A `"SystemAdmin"` Identity role is seeded on startup. The initial System Administrator's email and password are read from environment variables (`ARELIA_ADMIN_EMAIL`, `ARELIA_ADMIN_PASSWORD`).
- System Administrator UI lives under `/system/*`. Org Admin UI remains under `/admin/*`. These surfaces are never mixed.
- System Administrators can edit all fields on any `Organization` and manage user memberships (invite or directly assign) in any org, bypassing the tenant filter.
- System Administrators cannot access org-internal data (people, activities, finance, attendance) for orgs they are not a member of.
- A System Administrator may simultaneously hold `OrganizationUser` memberships and act as an Org Admin in specific orgs — the two roles are independent.
- Organization creation is restricted to System Administrators. Self-service org creation by regular users is not permitted.
- System Administrators can promote and demote other System Administrators through the UI. A System Administrator may demote themselves (operator must ensure at least one system admin remains via environment seed).
