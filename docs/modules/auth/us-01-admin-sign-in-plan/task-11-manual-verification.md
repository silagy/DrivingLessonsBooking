# Task 11 of 11: Manual Verification (acceptance criteria)

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–10 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`.

## Acceptance criteria (issue #2)

Given I open the admin app not signed in and have valid administrator credentials, when I submit my email and password on the login page, then I am signed in and taken to the admin area, which is unreachable without authentication.

## Checklist

Dev mode:

- [ ] 1. `docker run -d --name dl-postgres -p 5432:5432 -e POSTGRES_DB=drivinglessons -e POSTGRES_USER=app -e POSTGRES_PASSWORD=devpassword postgres:17` (skip if already running from Task 6).
- [ ] 2. `dotnet run --project src/DrivingLessons.Presentation.Web` — log shows migration applied; `admin_users` has exactly one row.
- [ ] 3. `cd client && npx ng serve` → open `http://localhost:4200`.
- [ ] 4. **Guard:** deep-link to `http://localhost:4200/` with empty localStorage → redirected to `/login`.
- [ ] 5. **Wrong password:** valid email + wrong password → `auth.invalidCredentials` message; network shows 401 with no field-level detail; still on `/login`.
- [ ] 6. **Right credentials:** `admin@local.dev` / `DevAdmin#2026` → navigated to `/` admin shell; JWT in localStorage. ✅ acceptance criterion.
- [ ] 7. **RTL/i18n:** loads in Hebrew with `dir="rtl"`; toggle → English/LTR; reload preserves choice; layout mirror-correct both ways.
- [ ] 8. **Refresh persistence:** F5 on `/` stays in the admin area.
- [ ] 9. **Logout:** → back at `/login`; deep-link to `/` → redirected again.
- [ ] 10. **No token via API:** `curl http://localhost:5080/api/anything` → 401.

Production mode:

- [ ] 11. `cp .env.example .env`, fill values, `docker compose up --build` → `http://localhost:8080` serves the login page from Kestrel; repeat steps 4–9.
- [ ] 12. Deep-link `http://localhost:8080/` and `/login` directly — SPA fallback serves index.html, Angular router takes over.

## After verification

All tasks complete and verified → finish the branch (merge/PR decision). If any check fails, stop and investigate before committing further work.
