# Email

Arelia sends transactional emails for account confirmation and password resets. The email sender is selected automatically based on configuration:

| Configuration | Behaviour |
|---|---|
| `Resend:ApiToken` **not set** | Logs email links to the console (`DevEmailSender`) — default in development |
| `Resend:ApiToken` **set** | Sends real emails via [Resend](https://resend.com) (`ResendEmailSender`) |

## Setting up Resend

1. **Create an account** at [resend.com](https://resend.com) (free tier: 3,000 emails/month, no credit card required).

2. **Add and verify your sending domain** — in the Resend dashboard go to **Domains → Add Domain** and add the DNS records it provides. This authorises Resend to send mail on behalf of your domain.

3. **Create an API key** under **API Keys → Create API Key**. Copy the key — it is only shown once.

4. **Configure the application** by setting the following environment variables on your server (never put secrets in `appsettings.json`):

   | Variable | Description |
   |---|---|
   | `Resend__ApiToken` | Your Resend API key (`re_...`) |
   | `Resend__From` | Sender address shown to recipients, e.g. `Arelia <noreply@yourdomain.com>` |

   In the production `.env` file next to `docker-compose.yml`:

   ```bash
   Resend__ApiToken=re_xxxxxxxxxxxxxxxxxxxxxxxx
   Resend__From=Arelia <noreply@yourdomain.com>
   ```

5. **Restart the container** — the app picks up the new environment variables on startup and switches to the real email sender automatically.

> **Development:** With no API token set, email links are printed to the application log (`[DEV EMAIL] Confirmation link for ...`). You can copy the link from the terminal to confirm accounts without sending any email.
