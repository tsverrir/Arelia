# Admin/Users page removed in favour of People list + PersonDetail

An Org Admin's primary concern is the people in their organisation — not a filtered view of only those with linked accounts. The dedicated `/admin/users` page provided a user-centric view (invite, suspend, remove, resend invite) but split the admin workflow across two lists. Since every relevant account action is accessible via PersonDetail, and since Org Admins think in terms of People rather than Users, the Admin/Users page was removed.

To preserve discoverability, the People list status chip was extended to surface account state with the following priority: **Pending** (account not yet activated) → **Suspended** (account exists, no active roles) → **Active** → **Inactive** (no linked account — person is manually managed by an admin).

## Considered options

- **Keep Admin/Users page** — a narrower view of only account-linked people. Rejected because it fragments the workflow: an Org Admin who wants to invite or suspend a member must leave the People list to find that person in a second list.
- **Merge into People list + PersonDetail** — surface account status inline, keep all actions in PersonDetail. Chosen because the People list already is the canonical view of an org's membership; account status is one property of a person, not a reason for a separate page.
