# Task 5 of 8: Nightly Postgres backup script

> Part of [#74: Production Deployment](README.md). Requires task 4 committed. Work on branch `74-production-deployment`, commands from the repo root in bash.

**Why:** Decision 12. The database is the only state in the system (the Excel files are regenerated from it). ADR 0002 accepted "no managed failover" on the condition that nightly dumps bound the loss. A backup that silently produces an empty file is worse than none, so the script must fail loudly and never upload an empty or partial dump.

**Files:**
- Create: `deploy/pg-backup.sh`

**Interfaces:**
- Consumes: task 4 — `dl-compose.sh` beside it, `.env` with `BACKUP_BUCKET`, services `postgres` (db `drivinglessons`, user `app`).
- Produces: `pg-backup.sh` (no arguments). Environment overrides: `BACKUP_DIR` (default `/var/backups/drivinglessons`). Writes `drivinglessons-<UTC yyyy-mm-ddTHHMMSSZ>.dump` (`pg_dump --format custom`), uploads it to `s3://$BACKUP_BUCKET/daily/<same name>` with AWS CLI profile `drivinglessons-backup`, keeps 7 days of local copies, prints one `backup uploaded: ...` line, exits non-zero on any failure. Task 6's runbook installs it in cron; task 8 runs it for real.

- [ ] **Step 1: Write the script**

Create `deploy/pg-backup.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

readonly AWS_PROFILE_NAME=drivinglessons-backup
readonly KEEP_LOCAL_DAYS=7
readonly BACKUP_DIR="${BACKUP_DIR:-/var/backups/drivinglessons}"

export PATH="/snap/bin:$PATH"
cd "$(dirname "$0")"

bucket=$(grep -E '^BACKUP_BUCKET=' .env | cut -d= -f2- | tr -d "\"'" || true)

if [[ -z "$bucket" ]]; then
    echo "BACKUP_BUCKET is not set in .env" >&2
    exit 1
fi

stamp=$(date -u +%Y-%m-%dT%H%M%SZ)
name="drivinglessons-$stamp.dump"
file="$BACKUP_DIR/$name"
partial="$file.partial"
trap 'rm -f "$partial"' EXIT

mkdir -p "$BACKUP_DIR"
./dl-compose.sh exec -T postgres pg_dump --username app --dbname drivinglessons --format custom > "$partial"

if [[ ! -s "$partial" ]]; then
    echo "pg_dump produced an empty file" >&2
    exit 1
fi

mv "$partial" "$file"
aws s3 cp "$file" "s3://$bucket/daily/$name" --profile "$AWS_PROFILE_NAME" --only-show-errors --no-progress
find "$BACKUP_DIR" -name 'drivinglessons-*.dump' -mtime +"$KEEP_LOCAL_DAYS" -delete

echo "backup uploaded: s3://$bucket/daily/$name ($(stat -c %s "$file") bytes)"
```

```bash
git add --chmod=+x deploy/pg-backup.sh
```

Behaviour to check in the review:
- Any failing command (Postgres down, `aws` rejected, disk full) stops the script with a non-zero exit (`set -euo pipefail`), and the `EXIT` trap removes the half-written `.partial` — a failed night leaves nothing that looks like a backup.
- Upload happens only after the dump is complete and non-empty.
- `/snap/bin` is prepended because cron's `PATH` does not include it (the AWS CLI is a snap — decision 13).

- [ ] **Step 2: Bring up the production-like stack again**

Same layout as task 4 Step 6, plus the script and a bucket name:

```bash
SMOKE=.superpowers/sdd/us-74-prod-smoke
rm -rf "$SMOKE" && mkdir -p "$SMOKE"
cp docker-compose.yml deploy/docker-compose.prod.yml deploy/Caddyfile deploy/dl-compose.sh deploy/pg-backup.sh "$SMOKE/"
cat > "$SMOKE/.env" <<'EOF'
POSTGRES_PASSWORD=smoke-db-password
ADMIN_EMAIL=admin@local.dev
ADMIN_PASSWORD='DevAdmin#2026'
JWT_SIGNING_KEY=smoke-only-signing-key-at-least-32-characters-long
EMAIL_ENABLED=false
DOMAIN=localhost
ACME_EMAIL=admin@local.dev
BACKUP_BUCKET=smoke-bucket
EOF
echo "APP_TAG=smoke" > "$SMOKE/release.env"
docker build -t ghcr.io/silagy/drivinglessonsbooking:smoke .
"$SMOKE/dl-compose.sh" up -d
sleep 20
curl -sk -w "  <- %{http_code}\n" https://localhost/api/health
```

Expected: `Healthy  <- 200` (the app has migrated the schema and seeded the admin — there is something to dump).

- [ ] **Step 3: A successful run with a stand-in `aws`**

No AWS here, so put a fake `aws` first on `PATH` that records its arguments:

```bash
mkdir -p "$SMOKE/fake-bin"
cat > "$SMOKE/fake-bin/aws" <<'EOF'
#!/usr/bin/env bash
echo "$*" >> "$(dirname "$0")/aws-calls.log"
EOF
chmod +x "$SMOKE/fake-bin/aws"

PATH="$PWD/$SMOKE/fake-bin:$PATH" BACKUP_DIR="$PWD/$SMOKE/backups" "$SMOKE/pg-backup.sh"; echo "exit: $?"
ls -l "$SMOKE/backups"
cat "$SMOKE/fake-bin/aws-calls.log"
docker exec -i "$("$SMOKE/dl-compose.sh" ps -q postgres)" pg_restore --list < "$(ls "$SMOKE"/backups/*.dump | head -1)" | grep -cE "TABLE DATA public (students|submissions|slot_requests|publications)"
```

Expected: `backup uploaded: s3://smoke-bucket/daily/drivinglessons-<stamp>.dump (<n> bytes)` and `exit: 0`; one non-empty `.dump` and no `.partial`; the log shows `s3 cp <path>/drivinglessons-<stamp>.dump s3://smoke-bucket/daily/drivinglessons-<stamp>.dump --profile drivinglessons-backup --only-show-errors --no-progress`; `pg_restore --list` prints `4` (the archive is a valid custom-format dump holding those tables).

- [ ] **Step 4: Failure runs leave nothing behind**

```bash
: > "$SMOKE/fake-bin/aws-calls.log"
"$SMOKE/dl-compose.sh" stop postgres
PATH="$PWD/$SMOKE/fake-bin:$PATH" BACKUP_DIR="$PWD/$SMOKE/backups-fail" "$SMOKE/pg-backup.sh"; echo "exit: $?"
ls -A "$SMOKE/backups-fail"; wc -l < "$SMOKE/fake-bin/aws-calls.log"
"$SMOKE/dl-compose.sh" start postgres

sed -i 's/^BACKUP_BUCKET=.*/BACKUP_BUCKET=/' "$SMOKE/.env"
PATH="$PWD/$SMOKE/fake-bin:$PATH" BACKUP_DIR="$PWD/$SMOKE/backups-fail" "$SMOKE/pg-backup.sh"; echo "exit: $?"
```

Expected — Postgres stopped: a compose error (`service "postgres" is not running`), `exit: 1`, `backups-fail` empty (no `.dump`, no `.partial`), `0` aws calls. Empty bucket: `BACKUP_BUCKET is not set in .env`, `exit: 1`.

- [ ] **Step 5: Tear down and commit**

```bash
"$SMOKE/dl-compose.sh" down -v
docker image rm ghcr.io/silagy/drivinglessonsbooking:smoke
git add deploy/pg-backup.sh
git commit -m "build(deploy): nightly pg_dump to S3 that never uploads an empty or partial dump

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
