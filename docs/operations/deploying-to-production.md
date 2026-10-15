# Deploying to Production

The whole path from "nothing in AWS" to the app live at `https://<domain>`, and then how every release after that works. This guide is the map. The exact console clicks and commands live in three places, and each step below links to the right one:

- [aws-account-setup.md](aws-account-setup.md): opening and securing the AWS account.
- [runbook.md](runbook.md): domain, server, email, backups, secrets, releases, rollback, restore.
- The deployment plan for [#74](https://github.com/silagy/DrivingLessonsBooking/issues/74): [docs/modules/deployment/us-74-production-deployment-plan/](../modules/deployment/us-74-production-deployment-plan/README.md), tasks 1-8.

Placeholders: `<domain>`, `<static-ip>` and `<bucket>` are filled in during setup. Real secrets are never written in the repo, in GitHub issues or in a chat with Claude.

## Where things stand

The plan and the AWS account guide are written. **The deployment pipeline itself is not built yet**: tasks 1-7 add the health check, rate limits, CI, the production compose file with Caddy (HTTPS), backups, the server bootstrap script, the runbook and the **Deploy** workflow. Until that PR is merged to `main` there is no Deploy button and nothing to deploy with.

Don't put the local `docker-compose.yml` on a server as a shortcut. It has no HTTPS, publishes the app on port 8080 to the internet, and has no backups.

## The path at a glance

| Phase | What happens | Who | Steps | Time |
|---|---|---|---|---|
| 0. AWS account | Account, advanced features, MFA, budget, Frankfurt allowed and selected | You | [aws-account-setup.md](aws-account-setup.md) | ~1 h, then up to 24 h for activation |
| 1. Long-lead items | Register the domain, start SES verification, request SES production access | You | runbook §1.1 and §1.4 steps 1-3 | Minutes of work, then hours to a day of waiting |
| 2. Build the pipeline | Tasks 1-7 in code, one PR | Claude builds, you review and merge | Plan tasks 1-7 | Several sessions |
| 3. Provision | Server, DNS, bootstrap, backup bucket, secrets, GitHub environment | You, Claude checks | Plan task 8 steps 2-3 (runbook §1.2, 1.3, 1.5-1.7) | 1-2 h |
| 4. First deploy | First release, acceptance drills, clean up, results PR | Both | Plan task 8 steps 4-12 | Half a day, plus a check the next morning |
| 5. Every release after | Press Deploy, watch it go green | You | [Releasing](#phase-5-releasing-a-new-version) below | ~5 min |

Phases 0 and 1 don't depend on any code. **Start them first**, and run phase 2 while you wait for AWS.

## Phase 0: AWS account

Follow [aws-account-setup.md](aws-account-setup.md) to the end, including its checklist in section 9. From then on you work in the project account in Frankfurt, signed in with your Builder ID and MFA, never as root and never in the management account.

## Phase 1: Long-lead items

These have waiting time, so do them as soon as the account is active. The exact steps are in runbook §1.1 and §1.4.

1. **Domain**: Route 53 → Registered domains → Register domain, which also creates the DNS hosted zone. For a TLD Route 53 doesn't sell (such as `.co.il`), buy it at a registrar for that TLD, create a public hosted zone in Route 53 and set the registrar's name servers to the zone's four NS values.
2. **SES email identity**: SES in Frankfurt → Identities → Create identity → Domain `<domain>`, with Easy DKIM and the custom MAIL FROM domain `mail.<domain>`, and publish the DNS records to Route 53.
3. **DMARC record**: a `_dmarc` TXT record in the hosted zone.
4. **SES production access**: SES → Account dashboard → Request production access. AWS reviews it within about 24 hours. Until it's approved, SES only delivers to addresses you have verified.

If a brand-new account is refused for domain registration or SES production access, open a free case in Support Center → Account and billing (see aws-account-setup.md §11).

## Phase 2: Build the pipeline (tasks 1-7)

The code and scripts are built one task at a time, in order, on the branch `74-production-deployment`. Each task file is self-contained.

| Task | Adds |
|---|---|
| 1 | `/api/health` with a database check; per-IP rate limits on admin sign-in and student identify |
| 2 | The client's translated "too many attempts" messages |
| 3 | `.github/workflows/ci.yml`: tests on every PR |
| 4 | `deploy/docker-compose.prod.yml`, `Caddyfile`, `dl-compose.sh` (HTTPS, restart policies, log limits) |
| 5 | `deploy/pg-backup.sh`: the nightly database dump to S3 |
| 6 | `deploy/bootstrap-server.sh` and `docs/operations/runbook.md` |
| 7 | `.github/workflows/deploy.yml` (the Deploy button) and the PR for tasks 1-7 |

How to run it:

1. Start a Claude session in this repo and ask for the next task, for example: *"Execute task 1 of the #74 deployment plan."* Do one task per session.
2. Review each task's commit. Task 7 opens the PR.
3. Wait for CI to go green on the PR, review it, and **merge it to `main`**. GitHub only offers a manual "Run workflow" button for workflows that are on the default branch, so the Deploy workflow can't be used before this merge.

## Phase 3: Provision the server

Follow plan task 8 steps 2-3. They point to these runbook sections:

1. **Server** (runbook §1.2): Lightsail in Frankfurt, Ubuntu 24.04 LTS, 2 GB plan, named `drivinglessons-prod`. Attach a static IP (`<static-ip>`). The firewall allows only SSH 22, HTTP 80, HTTPS 443 TCP and UDP 443. Add a Route 53 **A** record for `<domain>` pointing to `<static-ip>`.
2. **Bootstrap** (runbook §1.3): create a separate SSH key pair for GitHub Actions, copy `deploy/bootstrap-server.sh` to the server and run it once with sudo. It installs Docker, sets up automatic security updates and swap, creates the `deploy` user with your key and the CI key, and turns off password login.
3. **SES SMTP credentials** (runbook §1.4 step 5): create them once SES shows the domain as Verified.
4. **Backup bucket** (runbook §1.5): an S3 bucket with public access blocked, encryption on and 30-day expiry, plus an IAM user `drivinglessons-backup` that can only **write** to it. Configure its key on the server.
5. **Secrets** (runbook §1.6): as `deploy` on the server, write `/opt/drivinglessons/.env` with every key from `.env.example` and real values, then `chmod 600 .env`. Generate the passwords there with `openssl rand`. Keep a copy in your password manager. You type these yourself. Claude only checks that every key is present, without reading the values.
6. **GitHub** (runbook §1.7 steps 1-3): repo Settings → Environments → `production`, with secrets `DEPLOY_HOST`, `DEPLOY_SSH_KEY` and `DEPLOY_KNOWN_HOSTS`, and the variable `DEPLOY_DOMAIN`.

> `POSTGRES_PASSWORD` is used only the first time the database volume is created, both on the server and locally. Choose it before the first deploy. Changing it later needs the extra step in runbook §7.

## Phase 4: First deploy and acceptance

Follow plan task 8 steps 4-12. In short:

1. **First release**: GitHub → Actions → **Deploy** → Run workflow, `ref` = `main`. The first run is expected to **fail** at "Pull and restart", because GitHub creates the image package as private. Make it public (runbook §1.7 step 4: your profile → Packages → `drivinglessonsbooking` → Package settings → Change visibility → Public), then click **Re-run failed jobs**.
2. **Check it's live**: `https://<domain>/api/health` answers `Healthy`, `http://` redirects to `https://`, the certificate is from Let's Encrypt, and you can sign in as admin on your phone.
3. **Drills** (steps 5-10): a deliberately broken release that the health check catches, followed by a rollback; health turning unhealthy while the database is down; rate limits through the real proxy; a full test week end to end with the close-to-inbox time measured (target under one minute); a reboot; a backup and a restore check. All of this uses synthetic test data only.
4. **Clean up** (step 11): empty the database so the school starts fresh. The admin user is created again automatically.
5. **Install the nightly backup** (runbook §1.8), if you didn't already do it in the drills, and check the next morning that it ran.
6. **Results** (step 12): a small PR recording the measurements, which closes #74.

## Phase 5: Releasing a new version

Once phase 4 is done, this is all a release takes.

1. **Pick a quiet moment.** Avoid releasing while a submission window is open, unless the change is needed for that window. The admin dashboard shows which publications are open.
2. **Merge the change to `main`** and make sure CI on `main` is green.
3. **Run Deploy**: GitHub → Actions → **Deploy** → Run workflow → `ref` = `main` (or a specific commit sha) → Run workflow. From a terminal you can do the same with:
   ```bash
   gh workflow run deploy.yml -f ref=main
   ```
   and then follow it with `gh run watch`.
4. **Wait for green.** The workflow runs the tests, builds and pushes `ghcr.io/silagy/drivinglessonsbooking:<sha>`, copies the compose files to the server, restarts, and waits up to 3 minutes for `/api/health`. Database migrations are applied when the app starts.
5. **Check**: open `https://<domain>/api/health` (it should say `Healthy`) and sign in.

### If the run goes red

Production may be running the new image, but unhealthy. **Roll back**: run Deploy again with `ref` = the sha of the last successful release (it's in that run's log as `APP_TAG`, and in `git log`). Every image is kept, so this takes about a minute. See runbook §3.

A rollback can't undo a database migration, because migrations only move forward. If a release's migration has to be reverted, that's a restore from backup (runbook §6).

## Other everyday operations

All of these are in the runbook:

| Need | Runbook |
|---|---|
| See what's running, logs, disk and memory | §4 |
| After a server reboot | §5 |
| Restore or check a backup | §6 |
| Change a secret or setting in `.env` | §7 |

On the server, always use `./dl-compose.sh` in `/opt/drivinglessons` instead of plain `docker compose`. It combines `.env`, `release.env` and both compose files the same way the deploy does.

## Never do this

- `./dl-compose.sh down -v` on the server. `-v` deletes the database and the HTTPS certificates.
- Edit `docker-compose*.yml`, `Caddyfile` or the scripts in `/opt/drivinglessons` by hand. The next deploy overwrites them. Change them in the repo and release instead.
- Commit `.env`, or paste a secret into an issue, a PR or a chat with Claude.
- Deploy from your PC with `docker compose up` against the server.
