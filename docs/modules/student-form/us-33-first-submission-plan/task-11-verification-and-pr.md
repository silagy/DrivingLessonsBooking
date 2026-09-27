# Task 11 of 11: End-to-end verification (first submission, edits, rejections — HE + EN, 375px) + roadmap link + PR

> Part of [US-33…US-41: First Submission](README.md). Requires tasks 1–10 committed and the task-5/6 smoke API still available. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`.

**Files:**
- Modify: `docs\modules\student-form\README.md` (link this plan from the slice-3 row)
- Local only, never staged: `.claude\launch.json` (remove the `api-smoke` entry at the end)

**Interfaces:**
- Consumes: the `drivinglessons_us33_smoke` database, teachers, roster and publications from task 5 Steps 3–4 (`LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `COHEN_ID`, `LEVI_ID`); the client from tasks 7–10.
- Produces: verified slice + PR closing #33, #34, #35, #36, #37, #39, #40, #41.

- [ ] **Step 1: Bring up the stack**

1. `api-smoke` running against `drivinglessons_us33_smoke` (task 5 Step 3 / task 6 Step 4). In a bash shell, re-declare `API`, `json`, log in for `AUTH`, and set `LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `COHEN_ID`, `LEVI_ID` to the values written down in task 5 (if lost: `curl -s "$API/api/publications/by-week?week=2026-10-04" -H "$AUTH"` → `id`, `linkToken`; same for `2026-10-11`; teacher ids from `GET api/teachers/find`).
2. Week 1 must be open: `curl -s $API/api/submissions/by-link/$LINK | json state` → `open`. If it closed (its window was 3 hours), reopen it:
   ```bash
   curl -s -o /dev/null -w "reopen week 1: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
     -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
   ```
3. Two first-time students for the browser (synthetic IDs with valid check digits; D stays off the roster):
   ```bash
   ROSTER_DIR=$(mktemp -d)
   cat > "$ROSTER_DIR/roster-e2e.csv" <<'CSV'
   שם מלא,תעודת זהות,טלפון,מורה,רכב
   Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
   Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
   Smoke Student E,000000067,050-0000005,Smoke Cohen,Smoke Auto
   Smoke Student F,000000075,050-0000006,Smoke Levi,Smoke Manual
   CSV
   grep -v 000000075 "$ROSTER_DIR/roster-e2e.csv" > "$ROSTER_DIR/roster-without-f.csv"
   curl -s -w "  ← import: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
     -F "file=@$ROSTER_DIR/roster-e2e.csv;type=text/csv"
   ```
   Expected: `201` with `"added":2,"updated":2,"deactivated":0,"failed":0` (D was already inactive).
4. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080).
5. Browser pane: `resize_window` preset **mobile** (375×812), reload. Start **logged out** (`localStorage.removeItem('auth_token')`) and in **Hebrew** (the default).

Verify with `get_page_text`, `read_page`, `find`, `javascript_tool` and `read_network_requests` (project memory: `screenshot` can hang on this PrimeNG app — `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence if it still times out). Reload `/s/{LINK}` between students so each starts with a fresh store.

