# Task 8 of 8: Provision, first deploy, acceptance drills, results

> Part of [#74: Production Deployment](README.md). Requires the tasks 1–7 PR **merged to `main`**. Operational task: steps marked **(human)** are done by the human partner in the AWS console, GitHub settings or on their phone; the agent prepares commands, reads results back, and records them. The agent never types a real secret, never views `.env`, and never enters real student or teacher data — test data below is synthetic.

**Why:** The acceptance criteria of #74 can only be proven on the real system, and the PRD's success metric (window close → Excel in the teacher's inbox, target under one minute) is the reason the system exists.

**Files:**
- Create: `docs/modules/deployment/us-74-production-deployment-changes.md` (results, on a new branch `74-deployment-results` from `main`)
- No source change. Placeholders: `<domain>`, `<static-ip>`, `<bucket>` — the real values go in the results file only where noted (domain and IP are public anyway; never a secret).

**Interfaces:**
- Consumes: everything from tasks 1–7; the runbook `docs/operations/runbook.md` (section numbers below refer to it).
- Produces: v1 live at `https://<domain>`; a results document; #74 and #1 closed.

- [ ] **Step 1: Long-lead items (human — ideally already started on day one)**

Runbook §1.1 (register the domain) and §1.4 steps 1–3 (SES identity with custom MAIL FROM, DMARC record, production-access request). Record in the results file: domain registered at `<time>`, SES identity verified at `<time>`, production access granted at `<time>` (or "still sandboxed" — then Step 8 uses a verified recipient address).

- [ ] **Step 2: Instance, DNS, host (human, agent verifies)**

Human: runbook §1.2 and §1.3. Agent checks:

```bash
nslookup <domain>                                  # answers <static-ip>
ssh deploy@<static-ip> "docker compose version --short; free -h | grep -i swap; sudo -n true 2>&1 | head -1; ls -ld /opt/drivinglessons /var/backups/drivinglessons"
ssh -o PreferredAuthentications=password -o PubkeyAuthentication=no deploy@<static-ip> true 2>&1 | tail -1
```

Expected: the IP; a compose version ≥ 2.24; a 1 GB swap line; `sudo` refused for `deploy` (it is not a sudoer — it only needs docker); both directories owned by `deploy`; the password attempt ends with `Permission denied (publickey)`.

- [ ] **Step 3: Bucket, secrets, GitHub (human)**

Runbook §1.5, §1.6, §1.7 steps 1–3. Agent checks only what is safe to see:

```bash
ssh deploy@<static-ip> "stat -c '%a %U' /opt/drivinglessons/.env; grep -cE '^(POSTGRES_PASSWORD|ADMIN_EMAIL|ADMIN_PASSWORD|JWT_SIGNING_KEY|EMAIL_ENABLED|EMAIL_HOST|EMAIL_PORT|EMAIL_USERNAME|EMAIL_PASSWORD|EMAIL_FROM_ADDRESS|EMAIL_FROM_NAME|DOMAIN|ACME_EMAIL|BACKUP_BUCKET)=.+' /opt/drivinglessons/.env"
gh api repos/silagy/DrivingLessonsBooking/environments/production/secrets --jq '.secrets[].name'
gh api repos/silagy/DrivingLessonsBooking/environments/production/variables --jq '.variables[] | .name + "=" + .value'
```

Expected: `600 deploy`; `14` (every key present and non-empty — the agent counts, never prints values); secrets `DEPLOY_HOST`, `DEPLOY_KNOWN_HOSTS`, `DEPLOY_SSH_KEY`; variable `DEPLOY_DOMAIN=<domain>`.

- [ ] **Step 4: First deploy and HTTPS**

Human (or agent with the human's go-ahead): GitHub → Actions → Deploy → Run workflow, `ref` = `main`. The first run is expected to fail at `Pull and restart` with a GHCR `denied`/`unauthorized` error, because a new package starts private — human: runbook §1.7 step 4 (make the package public), then **Re-run failed jobs**.

When green:

```bash
curl -s -w "  <- %{http_code}\n" https://<domain>/api/health
curl -s -o /dev/null -w "http -> %{http_code} %{redirect_url}\n" http://<domain>/
curl -sI https://<domain>/ | grep -iE "^HTTP|strict-transport"
curl -svo /dev/null https://<domain>/ 2>&1 | grep -iE "issuer|expire date"
ssh deploy@<static-ip> "cat /opt/drivinglessons/release.env; cd /opt/drivinglessons && ./dl-compose.sh ps --format 'table {{.Service}}\t{{.Status}}\t{{.Ports}}'"
```

Expected: `Healthy  <- 200`; `http -> 308 https://<domain>/`; `HTTP/2 200` with HSTS; issuer `Let's Encrypt`, expiry about 90 days out; `APP_TAG=<the main sha>`; three services up, only Caddy with published ports. Human: open `https://<domain>` on a phone and sign in as admin.

- [ ] **Step 5: Rollback drill (no window open, before go-live)**

Prove that a broken release fails the gate and that the previous sha restores service:

```bash
git switch -c 74-rollback-drill main
sed -i 's/^app.Run();$/throw new InvalidOperationException("rollback drill");/' src/DrivingLessons.Presentation.Web/Program.cs
git commit -am "drill: an app that never starts (never merged)"
git push -u origin 74-rollback-drill
```

Run Deploy with `ref` = `74-rollback-drill` → expected **red** at `Wait for health` after ~3 minutes, with the app's crash in the printed logs; `https://<domain>/api/health` answers `502`. Run Deploy with `ref` = the `main` sha from Step 4 → **green**; health `200`. Record both run URLs. Clean up:

```bash
git switch main
git push origin --delete 74-rollback-drill
git branch -D 74-rollback-drill
```

(The drill image stays in GHCR — harmless; it is never deployed again.)

- [ ] **Step 6: Health reflects the database**

```bash
ssh deploy@<static-ip> "cd /opt/drivinglessons && ./dl-compose.sh stop postgres"
curl -s -w "  <- %{http_code}\n" https://<domain>/api/health
ssh deploy@<static-ip> "cd /opt/drivinglessons && ./dl-compose.sh start postgres"
sleep 10
curl -s -w "  <- %{http_code}\n" https://<domain>/api/health
```

Expected: `Unhealthy  <- 503`, then `Healthy  <- 200`.

- [ ] **Step 7: Rate limits through the real proxy**

```bash
for i in 1 2 3 4 5 6; do
  curl -s -o /dev/null -w "login $i: %{http_code}\n" -X POST https://<domain>/api/auth/login \
    -H "X-Forwarded-For: 198.51.100.$i" -H "Content-Type: application/json" \
    -d '{"email":"nobody@example.com","password":"wrong-password"}'
done
```

Expected: 1–5 `401`, 6 `429` (the spoofed header does not help). Human, on the phone, on mobile data (a different IP from the agent's): sign in normally → works (other IPs unaffected). Within the same minute, from the agent's machine, the browser login page shows "Too many sign-in attempts..." / the Hebrew text.

- [ ] **Step 8: End-to-end week and the success metric (synthetic data)**

Human, as admin on `https://<domain>`:
1. Create teacher **"Test Teacher"** with the human's **own** mailbox as contact email (in sandbox: an address verified in SES).
2. Upload a synthetic roster CSV in the format of `docs/modules/roster/us-49-roster-plan` with two fake students bound to "Test Teacher" (fake names and phones; national IDs `000000018` and `000000026`).
3. Prepare the teacher's week schedule for the **next** week, mark one slot Unavailable, publish with a window that starts now and **ends 10 minutes from now**; note the exact end time (`T_end`, Asia/Jerusalem).
4. On the phone, open the weekly link, identify as `000000018`, pick two slots, submit; edit once (US-43).
5. Wait for the close. When the email arrives, note the receive time shown by the mail client and the `Date:` header (`T_inbox`).

Expected: the link turns read-only at `T_end`; one email with subject `בקשות לשבוע <n> - Test Teacher - גרסה 1`, the two-sheet Excel attached, DKIM/SPF/DMARC **pass** in the message's authentication results (Gmail: "Show original"; Outlook: message headers). **Metric = `T_inbox − T_end`**, target < 60 s. Record it.

Also reopen the window for 3 more minutes (US-19) and let it close again → a `גרסה 2` email; record its delay too.

- [ ] **Step 9: Reboot**

Human (as `ubuntu` with the Lightsail key — `deploy` is not a sudoer): `sudo reboot`. After ~60 s:

```bash
curl -s -w "  <- %{http_code}\n" https://<domain>/api/health
ssh deploy@<static-ip> "cd /opt/drivinglessons && ./dl-compose.sh ps --format 'table {{.Service}}\t{{.Status}}' && ./dl-compose.sh logs caddy --since 5m | grep -ciE 'obtaining certificate|certificate obtained' || true"
```

Expected: `Healthy  <- 200`; three services up; `0` certificate-issuance lines (the certificate was reused from the `caddy_data` volume).

- [ ] **Step 10: Backup, write-only proof, restore drill**

Human: runbook §1.8 (install the cron line). Then:

```bash
ssh deploy@<static-ip> "/opt/drivinglessons/pg-backup.sh"
ssh deploy@<static-ip> "aws s3 ls s3://<bucket>/daily/ --profile drivinglessons-backup" ; echo "exit: $?"
```

Expected: `backup uploaded: s3://<bucket>/daily/drivinglessons-<stamp>.dump (<n> bytes)`; the `ls` fails with `AccessDenied` (the server cannot read backups). Human: S3 console shows the object, encrypted (SSE-S3).

Restore drill — runbook §6 steps 1–3 with that dump (it holds Step 8's synthetic data). Expected: the four counts for `drivinglessons` and `restore_check` are equal (2 students, 1 publication, 1 submission, its slot requests). Record them.

Next morning: `ssh deploy@<static-ip> "journalctl -t drivinglessons-backup --since yesterday"` shows the 00:30 UTC run's `backup uploaded` line.

- [ ] **Step 11: Clean the synthetic data before go-live**

The system has no real users yet, so start production from an empty database (the admin is re-seeded on start):

```bash
ssh deploy@<static-ip> "cd /opt/drivinglessons && ./dl-compose.sh stop app \
  && ./dl-compose.sh exec -T postgres dropdb -U app drivinglessons \
  && ./dl-compose.sh exec -T postgres createdb -U app drivinglessons \
  && ./dl-compose.sh start app"
sleep 20
curl -s -w "  <- %{http_code}\n" https://<domain>/api/health
```

Expected: `Healthy  <- 200`; the admin signs in and sees no teachers, no publications. (S3 keeps the synthetic dumps for 30 days — harmless.)

- [ ] **Step 12: Results document, PR, close the issues**

```bash
git switch -c 74-deployment-results main
```

Create `docs/modules/deployment/us-74-production-deployment-changes.md`:

```markdown
# #74 Production Deployment — What Shipped and What We Measured

**Live:** https://<domain> (Lightsail Frankfurt, static IP <static-ip>), first deployed <date> from `<sha>`.

## Success metric (PRD #1)

| Close | Window end (Asia/Jerusalem) | In inbox | Delay | Target |
|---|---|---|---|---|
| v1 | <T_end> | <T_inbox> | <n> s | < 60 s |
| v2 (after reopen) | <T_end> | <T_inbox> | <n> s | < 60 s |

Email authentication: DKIM <pass>, SPF <pass>, DMARC <pass>. SES production access: <granted on date / sandbox>.

## Acceptance drills

| Drill | Result | Evidence |
|---|---|---|
| HTTPS, HTTP→HTTPS, HSTS, Let's Encrypt | <pass> | Step 4 |
| Broken release fails the gate; previous sha restores | <pass> | run URLs |
| Health 503 with the database down | <pass> | Step 6 |
| Rate limits through Caddy, spoofed XFF ignored, other IPs unaffected | <pass> | Step 7 |
| Reboot: all containers back, certificate reused | <pass> | Step 9 |
| Backup uploaded; server cannot read the bucket; restore counts match | <pass> | Step 10 |

## Deviations from the plan

<none, or each one with the reason>
```

Fill every `<...>` with what was observed (domain and IP may be written; no secret, no personal data). Then:

```bash
git add docs/modules/deployment/us-74-production-deployment-changes.md
git commit -m "docs(ops): record the first production deployment and the measured close-to-inbox time

Closes #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git push -u origin 74-deployment-results
gh pr create --base main --title "Record the first production deployment (#74)" \
  --body "Results of task 8 of the deployment plan: the measured close-to-inbox time against the PRD target and every acceptance drill. Closes #74.

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

After the human partner merges it, and **only with their go-ahead**, close the PRD with the metric:

```bash
gh issue close 1 --comment "v1 is live (#74). Measured window close to Excel in the inbox: <n> s (target under 60 s). Details: docs/modules/deployment/us-74-production-deployment-changes.md"
```
