# Task 7 of 8: Deploy workflow, full check, PR

> Part of [#74: Production Deployment](README.md). Requires tasks 1–6 committed. Work on branch `74-production-deployment`, commands from the repo root in bash.

**Why:** Decisions 3, 4, 5. One button in GitHub that tests, builds, ships and proves the release is healthy, with rollback as "run it again with the old sha". It can only be *run* after this PR merges (GitHub offers `workflow_dispatch` only for workflows on the default branch) — task 8 does that.

**Files:**
- Create: `.github/workflows/deploy.yml`

**Interfaces:**
- Consumes: task 3 — `./.github/workflows/ci.yml` with input `ref`; task 4 — files `docker-compose.yml`, `deploy/docker-compose.prod.yml`, `deploy/Caddyfile`, `deploy/dl-compose.sh`, `release.env` format `APP_TAG=<sha>`; task 5 — `deploy/pg-backup.sh`; task 6 — environment `production`, secrets `DEPLOY_HOST`, `DEPLOY_SSH_KEY`, `DEPLOY_KNOWN_HOSTS`, variable `DEPLOY_DOMAIN`, user `deploy`, directory `/opt/drivinglessons`; task 1 — `GET /api/health` → `Healthy`.
- Produces: workflow **Deploy** (manual, input `ref`, default `main`); image `ghcr.io/silagy/drivinglessonsbooking:<full sha>`; the PR for tasks 1–7.

- [ ] **Step 1: Write the workflow**

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy

on:
  workflow_dispatch:
    inputs:
      ref:
        description: Branch, tag or commit sha to deploy (a previous sha rolls back)
        required: true
        default: main

concurrency:
  group: production
  cancel-in-progress: false

permissions:
  contents: read

env:
  IMAGE: ghcr.io/silagy/drivinglessonsbooking

jobs:
  test:
    name: Test
    uses: ./.github/workflows/ci.yml
    with:
      ref: ${{ inputs.ref }}

  image:
    name: Build and push image
    needs: test
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    outputs:
      tag: ${{ steps.sha.outputs.tag }}
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ inputs.ref }}

      - name: Resolve commit sha
        id: sha
        run: echo "tag=$(git rev-parse HEAD)" >> "$GITHUB_OUTPUT"

      - uses: docker/setup-buildx-action@v3

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ env.IMAGE }}:${{ steps.sha.outputs.tag }}
          labels: org.opencontainers.image.source=https://github.com/${{ github.repository }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    name: Deploy to production
    needs: image
    runs-on: ubuntu-latest
    environment:
      name: production
      url: https://${{ vars.DEPLOY_DOMAIN }}
    env:
      APP_TAG: ${{ needs.image.outputs.tag }}
      DOMAIN: ${{ vars.DEPLOY_DOMAIN }}
      TARGET: deploy@${{ secrets.DEPLOY_HOST }}
      SSH_KEY: ${{ secrets.DEPLOY_SSH_KEY }}
      KNOWN_HOSTS: ${{ secrets.DEPLOY_KNOWN_HOSTS }}
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ needs.image.outputs.tag }}

      - name: Prepare SSH
        run: |
          install -m 700 -d ~/.ssh
          printf '%s\n' "$SSH_KEY" > ~/.ssh/id_ed25519
          chmod 600 ~/.ssh/id_ed25519
          printf '%s\n' "$KNOWN_HOSTS" > ~/.ssh/known_hosts

      - name: Copy release files
        run: |
          scp docker-compose.yml deploy/docker-compose.prod.yml deploy/Caddyfile \
              deploy/dl-compose.sh deploy/pg-backup.sh "$TARGET:/opt/drivinglessons/"

      - name: Pull and restart
        run: |
          ssh "$TARGET" "set -e
            cd /opt/drivinglessons
            chmod +x dl-compose.sh pg-backup.sh
            echo APP_TAG=$APP_TAG > release.env
            ./dl-compose.sh pull app
            ./dl-compose.sh up -d --remove-orphans"

      - name: Wait for health
        run: |
          for attempt in $(seq 1 36); do
            if curl -fsS --max-time 5 "https://$DOMAIN/api/health"; then
              echo
              echo "Healthy on $APP_TAG"
              exit 0
            fi
            sleep 5
          done
          echo "Not healthy after 3 minutes - roll back by running Deploy with the previous sha." >&2
          ssh "$TARGET" "cd /opt/drivinglessons && ./dl-compose.sh ps && ./dl-compose.sh logs --tail 100 app"
          exit 1
