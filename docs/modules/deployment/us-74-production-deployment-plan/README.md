# #74: Production Deployment on AWS Lightsail — Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** v1 runs in production at a newly registered domain over HTTPS on one AWS Lightsail instance. Releases reach it through a manually triggered GitHub Actions workflow that refuses to report success until the app is healthy. Teachers get the window-close email through SES from the school's domain, Postgres is dumped to S3 every night, and the PRD success metric (window close → Excel in the inbox, target under one minute) is measured on the real system. Covers GitHub issue [#74](https://github.com/silagy/DrivingLessonsBooking/issues/74) and closes the PRD [#1](https://github.com/silagy/DrivingLessonsBooking/issues/1).

**Architecture:** Unchanged single deployable ([ADR 0001](../../../decisions/0001-monorepo-single-deployable.md)) on the hosting [ADR 0002](../../../decisions/0002-hosting-aws-lightsail.md) chose: Docker Compose with `postgres`, `app` and — new — `caddy` (HTTPS via Let's Encrypt). The repo's `docker-compose.yml` stays the shared base; a new `deploy/docker-compose.prod.yml` overlay swaps the local build for a GHCR image pinned by `APP_TAG`, adds Caddy, restart policies and log rotation, and turns on ASP.NET Core's forwarded-headers support so the app sees the real client IP. Two GitHub Actions workflows: `ci.yml` (every PR/push, and reusable) and `deploy.yml` (manual; test → build & push → SSH pull & restart → health gate). The only application code: an anonymous `/api/health` endpoint with a database check, per-IP rate limits on admin sign-in and student identify, and the client's translated "too many attempts" messages.

**Tech Stack:** .NET 10 / ASP.NET Core (built-in `Microsoft.AspNetCore.RateLimiting`, health checks + **`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.9**, new, Infrastructure); Angular 21 + Transloco + Vitest; Docker Compose ≥ 2.24 (`!reset`); Caddy 2; GitHub Actions + GHCR; AWS Lightsail (Ubuntu 24.04 LTS, 2 GB), Route 53, SES (SMTP), S3, IAM; AWS CLI v2 (snap) on the host.

**Spec:** issue [#74](https://github.com/silagy/DrivingLessonsBooking/issues/74) (acceptance criteria) · design approved in the planning conversation (recorded as the Decisions table below) · [ADR 0002](../../../decisions/0002-hosting-aws-lightsail.md) · [ADR 0005](../../../decisions/0005-email-over-ses-smtp.md) · [tech-stack.md — Hosting & Operations](../../../tech-stack.md) · PRD [#1](https://github.com/silagy/DrivingLessonsBooking/issues/1) "Further Notes" (the success metric)

**Branch:** `74-production-deployment` (created from `origin/main` at planning time). Tasks 1–7 land in **one PR**; task 8 deploys from `main` after that PR merges (GitHub only offers `workflow_dispatch` for workflows on the default branch) and ships its results in a small second PR.

## Story

*As an administrator, I want the system running at a public HTTPS address with automatic teacher emails and nightly backups, so that students can submit from the WhatsApp link and teachers receive their Excel without anyone running anything by hand.* — full Given/When/Then list in [#74](https://github.com/silagy/DrivingLessonsBooking/issues/74).

## Context

Every v1 story is merged. What exists for production today:
- `Dockerfile` — three-stage build (Angular → `dotnet publish` → `aspnet:10.0` runtime on `:8080`).
- `docker-compose.yml` — `postgres:17` (named volume, healthcheck) and `app` (`build: .`, **publishes `8080:8080`**, all settings from `.env`). `docker-compose.override.yml` publishes Postgres `5432` for local tools.
- `.env.example` — DB password, admin seed, JWT key, `EMAIL_*` for SES SMTP (`EMAIL_ENABLED=false`).
- `Program.cs` — migrates and seeds the admin at startup; fallback authorization policy requires a signed-in user; `[AllowAnonymous]` on `AuthController.Login`, the student controllers, and the SPA fallback.
- Quartz opens/closes windows; `PublicationReconciliationHostedService` catches up on startup — so a restart or reboot is safe by construction.

Missing: Caddy, any CI, a way to ship an image, a health endpoint, rate limiting, backups, and the AWS resources themselves.

## Decisions (approved at planning time — challenge on review)

| # | Decision |
|---|----------|
| 1 | **AWS Lightsail stays** (ADR 0002 reaffirmed; Hetzner, Render/Railway and Azure were weighed again and rejected — familiarity, SES/S3 in one account, budget). Region **Frankfurt `eu-central-1`**, matching `.env.example`'s SES endpoint. 2 GB Ubuntu 24.04 LTS instance, static IP, Lightsail firewall **22, 80, 443** only. |
| 2 | **A new domain registered in Route 53** (human partner's choice). The hosted zone carries the A record, the SES DKIM CNAMEs, SPF and DMARC. The app is served at the apex `https://<domain>`; Caddy redirects HTTP → HTTPS. |
| 3 | **Releases via GitHub Actions, triggered by hand** (human partner's choice over auto-deploy on merge: a merge mid-window must not change the live student form). `deploy.yml` input `ref` (default `main`) → runs `ci.yml` as a reusable workflow → builds and pushes `ghcr.io/silagy/drivinglessonsbooking:<full sha>` → SSHes in as `deploy`, copies the compose files, writes `release.env` (`APP_TAG=<sha>`), pulls and `up -d` → polls `https://<domain>/api/health` for up to 3 minutes and fails otherwise. **Rollback = re-run with the previous sha.** `concurrency: production` prevents overlapping deploys. |
| 4 | **The image lives in GHCR as a public package.** The repo is public, the image holds no secret (all secrets are environment variables), so the server pulls without registry credentials. Visibility is set once by hand after the first push (task 8). |
| 5 | **Server layout:** `/opt/drivinglessons/` holds `docker-compose.yml`, `docker-compose.prod.yml`, `Caddyfile`, `dl-compose.sh`, `pg-backup.sh` (all copied by the deploy), plus `.env` (secrets, `chmod 600`, written by hand, never in GitHub) and `release.env` (written by the deploy). `dl-compose.sh` is the only way compose is invoked there, so `.env`, `release.env` and both files are always combined the same way. |
| 6 | **The prod overlay** (`deploy/docker-compose.prod.yml`): `app.build: !reset null` + `image: ghcr.io/silagy/drivinglessonsbooking:${APP_TAG:?}`; `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (ASP.NET Core's built-in switch: honours `X-Forwarded-For`/`-Proto` from any proxy — safe because the app's port is **not published** in production, so only Caddy can reach it, and Caddy ignores client-sent `X-Forwarded-*`); `caddy:2` with `DOMAIN` and `ACME_EMAIL` from `.env`, ports 80/443 (TCP) and 443/UDP, `caddy_data`/`caddy_config` volumes; `restart: unless-stopped` and `json-file` logs capped at 10 MB × 3 on all three services. The `8080:8080` mapping moves from `docker-compose.yml` to `docker-compose.override.yml` so local runs are unchanged. |
| 7 | **Health:** `GET /api/health` (anonymous, ASP.NET Core health checks with `AddDbContextCheck<DrivingLessonsDbContext>`) → `200 Healthy` / `503 Unhealthy`. It is the deploy gate and the thing to curl when something looks wrong. |
| 8 | **Rate limits** (built-in limiter, fixed 1-minute window, no queue, partitioned by `RemoteIpAddress`): `admin-sign-in` **5/min** on `POST api/auth/login`; `student-identify` **30/min** on `POST api/submissions/by-link/{token}/identify` — generous because many students on one mobile carrier or one school Wi-Fi can share an IP. A rejection is `429` + `Retry-After` + a problem body with `code: "tooManyRequests"`. No global limiter. |
| 9 | **Client messages for 429** are screen-specific rather than a toast: the login form shows `auth.tooManyAttempts`; the identify step gets a new `IdentifyStatus.tooManyAttempts` (warn message `studentForm.identify.tooManyAttempts`, button stays enabled to retry). Both keys in `en.json` and `he.json`. (Refines the conversation's "`errors.tooManyRequests`" — neither screen uses the toast path.) |
| 10 | **CI** (`ci.yml`): on `pull_request`, `push` to `main`, and `workflow_call`. Job `backend`: .NET 10, `dotnet test DrivingLessons.sln -c Release`. Job `client`: Node 24, `npm ci`, `npx ng test --watch=false`, `npm run build`. No database needed — no test touches Postgres. |
| 11 | **Email:** SES in `eu-central-1`, **domain identity** with Easy DKIM and a **custom MAIL FROM** subdomain `mail.<domain>` (SES publishes the DKIM CNAMEs and the MAIL FROM MX + SPF records into Route 53 itself), so both DKIM and SPF align with the From domain; DMARC `_dmarc` TXT `v=DMARC1; p=none; rua=mailto:<admin address>`; **production access requested on day one** (AWS review takes up to ~24 h); SES SMTP credentials into `.env`; `EMAIL_FROM_ADDRESS=noreply@<domain>`. |
| 12 | **Backups:** `deploy/pg-backup.sh` run by the `deploy` user's crontab at **00:30 UTC** → `pg_dump -Fc` via `dl-compose.sh exec -T postgres` → `/var/backups/drivinglessons/` (7 days kept locally) → `aws s3 cp` to `s3://<bucket>/daily/` with the AWS CLI profile `drivinglessons-backup`. Bucket: Block Public Access on, SSE-S3 default encryption, lifecycle rule expires `daily/` after **30 days**. The IAM user `drivinglessons-backup` may only `s3:PutObject` on `arn:aws:s3:::<bucket>/daily/*` — a compromised server cannot read or delete backups. Restores download with the human partner's own credentials. Output goes to syslog (`logger -t drivinglessons-backup`). |
| 13 | **Host bootstrap is a script** (`deploy/bootstrap-server.sh`, run once with sudo): apt upgrade, `unattended-upgrades`, Docker Engine + compose plugin from Docker's apt repo, a 1 GB swap file, the `deploy` user (docker group, the human partner's and the CI's public keys), SSH `PasswordAuthentication no`, `/opt/drivinglessons` and `/var/backups/drivinglessons` owned by `deploy`, the AWS CLI snap. AWS console steps are a runbook, not IaC. |
| 14 | **Operations docs** live in `docs/operations/runbook.md` (setup, deploy, rollback, logs, backup, restore, reboot) — a lasting document, separate from this plan. |

## Global Constraints

- Every secret (DB password, admin password, JWT key, SES SMTP credentials, AWS backup keys, SSH private key) lives only in the server's `.env` / `~/.aws/credentials` or in GitHub environment secrets — never in the repo, never in the image.
- In production only Caddy publishes ports (80, 443 TCP, 443 UDP). Neither `app` (8080) nor `postgres` (5432) is reachable from outside the Docker network.
- Local development is unchanged: `docker compose up --build` still serves `http://localhost:8080`; `dotnet run` and `ng serve` untouched.
- Image name is lowercase: `ghcr.io/silagy/drivinglessonsbooking`. Tags are full commit shas; no `latest` is deployed.
- No comments in C# (only `//given //when //then` in tests); every user-visible string is a translation key in both languages; no en dash, em dash or ellipsis character anywhere (guard tests enforce it — use `-` and `...`).
- No PII in logs, scripts or docs (no real names, national IDs, phones, emails). Placeholder domain in docs: `<domain>`; never commit the real `.env`.
- One new NuGet package only: `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.9 in Infrastructure. No client package, no migration.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **The database is down or still starting when the deploy checks health** → `/api/health` answers `503` (not `200`, not a crash) and the deploy waits, then fails the run instead of reporting success → task 1 Step 9 (stop Postgres → 503), task 7 health loop, task 8 Step 6.
2. **Many students behind one IP** (school Wi-Fi / carrier NAT) identify at once → up to 30 per minute succeed; the 31st sees the translated "wait a minute" message with a retry button, not "not on the roster" and not a dead form → task 1 Step 8, task 2 store test `maps a 429 to tooManyAttempts`, task 8 Step 7.
3. **The server reboots or the app container restarts mid-window** → all three containers come back by themselves, the app reconciles windows that opened/closed while down, the certificate is not re-requested (Caddy volume) → task 4 Step 6 (local), task 8 Step 9 (real reboot).
4. **A backup that silently produces nothing** (Postgres down, disk full, wrong bucket) → the script exits non-zero, uploads nothing empty, and the failure is in syslog → task 5 `set -euo pipefail` + empty-dump guard + Step 4 failure run.
5. **A deploy of a broken image** → the health gate fails the workflow, and re-running with the previous sha restores service → task 7 Step 4, task 8 Step 5 (deliberate rollback drill).

Also pinned: a client faking `X-Forwarded-For` cannot dodge the limit (Caddy overwrites it — task 8 Step 7); HTTP redirects to HTTPS (task 8 Step 4); the 429 body carries `code: "tooManyRequests"` and `Retry-After` (task 1 tests); `docker compose up --build` locally still works without Caddy (task 4 Step 7).

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-health-and-rate-limits.md](task-01-health-and-rate-limits.md) | Backend — `/api/health` with DB check; per-IP rate limits on sign-in and identify with a 429 problem body (TDD) | ✅ own commit |
| 2 | [task-02-client-too-many-attempts.md](task-02-client-too-many-attempts.md) | Client — login and identify show translated "too many attempts" on 429 (TDD) | ✅ own commit |
| 3 | [task-03-ci-workflow.md](task-03-ci-workflow.md) | `.github/workflows/ci.yml` — backend + client jobs, reusable | ✅ own commit |
| 4 | [task-04-production-compose.md](task-04-production-compose.md) | `deploy/docker-compose.prod.yml`, `Caddyfile`, `dl-compose.sh`; port move; `.env.example`; local prod-overlay smoke | ✅ own commit |
| 5 | [task-05-backups.md](task-05-backups.md) | `deploy/pg-backup.sh` + local dry run | ✅ own commit |
| 6 | [task-06-bootstrap-and-runbook.md](task-06-bootstrap-and-runbook.md) | `deploy/bootstrap-server.sh` + `docs/operations/runbook.md` (AWS setup, deploy, rollback, backup, restore) + tech-stack/ADR touch-ups | ✅ own commit |
| 7 | [task-07-deploy-workflow-and-pr.md](task-07-deploy-workflow-and-pr.md) | `.github/workflows/deploy.yml`; full local check; PR for tasks 1–7 | ✅ own commit + PR |
| 8 | [task-08-first-deploy-and-acceptance.md](task-08-first-deploy-and-acceptance.md) | Provision AWS, first deploy, acceptance drills (HTTPS, rollback, rate limit, reboot, backup + restore, end-to-end week with timing); results PR; close #74 and #1 | ✅ results commit + PR |

**Start the long-lead items on day one, before task 1** (they block only task 8): the human partner registers the domain in Route 53, creates the SES domain identity in `eu-central-1`, and submits the SES production-access request — runbook §1 and §4 (written in task 6; the exact console clicks are also in task 8 Steps 1–2, readable now).

## How to Run a Task

1. Confirm you are on branch `74-production-deployment` (task 8: on `main` after the PR merged, then a results branch) and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing. `git add` only the paths each task lists.
4. Check off the `- [ ]` boxes in the task file as you go.

Environment notes (project memory):
- Backend commands from the repo root in bash. From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`) — `dl-postgres` has a stale migration history.
- The default `npm` is broken for installs on this machine: install with nvm's `v22.6.0` npm, build/test with `C:\Program Files\nodejs\node.exe` + the local Angular CLI (exact commands in task 2). CI uses a clean Node 24 and is unaffected.
- Shell scripts under `deploy/` must keep **LF** line endings (task 4 adds a `.gitattributes` rule) — CRLF breaks them on Linux.
- Task 8 is operational: most steps are the human partner's (AWS console, domain purchase, secrets). The agent prepares commands, checks results, and never handles real secrets or real student data.

## Open Items (non-blocking)

1. **No alerting.** A failed backup or a dead app is only visible in syslog / the health endpoint. Cheapest next step: a free dead-man's-switch ping (e.g. healthchecks.io) at the end of `pg-backup.sh`, plus an uptime check on `/api/health`.
2. **No email retry** (US-44 open item 1, unchanged): if SES rejects a send, the admin's Download button is the fallback.
3. **No staging.** Risky changes can be tried with the local prod-overlay smoke (task 4) before a deploy.
4. **Rate-limit numbers are guesses** (5 and 30 per minute). If a real class hits the identify limit, raise `StudentIdentifyPermitsPerMinute`.
5. **SSH is open to the world** (key-only, password login off). If log noise bothers, restrict port 22 in the Lightsail firewall to the human partner's IP and switch the deploy job to a self-hosted runner or Tailscale.

## Target Layout (new/changed this slice)

```
.github\workflows\ci.yml                                              new
.github\workflows\deploy.yml                                          new
.gitattributes                                                        new — LF for deploy\*.sh
deploy\docker-compose.prod.yml                                        new
deploy\Caddyfile                                                      new
deploy\dl-compose.sh                                                  new
deploy\pg-backup.sh                                                   new
deploy\bootstrap-server.sh                                            new
docker-compose.yml                                                    app no longer publishes 8080
docker-compose.override.yml                                           app publishes 8080 locally
.env.example                                                          DOMAIN, ACME_EMAIL, BACKUP_BUCKET
src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj  + HealthChecks.EntityFrameworkCore
src\DrivingLessons.Infrastructure\DependencyInjection.cs              AddHealthChecks().AddDbContextCheck
src\DrivingLessons.Presentation.Web\RateLimiting\RateLimitPolicies.cs     new
src\DrivingLessons.Presentation.Web\RateLimiting\RateLimitingExtension.cs new
src\DrivingLessons.Presentation.Web\Program.cs                        rate limiter, /api/health
src\DrivingLessons.Presentation.Web\Controllers\AuthController.cs     [EnableRateLimiting]
src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs  [EnableRateLimiting]
tests\DrivingLessons.Application.Test\RateLimiting\RateLimitingExtensionTest.cs  new
client\src\app\features\auth\sign-in-error.ts (+ .spec.ts)            new
client\src\app\features\auth\login.component.ts|html                  errorKey signal
client\src\app\features\student-form\domain\identify-status.enum.ts   + tooManyAttempts
client\src\app\features\student-form\state\student-form.store.ts (+ spec)  429 → tooManyAttempts
client\src\app\features\student-form\ui\components\identify-step\*    message + retry
client\public\i18n\en.json · he.json                                  2 new keys
docs\operations\runbook.md                                            new
docs\tech-stack.md · docs\decisions\0002-hosting-aws-lightsail.md     Caddy/GHCR/CI/region notes
docs\development\running-the-project.md                               link to the runbook
```
