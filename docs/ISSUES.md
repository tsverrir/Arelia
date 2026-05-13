# Known Issues & Gotchas

This document records issues, bugs, edge cases, and developer gotchas discovered during implementation and testing.

---

## ISSUE-001 — File-naming mismatch in Application layer commands

**Severity:** Low (cosmetic)  
**Status:** ✅ Fixed  
**Area:** Application / CQRS

**Description:**  
Two command files retained their old names while the class names inside had been updated:
- `DeactivateOrganizationUser.cs` → contained `SuspendOrganizationUserCommand` + `SuspendOrganizationUserHandler`
- `DeleteOrganizationUser.cs` → contained `RemoveOrganizationUserCommand` + `RemoveOrganizationUserHandler`

**Fix applied:** Renamed files to match class names (`git mv`):
- `DeactivateOrganizationUser.cs` → `SuspendOrganizationUser.cs`
- `DeleteOrganizationUser.cs` → `RemoveOrganizationUser.cs`

---

## ISSUE-002 — Suspended user still shows Active chip in People list

**Severity:** Medium  
**Status:** ✅ Fixed  
**Area:** Web / PeopleList.razor, Application / GetPeople.cs

**Description:**  
After suspending a user (which ends all active role assignments), the People list showed the user with an "Active" status badge because the chip was derived from `Person.IsActive`, not from whether they had active role assignments.

**Fix applied:**
- Added `IsSuspended` field to `PersonListDto`
- `GetPeopleHandler` now computes: `IsSuspended = IsActive && has OrganizationUser in org && no active RoleAssignments`
- `PeopleList.razor` status chip now shows three states:
  - 🟢 **Active** — `IsActive && !IsSuspended`
  - 🟠 **Suspended** — `IsActive && IsSuspended` (has OrganizationUser but no active roles)
  - ⚪ **Inactive** — `!IsActive`
- Grouped view now has a separate **Suspended** expansion panel (only shown when there are suspended people)

---

## ISSUE-003 — SQLite WAL lock during concurrent dev builds

**Severity:** Medium (dev only)  
**Status:** Open  
**Area:** Infrastructure / Database

**Description:**  
When the development app is running and a migration or build touches the SQLite database file, a WAL (Write-Ahead Logging) lock conflict can occur. Symptoms: `SqliteException: database is locked`.

**Impact:** Dev workflow interruption only. Not a production issue (production should use PostgreSQL or SQL Server).  
**Workaround:** Stop the running app before applying migrations. Alternatively, copy the DB to a new path for testing.

---

## ISSUE-004 — MSBuild `MSB3492` file-locking on `obj/` artifacts

**Severity:** Medium (dev only)  
**Status:** Open  
**Area:** Build

**Description:**  
After running `dotnet test` (which invokes CodeCoverage), MSBuild can leave locked files in `obj/` directories, causing subsequent builds to fail with `MSB3492: File XYZ could not be written — used by another process`.

**Workaround:**
```powershell
Remove-Item "src/Arelia.Application/obj" -Recurse -Force
Remove-Item "src/Arelia.Web/obj" -Recurse -Force
dotnet build -q
```
A second build attempt is sometimes needed after a clean if MSBuild recreates cached files.

---

## ISSUE-005 — `CreateOrganization` — system admin not auto-linked

**Severity:** Low  
**Status:** By design (acceptable)  
**Area:** Application / Organizations

**Description:**  
When a System Admin creates a new organisation via `/system/organizations`, they are **not** automatically added as a member of that organisation. The `CreatingUserId` was intentionally removed per ADR-0002 (System Admins operate at the system level, not the org level).

**Impact:** The newly created org has zero members until an org admin or the system admin manually adds people.  
**Workaround:** After creating an org, use "Direct Assign" or invite a user to give the org its first admin.

---

## ISSUE-006 — Token URL encoding in invitation emails

**Severity:** Medium  
**Status:** ✅ Fixed  
**Area:** Infrastructure / Email

