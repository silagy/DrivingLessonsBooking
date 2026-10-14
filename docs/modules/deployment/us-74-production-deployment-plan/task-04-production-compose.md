# Task 4 of 8: Production compose overlay, Caddy, and the compose wrapper

> Part of [#74: Production Deployment](README.md). Requires tasks 1–3 committed. Work on branch `74-production-deployment`, commands from the repo root in bash.

**Why:** Decisions 5 and 6. Production needs HTTPS in front of the app, a prebuilt image instead of a build on a 2 GB box, containers that survive reboots, logs that cannot fill the disk, and no port open except Caddy's. Local development must not change.

**Files:**
- Create: `deploy/docker-compose.prod.yml`
- Create: `deploy/Caddyfile`
- Create: `deploy/dl-compose.sh`
- Create: `.gitattributes`
- Modify: `docker-compose.yml` (drop the app's port mapping)
- Modify: `docker-compose.override.yml` (add it back for local runs)
- Modify: `.env.example`

**Interfaces:**
- Consumes: task 1 — `GET /api/health`; the `Dockerfile` (image listens on `8080`).
- Produces (tasks 5–8 rely on these):
  - Server directory layout (decision 5): `/opt/drivinglessons/{docker-compose.yml, docker-compose.prod.yml, Caddyfile, dl-compose.sh, pg-backup.sh, .env, release.env}`.
  - `dl-compose.sh <compose args>` — runs `docker compose --env-file .env --env-file release.env -f docker-compose.yml -f docker-compose.prod.yml <args>` from its own directory.
  - `release.env` holds exactly one line: `APP_TAG=<sha>`.
  - `.env` gains `DOMAIN`, `ACME_EMAIL`, `BACKUP_BUCKET`.
  - Compose service names `postgres`, `app`, `caddy`; database `drivinglessons`, user `app` (unchanged).

Docker Compose here is v5.1.4 (`!reset` needs ≥ 2.24; the server installs the current plugin in task 6). `core.autocrlf` is `true` on this machine, so without `.gitattributes` the scripts would be checked out with CRLF and fail on Linux with `/usr/bin/env: 'bash\r'`.

- [ ] **Step 1: Line endings for deploy files**

Create `.gitattributes`:

```gitattributes
deploy/*.sh text eol=lf
deploy/Caddyfile text eol=lf
deploy/*.yml text eol=lf
```

- [ ] **Step 2: Move the app port to the local override**

In `docker-compose.yml`, delete these two lines from the `app` service:

```yaml
    ports:
      - "8080:8080"
```

Replace `docker-compose.override.yml` with:

```yaml
services:
  postgres:
    ports:
      - "5432:5432"

  app:
    ports:
      - "8080:8080"
```

(`docker compose up` loads the override automatically, so `http://localhost:8080` keeps working; the server never loads it because `dl-compose.sh` names its files with `-f`.)

- [ ] **Step 3: The production overlay**

Create `deploy/docker-compose.prod.yml`:

```yaml
x-logging: &logging
  driver: json-file
  options:
    max-size: 10m
    max-file: "3"

services:
  postgres:
    restart: unless-stopped
    logging: *logging

  app:
    build: !reset null
    image: ghcr.io/silagy/drivinglessonsbooking:${APP_TAG:?APP_TAG is written to release.env by the deploy}
    restart: unless-stopped
    logging: *logging
    environment:
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"

  caddy:
    image: caddy:2
    restart: unless-stopped
    logging: *logging
    depends_on:
      - app
    ports:
      - "80:80"
      - "443:443"
      - "443:443/udp"
    environment:
      DOMAIN: ${DOMAIN:?DOMAIN must be set in .env}
      ACME_EMAIL: ${ACME_EMAIL:?ACME_EMAIL must be set in .env}
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
      - caddy_config:/config

volumes:
  caddy_data:
  caddy_config:
```

Notes for the reviewer:
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is ASP.NET Core's built-in switch: it adds the forwarded-headers middleware for `X-Forwarded-For` and `X-Forwarded-Proto` and trusts any proxy. That is safe only because the app's port is not published in production — Caddy is the only thing that can reach it — and Caddy replaces any client-sent `X-Forwarded-For` with the real client address. Task 1's per-IP limits depend on this.
- `caddy_data` holds the Let's Encrypt account and certificates; losing it means re-issuing (rate-limited by Let's Encrypt), so never `down -v` in production.

- [ ] **Step 4: Caddy and the wrapper**

Create `deploy/Caddyfile`:

```caddyfile
{
	email {$ACME_EMAIL}
}

{$DOMAIN} {
	encode zstd gzip
	header Strict-Transport-Security "max-age=31536000"
	reverse_proxy app:8080
}
```

(Caddy obtains and renews the certificate for `{$DOMAIN}` automatically and redirects HTTP to HTTPS on its own. For `DOMAIN=localhost` it uses its internal CA instead — the local smoke below relies on that.)

Create `deploy/dl-compose.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

exec docker compose \
    --env-file .env \
    --env-file release.env \
    -f docker-compose.yml \
    -f docker-compose.prod.yml \
    "$@"
```

```bash
git add --chmod=+x deploy/dl-compose.sh
```

- [ ] **Step 5: `.env.example`**

Append to `.env.example`:

```bash
# Production only (deploy/docker-compose.prod.yml, deploy/pg-backup.sh) - see docs/operations/runbook.md
DOMAIN=your-domain.example
ACME_EMAIL=admin@your-domain.example
BACKUP_BUCKET=your-backup-bucket-name
```

- [ ] **Step 6: Local smoke of the production overlay**

Recreate the server layout in a git-ignored folder and run it exactly as the server will (Docker Desktop running; nothing else on ports 80/443):

```bash
SMOKE=.superpowers/sdd/us-74-prod-smoke
rm -rf "$SMOKE" && mkdir -p "$SMOKE"
cp docker-compose.yml deploy/docker-compose.prod.yml deploy/Caddyfile deploy/dl-compose.sh "$SMOKE/"
cat > "$SMOKE/.env" <<'EOF'
POSTGRES_PASSWORD=smoke-db-password
ADMIN_EMAIL=admin@local.dev
ADMIN_PASSWORD='DevAdmin#2026'
JWT_SIGNING_KEY=smoke-only-signing-key-at-least-32-characters-long
EMAIL_ENABLED=false
DOMAIN=localhost
ACME_EMAIL=admin@local.dev
EOF
echo "APP_TAG=smoke" > "$SMOKE/release.env"
docker build -t ghcr.io/silagy/drivinglessonsbooking:smoke .
"$SMOKE/dl-compose.sh" config --quiet && echo "config ok"
"$SMOKE/dl-compose.sh" up -d
sleep 20
"$SMOKE/dl-compose.sh" ps --format "table {{.Service}}\t{{.Status}}\t{{.Ports}}"
```

Expected: `config ok`; three services `Up` (`postgres` healthy); **only** `caddy` lists published ports (`0.0.0.0:80->80/tcp`, `0.0.0.0:443->443/tcp`, `443/udp`); `app` and `postgres` show only container ports (`8080/tcp`, `5432/tcp`).

```bash
curl -sk -w "  <- %{http_code}\n" https://localhost/api/health
curl -s -o /dev/null -w "http -> %{http_code} %{redirect_url}\n" http://localhost/api/health
curl -sk -o /dev/null -w "spa: %{http_code}\n" https://localhost/
curl -sk -I https://localhost/ | grep -i strict-transport
for i in 1 2 3 4 5 6; do
  curl -sk -o /dev/null -w "login $i (spoofed XFF): %{http_code}\n" -X POST https://localhost/api/auth/login \
    -H "X-Forwarded-For: 198.51.100.$i" -H "Content-Type: application/json" \
    -d '{"email":"admin@local.dev","password":"wrong-password"}'
done
```

Expected: `Healthy  <- 200`; `http -> 308 https://localhost/api/health`; `spa: 200`; a `strict-transport-security: max-age=31536000` header; logins 1–5 `401`, login 6 `429` — a different spoofed `X-Forwarded-For` per request does **not** reset the limit (Caddy overwrote it).

Restart behaviour (the reboot itself is tested on the server in task 8):

```bash
docker exec "$("$SMOKE/dl-compose.sh" ps -q app)" sh -c 'kill 1'
sleep 15
"$SMOKE/dl-compose.sh" ps --format "table {{.Service}}\t{{.Status}}"
curl -sk -w "  <- %{http_code}\n" https://localhost/api/health
```

Expected: `app` is `Up` again (restarted by its `unless-stopped` policy); health `200`. (The process is stopped from inside on purpose: `docker kill`/`docker stop` count as a manual stop, which `unless-stopped` deliberately does not undo.)

Missing settings fail loudly:

```bash
mv "$SMOKE/release.env" "$SMOKE/release.env.bak" && touch "$SMOKE/release.env"
"$SMOKE/dl-compose.sh" config --quiet 2>&1 | head -2
mv "$SMOKE/release.env.bak" "$SMOKE/release.env"
```

Expected: an error naming `APP_TAG is written to release.env by the deploy`.

Tear down (the smoke volumes only — this project is named after the smoke folder):

```bash
"$SMOKE/dl-compose.sh" down -v
docker image rm ghcr.io/silagy/drivinglessonsbooking:smoke
```

- [ ] **Step 7: Local development still works**

```bash
docker compose config --format json | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const c=JSON.parse(s);console.log('app ports:',JSON.stringify(c.services.app.ports),'| caddy:',!!c.services.caddy)})"
```

Expected: `app ports: [{"mode":"ingress","target":8080,"published":"8080","protocol":"tcp"}] | caddy: false` (exact JSON shape may differ; what matters is `8080` published and no `caddy`). Running `docker compose up --build` remains the documented local flow — no need to run it here.

- [ ] **Step 8: Commit**

```bash
git add .gitattributes deploy/docker-compose.prod.yml deploy/Caddyfile deploy/dl-compose.sh \
        docker-compose.yml docker-compose.override.yml .env.example
git commit -m "build(deploy): production compose overlay with Caddy, pinned GHCR image and log rotation

- only Caddy publishes ports; the app port moves to the local override
- the app trusts forwarded headers so per-IP limits see the real client
- dl-compose.sh combines .env, release.env and both compose files on the server

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
