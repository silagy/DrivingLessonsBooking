# Production Runbook

How the production system is set up, released, backed up and restored. Hosting per [ADR 0002](../decisions/0002-hosting-aws-lightsail.md), email per [ADR 0005](../decisions/0005-email-over-ses-smtp.md). Region for everything: **Frankfurt, `eu-central-1`**. For the order of the whole path to production, start at [deploying-to-production.md](deploying-to-production.md).

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

Do 1.1 and 1.4 first: they have waiting time.

### 1.1 Domain

The DNS for `<domain>` lives in a Route 53 **public hosted zone** in the project account, wherever the domain itself is registered.

- **A TLD Route 53 sells** (`.com`, `.net`, ...): Route 53 → Registered domains → Register domain. Registration creates the hosted zone with the same name. Wait for the "registration succeeded" email (minutes to hours).
- **A TLD Route 53 does not sell** (for example `.co.il`): buy it at a registrar for that TLD. Then Route 53 → Hosted zones → Create hosted zone → name `<domain>`, type **Public**. Copy the zone's four **NS** values into the registrar's name-server settings for the domain. The registry publishes the change on its own schedule (the `.il` registry works in batches, so allow until the next morning). It is live when `nslookup -type=NS <domain> 8.8.8.8` returns the four `awsdns` servers.

SES (1.4) can be created right away; it verifies on its own once the name servers are live.

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

The last line must work with your own key and print the compose version. The script is safe to run again; it **replaces** `deploy`'s `authorized_keys` with the keys passed in, so always pass every key that should log in as `deploy`.

### 1.4 Email (SES)

1. SES (Frankfurt) → Identities → Create identity → **Domain** `<domain>`. Tick **Use a custom MAIL FROM domain**: `mail.<domain>`, behaviour on MX failure "Use default". Easy DKIM, RSA 2048, **Publish DNS records to Route 53**: on. Create.
2. Route 53 → `<domain>` → Create record: name `_dmarc`, type **TXT**, value `"v=DMARC1; p=none; rua=mailto:<admin address>"` (the `rua` part is optional; it asks receivers to send daily aggregate reports to that address).
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
4. After the **first** deploy pushes the image: GitHub → your profile → Packages → `drivinglessonsbooking` → Package settings → Change visibility → **Public** (the server pulls without credentials). The first deploy's pull step fails until this is done; re-run it.

### 1.8 Nightly backup

Do this **after the first successful deploy**: `pg-backup.sh` reaches `/opt/drivinglessons` with it, and a cron line installed earlier fails every night until then. Check with `ls -l /opt/drivinglessons/pg-backup.sh`.

```bash
ssh deploy@<static-ip>
( crontab -l 2>/dev/null; echo '30 0 * * * /opt/drivinglessons/pg-backup.sh 2>&1 | logger -t drivinglessons-backup' ) | crontab -
crontab -l
```

00:30 UTC is 02:30 / 03:30 in Israel. Check a run with `journalctl -t drivinglessons-backup --since yesterday`: a good night ends with `backup uploaded: ...`, a bad one with `backup FAILED (exit <n>)` after the reason. The server keeps the newest 7 dumps in `/var/backups/drivinglessons`; S3 keeps 30 days.

## 2. Releasing

GitHub → Actions → **Deploy** → Run workflow → `ref` = `main` (or any commit sha). The run tests, builds `ghcr.io/silagy/drivinglessonsbooking:<sha>`, copies the compose files and scripts to `/opt/drivinglessons`, writes `release.env`, pulls, restarts, and waits up to 3 minutes for `https://<domain>/api/health`. A red run means production may be on the new image but unhealthy: roll back.

Avoid releasing while a submission window is open unless the change is needed for it.

## 3. Rolling back

You need the previous good **full** 40-character sha: the `Healthy on <sha>` line of an earlier successful Deploy run, or `git log --format=%H`. A short sha fails at checkout.

**Fast path (about a minute, use it when the site is down):** the image of every sha ever deployed is still in GHCR, so the server can switch back without GitHub building anything.

```bash
ssh deploy@<static-ip>
cd /opt/drivinglessons
printf 'APP_TAG=%s\n' <previous-full-sha> > release.env
./dl-compose.sh up -d
curl -fsS https://<domain>/api/health    # Healthy
```

Then run **Deploy** with `ref` = that same sha, so the compose files, `Caddyfile` and scripts on the server match it again (only needed if the bad release changed them, but harmless otherwise). Deploy re-runs the tests and rebuilds the image first, so it takes several minutes; do not use it as the first response while students are submitting.

Database migrations only move forward: a release whose migration must be undone needs a restore (section 6).

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

All three containers have `restart: unless-stopped`, Docker starts at boot, and the app reconciles windows that opened or closed while it was down (and sends their emails). After `sudo reboot`, wait a minute and check section 4. Never run `./dl-compose.sh down -v`: `-v` deletes the database and the certificates.

## 6. Restoring a backup

Restores use **your own** AWS credentials (the server's backup user cannot read).

1. Pick a dump: S3 → `<bucket>` → `daily/` → newest `drivinglessons-<stamp>.dump` → Download (or `aws s3 cp s3://<bucket>/daily/<file> .` with your profile).
2. Copy it up: `scp drivinglessons-<stamp>.dump deploy@<static-ip>:/var/backups/drivinglessons/`.
3. **Check it** (safe, production untouched): restore into a scratch database and compare.

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
4. **Replace production** (only when the live data is lost or wrong; this discards everything after the dump):

   ```bash
   ./dl-compose.sh stop app
   ./dl-compose.sh exec -T postgres dropdb -U app drivinglessons
   ./dl-compose.sh exec -T postgres createdb -U app drivinglessons
   ./dl-compose.sh exec -T postgres pg_restore -U app -d drivinglessons --no-owner < /var/backups/drivinglessons/drivinglessons-<stamp>.dump
   ./dl-compose.sh start app
   curl -s https://<domain>/api/health
   ```

## 7. Rotating a secret

Edit `.env`, then `./dl-compose.sh up -d` (recreates only the containers whose settings changed). Changing `POSTGRES_PASSWORD` after the first start does **not** change the database's password; also run `./dl-compose.sh exec postgres psql -U app -d drivinglessons -c "alter user app password '<new>'"`. A new `JWT_SIGNING_KEY` signs the admin out.
