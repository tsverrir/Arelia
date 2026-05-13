# Docker

## Local testing

Build and run from source using Docker Compose:

```bash
docker compose up --build
```

The app starts at **http://localhost:8080**. On first boot the database is created and seeded with a default admin account (`admin@arelia.dev` / `Admin123!`).

Data (SQLite database and uploaded files) is stored in `./data/` on your host machine.

## Production deployment

The [GitHub Actions workflow](../.github/workflows/docker-publish.yml) builds and pushes an image to GitHub Container Registry on every push to `main`:

```
ghcr.io/sverrir-asmundsson/arelia:latest
ghcr.io/sverrir-asmundsson/arelia:<git-sha>
```

To deploy on a server, copy `docker-compose.yml`, set your credentials, and start:

```bash
# Create a .env file with required secrets
cat > .env <<'EOF'
Seed__AdminEmail=you@example.com
Seed__AdminPassword=YourStrongPassword1!
CLOUDFLARE_TUNNEL_TOKEN=eyJhIjoiY...   # from Cloudflare Zero Trust dashboard
EOF

docker compose pull
docker compose --profile production up -d
```

### Authenticating with GHCR

If the package is private (the default for new packages), you need to authenticate Docker before pulling:

1. Create a [GitHub Personal Access Token](https://github.com/settings/tokens) (classic) with the **`read:packages`** scope.
2. Log in:

```bash
echo YOUR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

Credentials are saved to `~/.docker/config.json` and reused automatically by `docker compose pull`.

### Automatic updates with Watchtower

The production `docker-compose.yml` includes [Watchtower](https://containrrr.dev/watchtower/), which polls GHCR every 5 minutes and automatically pulls and restarts the `arelia` container whenever a new image is pushed.

Watchtower reads GHCR credentials from `~/.docker/config.json` on the host, so **log in before starting the stack** (see above). Once running, every push to `main` will be live within ~5 minutes — no manual `docker compose pull` needed.

Data is stored in a named Docker volume (`arelia_data`). The admin credentials are written to the database on first boot only — changing the env vars afterwards has no effect on the existing account.

### Cloudflare Tunnel

The production stack includes a `cloudflared` service that creates a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/), exposing the app publicly over HTTPS without opening any firewall ports or needing a static IP. Cloudflare handles TLS termination; the container always speaks plain HTTP internally.

**One-time setup:**

1. Go to [Cloudflare Zero Trust](https://one.dash.cloudflare.com) → **Networks → Tunnels → Create a tunnel**.
2. Choose **Cloudflared** as the connector type and follow the wizard.
3. Under **Public Hostnames**, add a route and set the service to `http://arelia:8080` (the Docker internal hostname).
4. Copy the **tunnel token** shown in the install step.

**Starting the stack:**

Create a `.env` file next to `docker-compose.yml` and set the token:

```bash
CLOUDFLARE_TUNNEL_TOKEN=eyJhIjoiY...  # paste your token here
```

Then start with the production profile (includes Watchtower + cloudflared):

```bash
docker compose --profile production up -d
```

**How it works with Blazor Server:**

- Cloudflare Tunnels forward WebSocket connections natively — the SignalR connection Blazor relies on for all interactivity works without any additional configuration.
- The app uses `ForwardedHeaders` middleware to read the `X-Forwarded-For` / `X-Forwarded-Proto` headers that Cloudflare sets, so the app correctly identifies requests as HTTPS. This ensures authentication cookies get the `Secure` flag, antiforgery tokens are valid, and redirect URIs are correct.
- Cloudflare's WebSocket idle timeout is 100 seconds; Blazor's keep-alive ping (every 15 s by default) keeps connections alive well within that window.
