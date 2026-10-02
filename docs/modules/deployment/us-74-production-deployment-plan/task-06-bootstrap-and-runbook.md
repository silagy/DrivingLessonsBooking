# Task 6 of 8: Server bootstrap script and the operations runbook

> Part of [#74: Production Deployment](README.md). Requires tasks 4–5 committed. Work on branch `74-production-deployment`, commands from the repo root in bash.

**Why:** Decisions 1, 2, 11, 12, 13, 14. Everything outside the repo — the domain, the instance, SES, S3, IAM, GitHub secrets — is a one-time manual setup. Writing it as an exact runbook (and the host part as an idempotent script) makes it repeatable, reviewable, and the place to look when something breaks at 07:00 on a close day.

**Files:**
- Create: `deploy/bootstrap-server.sh`
- Create: `docs/operations/runbook.md`
- Modify: `docs/tech-stack.md` (Hosting & Operations)
- Modify: `docs/decisions/0002-hosting-aws-lightsail.md` (reaffirmation note)
- Modify: `docs/development/running-the-project.md` (link to the runbook)

**Interfaces:**
- Consumes: task 4 — server layout, `dl-compose.sh`, `.env` keys (`DOMAIN`, `ACME_EMAIL`, `BACKUP_BUCKET` + the existing ones); task 5 — `pg-backup.sh`, profile `drivinglessons-backup`, `/var/backups/drivinglessons`; task 1 — `/api/health`.
- Produces (task 7's workflow and task 8 rely on these exact names):
  - Linux user `deploy` (docker group), `/opt/drivinglessons` and `/var/backups/drivinglessons` owned by it.
  - GitHub environment **`production`** with secrets `DEPLOY_HOST` (static IP), `DEPLOY_SSH_KEY` (private key, ed25519), `DEPLOY_KNOWN_HOSTS` (the `ssh-keyscan` line) and variable `DEPLOY_DOMAIN` (bare domain, no scheme).
  - S3 bucket `<bucket>` with prefix `daily/`; IAM user `drivinglessons-backup`.

- [ ] **Step 1: The bootstrap script**

Create `deploy/bootstrap-server.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

readonly DEPLOY_USER=deploy
readonly APP_DIR=/opt/drivinglessons
readonly BACKUP_DIR=/var/backups/drivinglessons
readonly SWAP_FILE=/swapfile
readonly SWAP_SIZE=1G
readonly SSHD_DROP_IN=/etc/ssh/sshd_config.d/99-drivinglessons.conf

if [[ $EUID -ne 0 ]]; then
    echo "Run with sudo." >&2
    exit 1
fi

if [[ $# -lt 1 ]]; then
    echo "Usage: sudo ./bootstrap-server.sh '<ssh public key>' ['<ssh public key>' ...]" >&2
    exit 1
fi

export DEBIAN_FRONTEND=noninteractive

echo "== packages and automatic security updates"
apt-get update
apt-get -y upgrade
apt-get -y install ca-certificates curl unattended-upgrades
printf 'APT::Periodic::Update-Package-Lists "1";\nAPT::Periodic::Unattended-Upgrade "1";\n' \
    > /etc/apt/apt.conf.d/20auto-upgrades
timedatectl set-timezone UTC

echo "== docker engine and compose plugin"
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
. /etc/os-release
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable" \
    > /etc/apt/sources.list.d/docker.list
apt-get update
apt-get -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

echo "== swap"
if ! swapon --show | grep -q "$SWAP_FILE"; then
    fallocate -l "$SWAP_SIZE" "$SWAP_FILE"
    chmod 600 "$SWAP_FILE"
    mkswap "$SWAP_FILE"
    swapon "$SWAP_FILE"
    echo "$SWAP_FILE none swap sw 0 0" >> /etc/fstab
fi

echo "== deploy user"
if ! id "$DEPLOY_USER" > /dev/null 2>&1; then
    adduser --disabled-password --gecos "" "$DEPLOY_USER"
fi
usermod -aG docker "$DEPLOY_USER"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh"
printf '%s\n' "$@" > "/home/$DEPLOY_USER/.ssh/authorized_keys"
chown "$DEPLOY_USER:$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh/authorized_keys"
chmod 600 "/home/$DEPLOY_USER/.ssh/authorized_keys"

echo "== ssh: keys only"
printf 'PasswordAuthentication no\nKbdInteractiveAuthentication no\nPermitRootLogin no\n' > "$SSHD_DROP_IN"
sshd -t
systemctl reload ssh

echo "== directories"
install -d -m 750 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$APP_DIR" "$BACKUP_DIR"

echo "== aws cli"
if ! snap list aws-cli > /dev/null 2>&1; then
    snap install aws-cli --classic
fi

echo "== done: docker $(docker --version | cut -d' ' -f3 | tr -d ,), compose $(docker compose version --short)"
```

```bash
git add --chmod=+x deploy/bootstrap-server.sh
```

It is safe to run again (every step checks or overwrites to the same result). The `authorized_keys` file is **replaced** by the keys passed in — pass every key that should be able to log in as `deploy`.

- [ ] **Step 2: Syntax-check both scripts**

```bash
for f in deploy/*.sh; do bash -n "$f" && echo "$f: syntax ok"; done
docker run --rm -v "$(pwd -W 2>/dev/null || pwd)":/mnt koalaman/shellcheck:stable deploy/bootstrap-server.sh deploy/pg-backup.sh deploy/dl-compose.sh
git ls-files --eol deploy/ | grep -v "i/lf" || echo "all deploy files are LF in the index"
```

Expected: three `syntax ok`; shellcheck prints nothing (fix anything it reports; `SC1091` on `. /etc/os-release` may be silenced with `# shellcheck source=/dev/null` above that line — the only comment allowed in the script); `all deploy files are LF in the index`.

- [ ] **Step 3: The runbook**

Create `docs/operations/runbook.md` with exactly this content (placeholders in angle brackets are filled in at setup time and never committed with real values):

````markdown
# Production Runbook

How the production system is set up, released, backed up and restored. Hosting per [ADR 0002](../decisions/0002-hosting-aws-lightsail.md), email per [ADR 0005](../decisions/0005-email-over-ses-smtp.md). Region for everything: **Frankfurt, `eu-central-1`**.

| Thing | Where |
|---|---|
| App | `https://<domain>` |
| Health | `https://<domain>/api/health` → `Healthy` |
| Server | Lightsail instance `drivinglessons-prod`, static IP `<static-ip>`, user `deploy` |
| Files on the server | `/opt/drivinglessons/` (`.env` holds every secret, `release.env` the running version) |
| Image | `ghcr.io/silagy/drivinglessonsbooking:<commit sha>` |
| Backups | `s3://<bucket>/daily/`, 30 days; last 7 also in `/var/backups/drivinglessons/` |
| Releases | GitHub → Actions → **Deploy** → Run workflow |

Every compose command on the server goes through `./dl-compose.sh` (it combines `.env`, `release.env` and both compose files).

## 1. One-time setup

Do 1.1 and 1.4 first — they have waiting time.

### 1.1 Domain

1. Route 53 → Registered domains → Register domain. Pick a name and a TLD Route 53 sells. Registration creates a public **hosted zone** with the same name.
2. Wait for the "registration succeeded" email (minutes to hours).

### 1.2 Instance

1. Lightsail → Create instance: region **Frankfurt**, platform Linux, blueprint **OS only → Ubuntu 24.04 LTS**, plan **2 GB RAM**, name `drivinglessons-prod`.
2. Networking → Create static IP → attach to `drivinglessons-prod`. This is `<static-ip>`.
3. Instance → Networking → IPv4 firewall: keep **SSH 22** and **HTTP 80**, add **HTTPS 443 (TCP)** and a custom rule **UDP 443**. Delete anything else.
4. Route 53 → hosted zone `<domain>` → Create record: name empty (apex), type **A**, value `<static-ip>`, TTL 300.

### 1.3 Bootstrap the host

On your machine:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/drivinglessons-deploy -C "github-actions-deploy" -N ""
```

This key pair is only for GitHub Actions. Then, using the default Lightsail key (Account → SSH keys → download the Frankfurt default key):

```bash
scp -i <lightsail-key.pem> deploy/bootstrap-server.sh ubuntu@<static-ip>:~
ssh -i <lightsail-key.pem> ubuntu@<static-ip> \
  "sudo ./bootstrap-server.sh '$(cat ~/.ssh/id_ed25519.pub)' '$(cat ~/.ssh/drivinglessons-deploy.pub)'"
ssh deploy@<static-ip> "docker compose version && ls -ld /opt/drivinglessons"
```

The last line must work with your own key and print the compose version.

### 1.4 Email (SES)

1. SES (Frankfurt) → Identities → Create identity → **Domain** `<domain>`. Tick **Use a custom MAIL FROM domain**: `mail.<domain>`, behaviour on MX failure "Use default". Easy DKIM, RSA 2048, **Publish DNS records to Route 53**: on. Create.
2. Route 53 → `<domain>` → Create record: name `_dmarc`, type **TXT**, value `"v=DMARC1; p=none; rua=mailto:<admin address>"`.
3. SES → Account dashboard → **Request production access**: mail type Transactional; website `https://<domain>`; use case: "Weekly automated email to a handful of driving-school teachers (fewer than 20 recipients, one message each per week) with an Excel attachment, sent when a submission window closes. Recipients are the school's own teachers; no marketing, no purchased lists. Bounces and complaints are monitored in the SES console." Submit. Until approved, SES only delivers to verified addresses.
4. Wait until the identity shows **Verified** (DKIM and MAIL FROM both successful).
5. SES → SMTP settings → **Create SMTP credentials** → keep the generated username and password for `.env` (`EMAIL_USERNAME`, `EMAIL_PASSWORD`). The SMTP endpoint is `email-smtp.eu-central-1.amazonaws.com`, port 587.

### 1.5 Backup bucket and its write-only user

1. S3 (Frankfurt) → Create bucket `<bucket>` (globally unique, e.g. `drivinglessons-backups-<random>`): Block *all* public access **on**, versioning off, default encryption **SSE-S3**.
2. Bucket → Management → Create lifecycle rule `expire-daily`: prefix `daily/`, action **Expire current versions of objects** after **30** days.
3. IAM → Users → Create user `drivinglessons-backup` (no console access) → Attach policies directly → Create inline policy (JSON):

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": "s3:PutObject",
         "Resource": "arn:aws:s3:::<bucket>/daily/*"
       }
     ]
   }
   ```

4. User → Security credentials → Create access key → "Application running outside AWS". On the server:

   ```bash
   ssh deploy@<static-ip>
   aws configure --profile drivinglessons-backup   # access key, secret, region eu-central-1, output json
   ```

### 1.6 Secrets on the server

```bash
ssh deploy@<static-ip>
cd /opt/drivinglessons
nano .env        # every key from the repo's .env.example, real values
chmod 600 .env
```

- `POSTGRES_PASSWORD`, `ADMIN_PASSWORD`: `openssl rand -base64 24`; `JWT_SIGNING_KEY`: `openssl rand -base64 48`.
- `ADMIN_EMAIL`: the school owner's sign-in address.
- `EMAIL_ENABLED=true`, `EMAIL_HOST=email-smtp.eu-central-1.amazonaws.com`, `EMAIL_PORT=587`, `EMAIL_USERNAME`/`EMAIL_PASSWORD` from 1.4, `EMAIL_FROM_ADDRESS=noreply@<domain>`, `EMAIL_FROM_NAME` in Hebrew (quote it: `EMAIL_FROM_NAME='בית הספר לנהיגה'`).
- `DOMAIN=<domain>`, `ACME_EMAIL=<admin address>`, `BACKUP_BUCKET=<bucket>`.

Keep a copy of `.env` in your password manager. It is never committed and never stored in GitHub.

### 1.7 GitHub

1. Repo → Settings → Environments → New environment **`production`** (optionally: Required reviewers = you).
2. Environment secrets: `DEPLOY_HOST` = `<static-ip>`; `DEPLOY_SSH_KEY` = contents of `~/.ssh/drivinglessons-deploy` (the private key); `DEPLOY_KNOWN_HOSTS` = output of `ssh-keyscan -t ed25519 <static-ip>`.
3. Environment variable: `DEPLOY_DOMAIN` = `<domain>`.
4. After the **first** deploy pushes the image: GitHub → your profile → Packages → `drivinglessonsbooking` → Package settings → Change visibility → **Public** (the server pulls without credentials). The first deploy's pull step fails until this is done — re-run it.

### 1.8 Nightly backup

```bash
ssh deploy@<static-ip>
( crontab -l 2>/dev/null; echo '30 0 * * * /opt/drivinglessons/pg-backup.sh 2>&1 | logger -t drivinglessons-backup' ) | crontab -
crontab -l
```

00:30 UTC is 02:30 / 03:30 in Israel. Check a run with `journalctl -t drivinglessons-backup --since yesterday`.

## 2. Releasing

GitHub → Actions → **Deploy** → Run workflow → `ref` = `main` (or any commit sha). The run tests, builds `ghcr.io/silagy/drivinglessonsbooking:<sha>`, copies the compose files and scripts to `/opt/drivinglessons`, writes `release.env`, pulls, restarts, and waits up to 3 minutes for `https://<domain>/api/health`. A red run means production may be on the new image but unhealthy — roll back.

