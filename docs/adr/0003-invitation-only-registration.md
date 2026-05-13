# Closed registration: accounts created by invitation only

User accounts can only be created through an Invitation sent by a System Administrator or a user with the `ManagePeople` permission. The `/Account/Register` page is retained in the UI but rejects self-registration attempts with an "invitation only" message.

## Considered options

- **Open self-registration** — any visitor can create an account. Incompatible with the requirement that only trusted admins can bring users into the system.
- **Token-gated registration** — the Register page is only functional when accessed with a valid invite token in the URL. Simpler technically, but ambiguous: it uses the "Register" framing for what is really Registration Completion.
- **Dedicated invite-only model with a separate completion page** — self-registration is blocked; a dedicated `/Account/AcceptInvitation` page handles Registration Completion using the password-reset token mechanism. Chosen.

## Consequences

- `/Account/Register` shows an "invitation only" message if accessed without a valid invite token. It does not create accounts.
- New user accounts are created server-side at invite time with a null password hash and `EmailConfirmed = false` (Pending Account state).
- The invite link sent by email contains a password-reset token. Following the link lands on `/Account/AcceptInvitation`, which sets the password, confirms the email, and signs the user in.
- Expired invite links redirect to a clear error page with a self-service "resend invitation" button, which works only if the account is still in Pending state (null password hash).
- A Person can be created without a linked User (no email provided). Role assignments are valid for any Person regardless of User linkage.
- `UserManagement` is removed from the `Permission` enum. Inviting/removing users from an org is governed by `ManagePeople`.