**Description:**  
The ASP.NET Identity password-reset token contains characters (`+`, `=`, `/`) that can be corrupted by some email clients or HTTP libraries if not URL-encoded before embedding in the invite link. The current implementation uses `Uri.EscapeDataString(token)` which handles this correctly for most cases, but some clients double-decode `%2B` → `+` before the server receives it.

**Fix applied:**
- Created `InvitationTokenProvider` in `src/Arelia.Infrastructure/Services/InvitationTokenProvider.cs`
- Extends `DataProtectorTokenProvider<ApplicationUser>`, overriding `GenerateAsync` (Base64→Base64url) and `ValidateAsync` (reverse conversion before delegating)
- Registered as the default token provider via `.AddTokenProvider<InvitationTokenProvider>(TokenOptions.DefaultProvider)` in `DependencyInjection.cs`
- Removed `Uri.EscapeDataString(token)` from `UserService.SendInvitationEmailAsync` — tokens are now URL-safe by construction

---

## ISSUE-007 — Duplicate person records on re-invite

**Severity:** Medium  
**Status:** ✅ Fixed  
**Area:** Application / InviteUser command

**Description:**  
If an org admin invites the same email address twice (e.g. by navigating away and re-submitting the invite form), a second `Person` record can be created for the same email before the first user completes registration.

**Fix applied:**
- In `InviteUserCommand` (Email-first path), both the "user already exists" and "new user" branches now query for an existing `Person` with the same email in the same org (`IgnoreQueryFilters()`, `!IsDeleted`) before creating a new one
- If a matching Person is found, it is reused rather than creating a duplicate

---

## ISSUE-008 — Self-service resend uses generic org name

**Severity:** Low  
**Status:** ✅ Fixed  
**Area:** Web / AcceptInvitation.razor

**Description:**  
When a user clicks "Resend Invitation" from the `AcceptInvitation` page (self-service path), the invitation email is sent with the org name set to `"your organisation"` because there is no org context available on the unauthenticated invitation page.

**Fix applied:**
- `UserService.SendInvitationEmailAsync` now appends `&orgName={Uri.EscapeDataString(orgName)}` to the invite URL
- `AcceptInvitation.razor` reads `OrgName` from the query string via `[SupplyParameterFromQuery]` and stores it in `InputModel.OrgName` with a hidden form field
- `ResendInvitationAsync` uses `Input.OrgName ?? "your organisation"` as the org name when re-sending

---

## ISSUE-009 — System Admin with org memberships always sees org picker first

**Severity:** Low  
**Status:** ✅ Fixed  
**Area:** Web / SelectOrganization.razor

**Description:**  
A System Admin who is also a member of one or more organisations sees the standard org picker on login. There is no prominent "Go to System Panel" shortcut in the org picker.

**Fix applied:**
- `SelectOrganization.razor` tracks `_isSystemAdmin` from `AuthenticationState`
- When the user is a SystemAdmin and the org list is non-empty, an "Open System Panel" button (`/system/organizations`) is shown below the organisation list, separated by a divider

---

## ISSUE-010 — `RoleDetail.razor` — Admin role is not read-only

**Severity:** Medium  
**Status:** ✅ Fixed  
**Area:** Web / RoleDetail.razor

**Description:**  
The `Admin` role detail page (`/roles/{id}` where the role has `RoleType == Admin`) shows editable fields, save, and delete buttons. The permission matrix on `/roles` correctly disables Admin checkboxes, but `RoleDetail.razor` does not enforce read-only behaviour for system roles (`Admin`, `Board`, `Member`).

**Fix applied:**
- Added `RoleType` to `RoleDetailDto` and projected it in `GetRoleDetailHandler`
- `RoleDetail.razor` now checks `_role.RoleType`:
  - `Admin`: permission checkboxes are greyed-out (disabled), name field is read-only, Save/Delete buttons are hidden, info alert explains constraints
  - `Board` / `Member`: name field is read-only, Save/Delete hidden, permissions remain editable, info alert explains constraints
  - `Custom`: full editing capabilities (unchanged)