Avoid releasing while a submission window is open unless the change is needed for it.

## 3. Rolling back

Run **Deploy** again with `ref` = the previous good sha (the `APP_TAG` in an earlier successful run's log, or `git log`). Images are never deleted, so any earlier sha redeploys in about a minute. Database migrations only move forward — a release whose migration must be undone needs a restore (section 6).

## 4. Day to day

```bash
ssh deploy@<static-ip>
cd /opt/drivinglessons
cat release.env                         # running version
./dl-compose.sh ps                      # all three Up, postgres healthy
./dl-compose.sh logs --tail 200 app     # app log (window opens/closes, email sends)
./dl-compose.sh logs --tail 100 caddy   # certificates, proxy errors
curl -s https://<domain>/api/health
df -h / && free -h
```

## 5. Reboots and restarts

All three containers have `restart: unless-stopped`, Docker starts at boot, and the app reconciles windows that opened or closed while it was down (and sends their emails). After `sudo reboot`, wait a minute and check section 4. Never run `./dl-compose.sh down -v` — `-v` deletes the database and the certificates.

## 6. Restoring a backup

Restores use **your own** AWS credentials (the server's backup user cannot read).

1. Pick a dump: S3 → `<bucket>` → `daily/` → newest `drivinglessons-<stamp>.dump` → Download (or `aws s3 cp s3://<bucket>/daily/<file> .` with your profile).
2. Copy it up: `scp drivinglessons-<stamp>.dump deploy@<static-ip>:/var/backups/drivinglessons/`.
3. **Check it** (safe, production untouched) — restore into a scratch database and compare:

   ```bash
   ssh deploy@<static-ip>
   cd /opt/drivinglessons
   ./dl-compose.sh exec -T postgres createdb -U app restore_check
   ./dl-compose.sh exec -T postgres pg_restore -U app -d restore_check --no-owner < /var/backups/drivinglessons/drivinglessons-<stamp>.dump
   for db in drivinglessons restore_check; do
     ./dl-compose.sh exec -T postgres psql -U app -d $db -Atc \
       "select '$db', (select count(*) from students), (select count(*) from publications), (select count(*) from submissions), (select count(*) from slot_requests)"
   done
   ./dl-compose.sh exec -T postgres dropdb -U app restore_check
   ```

   The counts match production as of the dump's time.
4. **Replace production** (only when the live data is lost or wrong — this discards everything after the dump):

   ```bash
   ./dl-compose.sh stop app
   ./dl-compose.sh exec -T postgres dropdb -U app drivinglessons
   ./dl-compose.sh exec -T postgres createdb -U app drivinglessons
   ./dl-compose.sh exec -T postgres pg_restore -U app -d drivinglessons --no-owner < /var/backups/drivinglessons/drivinglessons-<stamp>.dump
   ./dl-compose.sh start app
   curl -s https://<domain>/api/health
   ```

## 7. Rotating a secret

Edit `.env`, then `./dl-compose.sh up -d` (recreates only the containers whose settings changed). Changing `POSTGRES_PASSWORD` after the first start does **not** change the database's password — also run `./dl-compose.sh exec postgres psql -U app -d drivinglessons -c "alter user app password '<new>'"`. A new `JWT_SIGNING_KEY` signs the admin out.
````

- [ ] **Step 4: Point the existing docs at it**

`docs/tech-stack.md` — in **Hosting & Operations**, replace the first bullet (`- **AWS Lightsail** instance ...`) with:

```markdown
- **AWS Lightsail** instance (2 GB plan, ~$10–12/mo, Frankfurt `eu-central-1`) with a static IP and a Route 53 domain
```

and replace the `caddy` bullet with:

```markdown
  - `caddy` — reverse proxy terminating HTTPS with automatic Let's Encrypt certificates (production overlay `deploy/docker-compose.prod.yml` only; local runs have no Caddy)
```

and add after the Backups bullet:

```markdown
- **Releases**: GitHub Actions — `ci.yml` tests every PR; `deploy.yml` (run by hand) pushes `ghcr.io/silagy/drivinglessonsbooking:<sha>` and restarts the server over SSH, gated on `/api/health`
- **Abuse limits**: per-IP rate limits on admin sign-in (5/min) and student identify (30/min)
- **Runbook**: [docs/operations/runbook.md](operations/runbook.md)
```

`docs/decisions/0002-hosting-aws-lightsail.md` — under the `**Date:** 12 June 2026` line, add:

```markdown
**Reaffirmed:** 2 October 2026 ([#74](https://github.com/silagy/DrivingLessonsBooking/issues/74)) — Hetzner, Render/Railway and Azure re-evaluated before go-live; Lightsail kept. Region Frankfurt; image registry GHCR; see [the runbook](../operations/runbook.md).
```

`docs/development/running-the-project.md` — at the end of the first paragraph (after `...for the architecture.`), add: ` Production setup and operations: [operations/runbook.md](../operations/runbook.md).`

- [ ] **Step 5: Commit**

```bash
git add deploy/bootstrap-server.sh docs/operations/runbook.md docs/tech-stack.md \
        docs/decisions/0002-hosting-aws-lightsail.md docs/development/running-the-project.md
git commit -m "docs(ops): production runbook and an idempotent server bootstrap script

Covers domain, Lightsail, SES with DKIM and custom MAIL FROM, the write-only
backup user, GitHub environment secrets, releases, rollback, reboots and restores.

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