```

Review notes:
- Secrets reach the shell only through `env:` — never interpolated into `run:` text (no script injection, and GitHub masks them in logs).
- `known_hosts` is pinned from a secret, so a host-key change fails the run instead of trusting a new key.
- `up -d` recreates only `app` when only the tag changed; `postgres` and `caddy` (and Caddy's certificates) are untouched. A missing `caddy:2`/`postgres:17` image is pulled on first run.
- `concurrency: production` queues a second run behind the first instead of interleaving two deploys.

- [ ] **Step 2: Lint both workflows**

```bash
docker run --rm -v "$(pwd -W 2>/dev/null || pwd)":/repo -w /repo rhysd/actionlint:latest -color
```

Expected: no output, exit 0. (actionlint may warn that `vars.DEPLOY_DOMAIN` / the secrets are unknown — it cannot see repository settings; any other finding must be fixed.)

- [ ] **Step 3: Full local check**

```bash
dotnet build DrivingLessons.sln
dotnet test DrivingLessons.sln
cd client
NG='"/c/Program Files/nodejs/node.exe" node_modules/@angular/cli/bin/ng.js'
eval $NG test --watch=false
eval $NG build --project client
cd ..
git status --short
```

Expected: everything green; `git status` shows only `.github/workflows/deploy.yml` untracked.

- [ ] **Step 4: What a failed health gate looks like (reasoned, not run)**

The gate is exercised for real in task 8 Step 5 (a deliberately broken release). Before committing, re-read the `Wait for health` step against these cases and confirm each ends red: app crashes at startup (Caddy answers `502` → `curl -f` fails → 36 tries → exit 1 with logs); database unreachable (`503 Unhealthy` → same); DNS/TLS broken (`curl` error → same). Only a `2xx` from `/api/health` makes it green.

- [ ] **Step 5: Commit and open the PR for review**

```bash
git add .github/workflows/deploy.yml
git commit -m "ci: manual Deploy workflow that ships a sha-tagged image and waits for health

test (ci.yml) -> build and push to GHCR -> copy compose files over SSH -> pull and restart
-> fail unless /api/health answers within 3 minutes. Rollback = run it with an older sha.

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git push
```

Update the draft PR from task 3 and mark it ready:

```bash
cat > .superpowers/sdd/us-74-pr-body.md <<'EOF'
Implements the code side of #74 (production deployment on AWS Lightsail). Plan: `docs/modules/deployment/us-74-production-deployment-plan/README.md`.

## What changes
- **App:** anonymous `GET /api/health` (database check); per-IP rate limits - admin sign-in 5/min, student identify 30/min - answering 429 with `Retry-After` and `code: tooManyRequests`.
- **Client:** login and the identify step show a translated "wait a minute" message on 429 (Hebrew and English).
- **CI:** `ci.yml` runs backend and client tests on every PR and push to `main`.
- **Deploy:** `deploy.yml` (manual) - test, push `ghcr.io/silagy/drivinglessonsbooking:<sha>`, restart over SSH, gate on `/api/health`.
- **Production compose:** `deploy/docker-compose.prod.yml` adds Caddy (HTTPS), restart policies, log rotation, forwarded headers; only Caddy publishes ports. Local `docker compose up` unchanged (app port moved to the override).
- **Ops:** `pg-backup.sh` (nightly dump to a write-only S3 prefix), `bootstrap-server.sh`, `docs/operations/runbook.md`.

## Verified locally
- Unit tests for the 429 response and the client mappings; full `dotnet test` and client test/build green.
- Live: 6th sign-in and 31st identify per minute answer 429; health 503 with Postgres stopped.
- Production overlay smoke on `DOMAIN=localhost`: only Caddy publishes ports, HTTP -> HTTPS, HSTS, spoofed `X-Forwarded-For` does not reset the limit, killed app container restarts.
- Backup script: valid custom-format dump uploaded via a stand-in `aws`; Postgres down -> exit 1, nothing left behind.

## Not in this PR
Provisioning AWS and the first deploy (task 8 of the plan, after merge - `workflow_dispatch` needs the workflow on `main`).

Refs #74

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
gh pr edit 74-production-deployment --body-file .superpowers/sdd/us-74-pr-body.md
gh pr ready 74-production-deployment
```

Read the PR's CI (in the desktop app via the `ccd_pr` tools; do not poll in a loop). **Do not merge** — the human partner reviews and merges. Task 8 starts from `main` after the merge.