- A "System Role" chip is displayed in the page heading for non-Custom roles

---

## ISSUE-011 — `AcceptInvitation` — non-existent userId shows "Account Already Active"

**Severity:** High  
**Status:** ✅ Fixed  
**Area:** Web / AcceptInvitation.razor

**Description:**  
If a user navigated to `/Account/AcceptInvitation?userId=00000000-...&token=...` with a fake or deleted user ID, the page showed "Account Already Active" instead of "Invalid or Expired Link". Root cause: `IsAccountPendingAsync` returns `false` for non-existent users (correct by design), but the page did not distinguish between "user exists but active" and "user does not exist".

**Fix applied:** Added `UserManager.FindByIdAsync(resolvedUserId)` null check in `OnInitializedAsync` **before** calling `IsAccountPendingAsync`. A null result now sets `isInvalidLink = true`.

---

## ISSUE-012 — "Resend Invitation" button appears for fake/invalid user IDs

**Severity:** Low  
**Status:** ✅ Fixed  
**Area:** Web / AcceptInvitation.razor

**Description:**  
On the `AcceptInvitation` page, the "Resend Invitation" button in the invalid-link state is shown whenever `Input.UserId` is non-empty — even for fake or non-existent user IDs. Clicking it calls `ResendInvitationAsync`, which calls `IsAccountPendingAsync` (returns `false` for non-existent user), and then incorrectly shows "Account Already Active".

**Fix applied:**
- `ResendInvitationAsync` now calls `UserManager.FindByIdAsync(Input.UserId)` first
- If the user is not found, it sets `errorMessage = "This invitation is no longer valid."` and returns without proceeding to the resend path

---

## ISSUE-013 — Permission enum values displayed as `PascalCase` code names in UI

**Severity:** Low  
**Status:** ✅ Fixed  
**Area:** Web / RoleManagement.razor, RoleDetail.razor

**Description:**  
Permissions are displayed using their C# enum names (e.g. `ManagePeople`, `ViewFinanceReports`) rather than human-friendly labels. This looks technical and is not suitable for end users.

**Fix applied:**
- Added `[Display(Name = "...")]` attributes to all `Permission` enum values in `src/Arelia.Domain/Enums/Permission.cs`
- Created `src/Arelia.Web/Extensions/PermissionExtensions.cs` with `GetDisplayName()` extension method (reads `DisplayAttribute` via reflection, falls back to `ToString()`)
- `RoleDetail.razor` and `RoleManagement.razor` updated to call `permission.GetDisplayName()` instead of `permission.ToString()` / default interpolation

---

## Developer Gotchas

### Log capture in development
`DevEmailSender` writes invitation URLs to the application log with a `[DEV EMAIL]` prefix. To capture these during testing:
```powershell
# Run app with log tee
dotnet run --project src/Arelia.Web 2>&1 | Tee-Object -FilePath app.log

# Extract invite URLs after triggering an invite
Get-Content app.log | Select-String "DEV EMAIL" | Select-Object -Last 5
```

### Finding app PID for restart
```powershell
netstat -ano | Select-String ":5047"
# Then Stop-Process -Id <PID>
```

### Test project build after `dotnet test`
Run `dotnet build tests/Arelia.Application.Tests` followed by `dotnet test --no-build` to avoid MSBuild locking issues. Do **not** run `dotnet test` alone during active development if CodeCoverage artifacts are present.

### Tenant context in handlers
All handlers that create or query `BaseEntity` subtypes must use `IAreliaDbContext` directly. The global query filter silently applies `OrganizationId == currentOrgId && IsActive == true`. Use `IgnoreQueryFilters()` with an explicit org predicate for cross-tenant lookups (e.g. System Admin queries).