Students: **A** `000000018` (Cohen, has a submission from task 5) · **B** `000000026` (Levi, has one) · **E** `000000067` (Cohen, first-timer) · **F** `000000075` (Levi, first-timer; Levi's Sunday morning is blocked) · **D** `000000042` (deactivated).

- [ ] **Step 2: Student E, Hebrew — the whole first submission (US-33, US-35, US-36, US-37, US-39, US-41)**

1. `/s/{LINK}` → `שלב 1 מתוך 5`. Type `000000067` → `מצאנו אתכם — Smoke Student E.` → `המשך` → details `שלב 2 מתוך 5`, **Smoke Cohen** → `המשך`.
2. **Target** (US-33): `שלב 3 מתוך 5`, heading `כמה שיעורים תרצו השבוע?`, the count reads `1`, the `−` button is disabled (`document.querySelector('.target__decrease').disabled` → `true`), caption `שבוע 41 · … · Smoke Cohen`, `שיעורים · מינימום 1`. Press `+` → `2`. Press `חזרה` → details; `המשך` → the target still reads `2` (Back keeps it). `לבחירת שעות`.
3. **Slots**: `שלב 4 מתוך 5`, heading `בחרו שעות, המועדפת קודם`, footer `יעד: 2 · נבחרו: 0`, `לסקירת הרשימה` disabled.
4. Tap `ראשון` · `אחה״צ` → the sheet slides in: rank `1`, title `ראשון · אחה״צ`, `15:00–18:00`, `יחיד` selected, empty constraint, focus on `יחיד` (`document.activeElement.classList.contains('pick-sheet__single')` → `true`). Choose `כפול`, type `רק אחרי 16:00`, press `הוספה כבחירה מס׳ 1` → the chip turns sky with a `1` badge at its **top-left** corner (inline end in RTL).
5. Add `שני` · `ערב` and `רביעי` · `אחה״צ` (both `יחיד`) → badges `2`, `3`; footer `יעד: 2 · נבחרו: 3 ✓ … בחירות נוספות = גיבוי` (US-39: extra picks are allowed and shown as backups).
6. `לסקירת הרשימה` → **Review** `שלב 5 מתוך 5`, `הרשימה המדורגת שלכם`, `יעד: 2 · נבחרו: 3 דירוגים 1–2 הם השעות המועדפות שלכם; השאר הן גיבוי.`; three rows in order; rows 1–2 have the gradient rank and sky border, row 3 grey; row 1 shows the pill `כפול` and the constraint in quotation marks; **no** "replaces" notice.
7. `שליחה` → `read_network_requests` (`urlPattern: "by-link"`): one `POST /api/submissions/by-link/{LINK}` → **204**, payload `{"nationalId":"000000067","targetCount":2,"slotRequests":[{"slotId":…,"sessionType":"double","constraint":"רק אחרי 16:00"},{…"single",null},{…"single",null}]}`.
8. **Confirmation** (US-41): heading `נשלח`, `3 בחירות לשבוע 41 אצל Smoke Cohen. אפשר לערוך אותן עד {day, date, time} — פשוט פתחו שוב את הקישור והזינו את מספר תעודת הזהות.`, button `עריכת ההגשה`, **no caption bar** (`document.querySelector('.student-shell__caption')` → `null`).
9. What the server holds:
   ```bash
   docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us33_smoke -c "
   select r.rank, r.session_type, r.constraint_text, s.target_count
   from submissions s join students st on st.id = s.student_id join slot_requests r on r.submission_id = s.id
   where st.name = 'Smoke Student E' order by r.rank;"
   ```
   Expected: ranks 1–3, `session_type` 20 / 10 / 10, `constraint_text` `רק אחרי 16:00` on rank 1 only, `target_count` 2.

- [ ] **Step 3: Every other path**

1. **Edit from the confirmation** (Review Focus 5) — press `עריכת ההגשה` → review with the same three rows **and** the notice `כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.`; press `חזרה` → slots, remove pick `שני · ערב` (tap it → `הסרת הבחירה`) → `רביעי` becomes `2`; review → `שליחה` → the network list shows a **`PUT`** → 204 (never a second POST); confirmation now says `2 בחירות…`.
2. **Unavailable is unselectable** (US-34) — reload, identify **F** (`000000075`, Levi) → … → slots: the first `ראשון` chip is striped, `document.querySelector('[data-slot-id]').disabled` → `true`; tapping it opens nothing.
3. **Not enough picks** (US-40, Review Focus 8) — still F: target `3`, pick two slots → review shows `אין מספיק בחירות.` / `היעד שלכם הוא 3, אבל בחרתם 2. הוסיפו עוד 1, או הורידו את היעד.`, `שליחה` disabled, `השליחה תיפתח כשיהיו לכם 3 בחירות.`. Press `שינוי היעד` → target `3` → `−` → `2` → `לבחירת שעות` → the two picks are still ranked `1`, `2` → review → no error → `שליחה` → `POST` 204.
4. **Returning student** (Review Focus 5) — reload, identify **A** → … → pick one slot → review shows the "replaces" notice → `שליחה` → **`PUT`** 204. The task-5 list is replaced (psql for A → one slot request).
5. **Deactivated between identify and submit** (Review Focus 6) — reload, identify **F** → … → review (do not submit). In the shell: `curl -s -X POST $API/api/roster-imports -H "$AUTH" -F "file=@$ROSTER_DIR/roster-without-f.csv;type=text/csv"` (→ `"deactivated":1`). Press `שליחה` → `PUT` **404** → `לא מצאנו יותר את הפרטים שלכם ברשימת התלמידים או את השעות של השבוע. פנו לבית הספר.`, `שליחה` disabled. Restore: re-upload `roster-e2e.csv` (→ `"updated":4`, F reactivated).
6. **Window closes mid-submit** (Review Focus 4) — reopen week 2 for three minutes:
   ```bash
   curl -s -o /dev/null -w "reopen week 2: %{http_code}\n" -X POST $API/api/publications/$PUB2_ID/reopen -H "$AUTH" \
     -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+3 minutes' +%Y-%m-%dT%H:%M:%SZ)\"}"
   ```
   In the browser open `/s/{LINK2}`, identify **E**, go through to the review with one pick, **stop**. Wait until `curl -s $API/api/submissions/by-link/$LINK2 | json state` → `closed`. Press `שליחה` → `POST` **409** → `לא הצלחנו לשמור את הרשימה — ייתכן שחלון ההגשה נסגר או שהשעות של השבוע השתנו.` + `בדיקת הטופס מחדש`, `שליחה` disabled. Press `בדיקת הטופס מחדש` → slice 1's closed screen `ההגשה נסגרה`.
7. **The sheet never shows a previous slot** (Review Focus 10) — on any grid: tap a slot, type `abc`, close with **Escape**; tap another slot → its own title and time, empty constraint, `document.querySelectorAll('.pick-sheet').length` → `1`. Tap a slot and close by tapping the dark backdrop → closed, nothing added.
8. **Constraint cap** (Review Focus 7) — in a sheet, focus the constraint and use `computer` `type` with 250 characters → `document.querySelector('#pick-constraint').value.length` → `200`. Save; the review row wraps the long text inside the card (`document.documentElement.scrollWidth <= window.innerWidth` → `true`).
9. **PII** — `read_network_requests` (`urlPattern: "by-link"`): every POST/PUT URL is `/api/submissions/by-link/{token}` with no digits of an ID; the ID appears only in request payloads. `javascript_tool`: `Object.entries(localStorage)` → only `app_lang` (and `auth_token` if signed in) — no national ID, no picks; `sessionStorage.length` → `0`; `location.href` → `/s/{token}` only.
10. **Stale admin session** — `localStorage.setItem('auth_token', 'not-a-real-jwt')`, reload, submit as E (edit) → `PUT` 204, no redirect to `/login`. Remove the key.

- [ ] **Step 4: Hebrew/RTL mechanics at 375px** (`javascript_tool`)

On the target, slots (with the sheet open and closed), review and confirmation screens:
- `document.documentElement.dir` → `"rtl"`; `document.documentElement.scrollWidth <= window.innerWidth` → `true`.
- Touch targets ≥ 40px: `.target__step` (48), every `.slot-chip`, `.pick-sheet__option`, the sheet's buttons, `.wizard-step__back button`, and the footer button — e.g. `[...document.querySelectorAll('.slot-chip, .pick-sheet__option, .pick-sheet button, .wizard-step__back button, .wizard-step__footer button')].every(x => x.getBoundingClientRect().height >= 40)` → `true`.
- Target: `−` at the **right**, `+` at the **left** (flex follows `dir`).
- Slots: `ראשון` chips start at the right edge; each rank badge's `getBoundingClientRect().left` is left of its chip's left edge + 10px (top-left corner in RTL); times read `07:00–12:00` left-to-right.
- Sheet: anchored to the bottom (`.pick-sheet` bottom = viewport bottom), full width at 375px, rounded top corners; the rank badge and title at the right, the time at the left.
- Review: rank circles at the right; the constraint's quotation marks follow Hebrew typography; `Back` label `חזרה` sits at the left of the counter row.

- [ ] **Step 5: English**

On the slots step press `EN` → `dir` becomes `"ltr"`, the page **stays on the step with the picks and badges** (`Step 4 of 5`, `Pick your slots, best first`, `Target: 2 · Picked: …`), the badge moves to the top-right corner. Open a sheet → `Add as pick #n` / `Single` / `Double (2×)`. Reload and walk E's edit once in English: `How many lessons do you want this week?` → picks → `Your ranked list` → `Submit` → `Submitted` / `N picks for week 41 with Smoke Cohen. You can edit them until …`. Check the not-enough message once in English (`Not enough picks.`).

- [ ] **Step 6: Desktop and admin**

- `resize_window` preset `desktop`: the shell is the centered 30rem column; the sheet is 30rem wide and centered, not edge to edge.
- Sign in as the admin (`admin@local.dev` / `DevAdmin#2026`, the dev seed), open **Publications** → week 41 → **Smoke Cohen**: *students submitted* and *total picks* match `curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$COHEN_ID" -H "$AUTH"`, *last submission* shows a time, and the grid's cells show the per-slot counts (a Double counts 1). **Smoke Levi** likewise. (Refresh the page to see new submissions — requirements §6.3.)
- Sign out and reset the viewport to `desktop`.

If any check fails, fix it in the owning task's files (with a failing spec or test first where the behavior is testable), re-run the suites, and commit the fix separately before continuing.

- [ ] **Step 7: Link this plan from the roadmap**

In `docs\modules\student-form\README.md`, change the slice-3 table row's first cell from `3. First submission` to:

```markdown
3. First submission — [plan](us-33-first-submission-plan/README.md)
```

- [ ] **Step 8: Full check + commit**

```bash
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES / builds clean, no new warnings, `No changes have been made to the model since the last migration.`

```bash
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 3 plan from the roadmap"
```

- [ ] **Step 9: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server.
2. Remove the `api-smoke` entry from `.claude\launch.json` (restore the file to its pre-task-5 local state; never stage it).
3. Drop the throwaway database — it only holds this slice's smoke data: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us33_smoke`.

- [ ] **Step 10: Push and open the PR** (only once your human partner has approved pushing)

The branch was created from `origin/main` and tracks it; `-u` re-points it at its own remote branch.

```bash
git push -u origin 33-us-33-34-35-36-37-39-40-41-first-submission
gh pr create --title "US-33…US-41: Student submits a ranked weekly list" --body "Closes #33
Closes #34
Closes #35
Closes #36
Closes #37
Closes #39
Closes #40
Closes #41

## Summary
- \`Submission\` aggregate with ranked \`SlotRequest\`s (Single/Double, optional 200-character constraint): only open windows, active roster students, their own teacher's grid, open slots, one request per slot and picks ≥ target are accepted; \`Revise\` replaces the whole list. New \`AddSubmissions\` migration.
- Anonymous \`POST\` / \`PUT api/submissions/by-link/{token}\` (national ID in the body only, 204); unknown links, students, grids and foreign slots 404, every rule 409; no response or error carries an id or the national ID.
- Identify now reports \`hasSubmission\`, so the form creates or replaces; the admin dashboard and Excel summary count real slot requests (a double counts once).
- The student form is five steps: target stepper (no upper limit), day-by-day chips with rank badges and a bottom sheet per pick, the ranked review with the not-enough-picks message, and a confirmation that explains editing via the same link and ID.
- Verified in Hebrew (RTL) and English at 375px, including edits, a returning student, a student removed from the roster mid-flow and a window that closed mid-submit.

## Notes
- A returning student starts from an empty list in this slice and the review says submitting replaces their earlier one; slice 5 loads the existing picks.
- The client maps statuses only; a rejected submit offers \"check the form again\", which shows the closed screen when the window closed. Slice 5 adds the dedicated window-closed screen.
- Back navigation on the target, grid and review steps is an addition to the mockup (one route, so the browser Back would leave the form).

## Test plan
- [x] Domain: value objects (target, constraint, rank, pick) and every \`Submission\` rule incl. revise
- [x] Application: create/revise interactors — ordering, not-found paths, already exists, unavailable/foreign slot, closed window, target and constraint limits
- [x] API smoke on a throwaway DB: all 204/400/404/409 outcomes, window closing mid-submit, stored ranks/types/constraints, dashboard counts, hasSubmission
- [x] Client (Vitest): pick/constraint/target/review models, API bodies, target stepper, picking + sheet, review, submit POST vs PUT, error states, double-tap guard
- [x] Browser: full first submission and every edge path in HE + EN at 375px, RTL mechanics, PII absent from URLs and storage, admin dashboard counts

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-33 AC (target ≥ 1, no upper limit, carried on the Submission) → task 1 `TargetSessionCount`, task 2 `Create`, task 5 check 1/9, task 8 stepper spec (31 presses, never below 1), Step 2.2.
  - US-34 AC (Unavailable visibly blocked and unpickable; Open selectable) → task 2 `Create__Slot_Must_Be_Open`, task 4 `Unavailable_Slot_Is_Rejected`, task 5 check 6, task 9 disabled chip spec, Step 3.2.
  - US-35 AC (Double carried on the Slot Request; Excel detail later) + note (Double counts one) → task 2 `Keeps_Session_Type…`, task 5 Step 7 table, task 6 Step 4 item 3, task 9/10 specs, Step 2.4/2.9. The Excel detail sheet itself is the Excel module's (README open item 6).
  - US-36 AC (constraint on that Slot Request only) → same tests as US-35, task 5 Step 7 (constraint on its own row), task 9 "keeps Double and the constraint on that pick only".
  - US-37 AC (one ranked list in selection order) → task 2 `Ranks_Slot_Requests_In_Selection_Order`, task 4 `Adds_The_Submission_Ranked_In_Request_Order`, task 9 "numbers picks in the order they are tapped", task 10 request body spec.
  - US-39 AC (target 2 + 5 picks accepted as ranks 1–5) → task 2 `Accepts_Extra_Picks_As_Backups`, task 5 check 3, task 10 "lists the picks…" (backups grey), Step 2.5.
  - US-40 AC (target 3 + 2 picks blocked with a clear message) + siblings (target ≥ 1, slot once, ID on the roster) → task 2 `Picks_Must_Cover_Target` (3, 2), task 5 checks 5/8/9/14, task 10 "blocks a list shorter than the target", Step 3.3.
  - US-41 AC (confirmation: received, editable via the same link + ID until close) → task 10 "confirms…", "edits from the confirmation…", Step 2.8 / 3.1.
  - Roadmap slice 3: aggregate + child (tasks 1–3), target stepper (8), chip day list + bottom sheet (9), validation, submit, confirmation (10), `SubmissionQueries` stub replaced (6). Roadmap decisions 2 (POST throws if exists / PUT full replace — task 4, 5), 3 (`Revise`, one `SubmissionRevised` — task 2), 4 (one route, store-held wizard — tasks 8–10), 5 and 7 (chip list, sheet in this slice — task 9), 8 (extra picks via picks ≥ target — task 2).
  - Carried over from slice 2: window rule in the commands (task 2 + 5 Step 6), stateless identity with the ID in the body only (tasks 5, 7, Step 3.9), `WIZARD_STEPS` 3 → 5 (task 8), `hasAvailability` counts Open slots (tasks 7–8), the `p-message` leave animation (review scss) and a sheet that cannot linger (task 9), statuses mapped to keys (task 10), en + he in the same commit (tasks 8–10).
  - Requirements §7 validation rules: national ID on the roster (resolver 404), target ≥ 1, picks ≥ target, slot once, window closed between load and submit (409 → recheck) — all covered above. §8.1 (Double = 1 in counts) — task 6. §8.3 (instants UTC: `submitted_at_utc` / `revised_at_utc` timestamptz; slot windows stay wall-clock labels) — tasks 3, 7. §10 (concurrent submissions — unique index, README open item 1).
- **Placeholder scan:** every code step carries full file contents or an exact, anchored edit; the deferred behaviors (loading an existing submission, the window-closed screen, reorder, Excel detail sheet) are named with their owning slice or module.
- **Type consistency:** C# names match across tasks — `SlotPick.Of`, `Submission.Create/Revise` (same six parameters), `ISubmissionRepository.GetByPublicationAndStudentAsync`, `IStudentRepository.GetActiveByNationalIdAsync`, `SubmissionContextResolver.ResolveAsync/ResolvePicks`, `SlotRequestForSubmissionRequest(SlotId, SessionType, Constraint)`; JSON (`nationalId`, `targetCount`, `slotRequests[].slotId/sessionType/constraint`, `hasSubmission`) matches the TypeScript DTOs (task 7) and the page-spec request expectation (task 10). Store members used by templates (`targetCount`, `minTargetCount`, `pickCount`, `pickSheet`, `slotDays`, `reviewItems`, `missingPicks`, `submitStatus`, `replacesEarlierSubmission`, `canSubmit`, `submittedBodyKey`, `submittedParams`, and the methods `continueToTarget`, `continueToSlots`, `increaseTarget`, `decreaseTarget`, `goBack`, `openPick`, `savePick`, `removeOpenPick`, `closePick`, `continueToReview`, `changeTarget`, `submit`, `editSubmission`, `recheck`) are defined in tasks 8–10; translation keys used in templates are exactly those added in tasks 8, 9 and 10.
- **Judgment calls to confirm while implementing:** PrimeNG 21 `p-button` inputs used here (`text`, `outlined`, `severity`, `fluid`, `loading`) and `pTextarea` / `pFocusTrap` are the installed 21.1.9 APIs — existing usages win if anything differs. If EF cannot translate task 6's `GroupBy` over the owned collection, group the selected slot ids in memory (noted there). If the task-5 `PUT` answers 500 with a `DbUpdateConcurrencyException`, check task 3's `ValueGeneratedNever()` first. If jsdom does not fire `change` when a hidden radio is clicked, dispatch `change` on it in the spec's `chooseDouble` helper.
