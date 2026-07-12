# Task 12 of 12: Manual verification — acceptance walkthrough

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–11 committed. No commit — this task only verifies.

## Shared Context

**Goal:** Prove the three acceptance criteria end-to-end, and prove the task-5 DbContext rename against both a fresh database and one that already carries the US-01 migration.

---

- [ ] **Step 1: Automated checks**

```
dotnet build
dotnet test
```

Expected: build succeeds; all domain tests pass.

- [ ] **Step 2: Migration chain from zero (fresh DB)**

```
docker compose down -v
docker compose up --build -d
docker compose logs app --tail 50
```

Expected: the app boots, applies `InitialCreate` + `AddTeachers` in order, seeds the admin, no errors. This proves the renamed context replays the whole chain from an empty database.

- [ ] **Step 3: Upgrade path (DB that already has US-01)**

```
docker compose down -v
git stash list
```

Check out `main` (US-01 only), start the stack so the DB gets `InitialCreate` applied, then return:

```
git checkout main
docker compose up --build -d
docker compose down
git checkout 3-us-02-04-teachers-module
docker compose up --build -d
docker compose logs app --tail 50
```

Expected: second boot applies **only** `AddTeachers` (check `__EFMigrationsHistory` has both rows), no duplicate-table errors — proves existing production databases survive the rename.

- [ ] **Step 4: US-02 — Admin creates a teacher**

In the browser (Docker stack or dev servers): sign in → Teachers & cars → Add teacher → name + contact email → save.

Expected: success toast; the teacher card appears in the grid with the entered values; `GET api/teachers/find` returns it.

- [ ] **Step 5: US-03 — Admin adds cars with transmission**

On the new teacher's card: Add car → "Corolla White" / "Sedan" / Automatic → save. Repeat with a Manual car and a third car.

Expected: each car appears as a row under the teacher with the right transmission pill; no maximum blocks the third car.

- [ ] **Step 6: US-04 — Admin edits teacher and car details**

1. Edit the teacher → change the contact email → save → the card shows the new email; hard-refresh the page → still shows it (persisted).
2. Click a car row → switch transmission Automatic → Manual and change the name → save → row updates; hard-refresh → persisted.
3. Verify in the DB (optional): `docker compose exec postgres psql -U postgres -d drivinglessons -c "select name, contact_email from teachers; select name, transmission from cars;"`

- [ ] **Step 7: Error-path checks**

1. Sign out → `GET api/teachers/find` (e.g. via Scalar without a token) → 401; navigating to `/teachers` redirects to login.
2. Create a teacher with an invalid email past the client validation (via Scalar) → 409 with `EmailMustBeValid` detail.
3. `PUT api/teachers/{id}/cars/{carId}` with a foreign `carId` → 404.

- [ ] **Step 8: Hebrew pass**

Toggle to עברית and repeat steps 4–6 at a glance — mirror-correct, translated, no missing-key warnings in the console.

- [ ] **Step 9: Close out**

All boxes above checked → the slice satisfies issues #3, #4, #5. Record any deviations discovered during implementation in `docs/modules/teachers/us-02-04-teachers-changes.md`, then open the PR to `main`.
