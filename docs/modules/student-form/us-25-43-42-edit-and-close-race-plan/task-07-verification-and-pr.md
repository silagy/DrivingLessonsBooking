# Task 7 of 7: End-to-end verification (returning student, edits, dropped picks, window closed mid-submit, in HE and EN at 375px), roadmap link, PR

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires tasks 1–6 committed and the task-2/3 smoke API still available. Work on branch `26-us-25-43-42-edit-and-close-race`.

**Files:**
- Modify: `docs\modules\student-form\README.md` (link this plan from the slice-5 row)
- Local only, never staged: `.claude\launch.json` (remove the `api-smoke` entry at the end)

**Interfaces:**
- Consumes: the `drivinglessons_us25_smoke` database and the shell values from task 2 Step 4 (`API`, `AUTH`, `WEEK`, `LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `COHEN_ID`, `COHEN_WS`); the client from tasks 4–6.
- Produces: a verified slice and a PR closing #26, #43, #42.

State left by tasks 2–3: **A** `000000018` (Cohen) has a week-1 list (target 3; four picks in rank order: Tuesday noon, Wednesday afternoon "pick me up from work", Friday morning Double, Sunday evening) and a week-2 list (one Double pick). **B** `000000026` (Levi) and **E** `000000067` (Cohen) have none. Week 1 is open for three hours from task 2. Week 2 is closed.

- [ ] **Step 1: Bring up the stack**

1. `api-smoke` running against `drivinglessons_us25_smoke` (task 2 Step 4). In a bash shell, re-declare `API`, `json`, log in for `AUTH`, and set `WEEK`, `LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `COHEN_ID`, `COHEN_WS` to the values written down in task 2. If they are lost: `curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH"` gives `id` / `linkToken`, the same call works for `$WEEK2`, and teacher ids come from `GET api/teachers/find`.
2. Week 1 must be open: `curl -s $API/api/submissions/by-link/$LINK | json state` → `open`. If its three hours ran out, reopen it:
   ```bash
   curl -s -o /dev/null -w "reopen week 1: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
     -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
   ```
3. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080).
4. Browser pane: `resize_window` preset **mobile** (375×812), reload. Start **logged out** (`localStorage.removeItem('auth_token')`) and in **Hebrew** (the default).

Verify with `get_page_text`, `read_page`, `find`, `javascript_tool` and `read_network_requests`. Project memory: `screenshot` can hang on this PrimeNG app, so `await document.fonts.ready` first and fall back to DOM / `getComputedStyle` evidence if it still times out. Reload `/s/{LINK}` between students so each starts with a fresh store. A helper for the grid's badges (paste into `javascript_tool` when needed):

```js
[...document.querySelectorAll('[data-slot-id]')]
    .filter(chip => chip.querySelector('.slot-chip__rank'))
    .map(chip => [chip.querySelector('.slot-chip__rank').textContent.trim(), chip.dataset.slotId])
```

- [ ] **Step 2: Returning student A, Hebrew: the saved list is loaded for editing (US-25)**

1. `/s/{LINK}` → `שלב 1 מתוך 5`. Type `000000018` → the green banner `ברוכים השבים, Smoke Student A.` and `כבר שלחתם 4 בחירות ב{weekday, date, time}, אז טענו אותן. אפשר לערוך את ההגשה עד שהחלון ייסגר ({closesAt}).`. The button reads **`עריכת ההגשה`**, not `המשך`.
2. `read_network_requests` (`urlPattern: "identify"`) → open the response body: `submission.targetCount` 3, four `slotRequests` (`slotId`, `sessionType`, `constraint` only), `lastSavedAtUtc` set; no `hasSubmission`, no ids other than slot ids, no national ID.
3. Press `עריכת ההגשה` → details `שלב 2 מתוך 5`: Smoke Student A, Smoke Cohen, automatic transmission, Smoke Auto, all read-only (`document.querySelectorAll('app-details-step input, app-details-step select').length` → `0`). → `המשך`.
4. **Target**: the count reads `3` (the saved target), not `1`.
5. **Slots**: the badge helper returns `1`–`4` on exactly the four `slotId`s of the identify response, **in its order** (rank order, not grid order). The rank-2 chip (`רביעי · אחה״צ`) opens the sheet with `יחיד` checked and the constraint `pick me up from work`. The rank-3 chip (`שישי · בוקר`) opens with `כפול` checked. Close each sheet with **Escape** (nothing changes).

- [ ] **Step 3: Edit any number of times (US-43)**

1. Still A: remove the rank-4 pick (tap `ראשון · ערב` → `הסרת הבחירה`). Reopen rank 2, replace the constraint with `רק אחרי 17:00` → `שמירה`. Add `חמישי · ערב` → it becomes rank 4. `לסקירת הרשימה` → review: four rows in the new order, the notice `כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.`, **no** dropped-picks notice.
2. `שליחה` → `read_network_requests`: one **`PUT`** `/api/submissions/by-link/{LINK}` → **204** (never a `POST`), payload `targetCount` 3 and the four slot requests in order with the new constraint. Confirmation heading **`השינויים נשמרו`**, and the body `4 בחירות לשבוע … אצל Smoke Cohen. אפשר לערוך אותן עד …`.
3. `עריכת ההגשה` → review → `חזרה` → `חזרה` → target `−` → `2` → `לבחירת שעות` → remove `חמישי · ערב` → review → `שליחה` → a second **`PUT`** → 204 → `השינויים נשמרו`, `3 בחירות…`.
4. What the server holds (exactly three rows, target 2, the new constraint on rank 2, `revised` `t`):
   ```bash
   docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us25_smoke -c "
   select r.rank, r.session_type, r.constraint_text, s.target_count, s.revised_at_utc
   from submissions s join students st on st.id = s.student_id join publications p on p.id = s.publication_id
   join slot_requests r on r.submission_id = s.id
   where st.name = 'Smoke Student A' and p.week_start = '$WEEK' order by r.rank;"
   ```
5. **Re-entry**: reload `/s/{LINK}`, type `000000018` → the banner now says `כבר שלחתם 3 בחירות…` with the later time. `עריכת ההגשה` → target `2` → the three badges match step 4's rows.

- [ ] **Step 4: A saved pick that is no longer offered (Review Focus 1, 2)**

1. Mark A's rank-1 slot Unavailable, as the admin might after A submitted:
   ```bash
   A_RANK1=$(docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us25_smoke -tA -c "
   select r.slot_id from submissions s join students st on st.id = s.student_id
   join publications p on p.id = s.publication_id join slot_requests r on r.submission_id = s.id
   where st.name = 'Smoke Student A' and p.week_start = '$WEEK' and r.rank = 1;")
   curl -s -o /dev/null -w "block A's rank 1: %{http_code}\n" -X POST \
     "$API/api/week-schedules/$COHEN_WS/slots/$A_RANK1/mark-unavailable" -H "$AUTH"
   ```
   Expected `204`.
2. Reload, identify A → the banner still says `3 בחירות` (what A sent) → `עריכת ההגשה` → target `2` → slots: that chip is striped and `disabled`, with no badge. The other two picks are ranked `1`, `2`.
3. `לסקירת הרשימה` → review: the warning `אחת הבחירות הקודמות שלכם כבר לא זמינה השבוע, אז הסרנו אותה מהרשימה.`, two rows, and **no** not-enough message (2 picks against target 2).
4. `חזרה` → `חזרה` → target `+` → `3` → `לבחירת שעות` → `לסקירת הרשימה` → `אין מספיק בחירות.` / `היעד שלכם הוא 3, אבל בחרתם 2…` with `שליחה` disabled; the dropped notice is still shown. `הוספת שעות` → add one slot → review → `שליחה` → `PUT` 204 whose payload does **not** contain `$A_RANK1`.
5. `עריכת ההגשה` → review → the dropped notice is **gone** (the list just sent no longer had that pick).
6. Restore the slot: `curl -s -o /dev/null -w "%{http_code}\n" -X POST "$API/api/week-schedules/$COHEN_WS/slots/$A_RANK1/mark-available" -H "$AUTH"` → `204`.

- [ ] **Step 5: First-timer E and the other-tab conflict (Review Focus 5, 7)**

1. Reload, type `000000067` → `מצאנו אתכם — Smoke Student E.`, **no** welcome back, button `המשך` → target `1` → no badges → pick one slot → review (no replaces notice) → `שליחה` → **`POST`** 204 → heading **`נשלח`** (not `השינויים נשמרו`).
2. **B in two tabs**: in this tab reload, identify `000000026` (Levi, no list) → pick two slots → review, **stop**. `tabs_create` a second tab → `/s/{LINK}` → identify B → pick one slot → submit → `POST` 204. Back in the first tab press `שליחה` → `POST` **409** → inline `לא הצלחנו לשמור את הרשימה — משהו השתנה מאז שפתחתם את הטופס, למשל שעה שכבר לא זמינה.` + `בדיקת הטופס מחדש`, and **not** the window-closed screen. Press `בדיקת הטופס מחדש` → the review still lists **this tab's two picks**, not the one saved from the other tab, and now shows the replaces notice → `שליחה` → **`PUT`** 204 → `השינויים נשמרו`. Close the second tab.

- [ ] **Step 6: The window closes mid-submit (US-42, Review Focus 3–4)**

1. Reopen week 2 for three minutes:
   ```bash
   curl -s -o /dev/null -w "reopen week 2: %{http_code}\n" -X POST $API/api/publications/$PUB2_ID/reopen -H "$AUTH" \
     -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+3 minutes' +%Y-%m-%dT%H:%M:%SZ)\"}"
   ```
2. **Tab 1**: `/s/{LINK2}`, identify **E** (no week-2 list) → target → one pick → review, **stop**.
3. **Tab 2** (`tabs_create`): `/s/{LINK2}`, identify **A** → welcome back with `בחירה אחת` → `עריכת ההגשה` → add one pick → review, **stop**.
4. Wait until `curl -s $API/api/submissions/by-link/$LINK2 | json state` → `closed`.
5. Tab 1 → `שליחה` → `POST` **409** whose response body has `"type":"problems/submission-window-closed"` → the screen `החלון נסגר ממש עכשיו`, `ההגשה לשבוע … נסגרה בזמן שהדף היה פתוח אצלכם. הרשימה שלכם לא נשמרה. אם זה דחוף, פנו ישירות אל Smoke Cohen.`, the hint `אם בית הספר יפתח את החלון מחדש, הקישור יעבוד שוב.`. There is no caption bar (`document.querySelector('.student-shell__caption')` → `null`) and no button inside the message (`document.querySelectorAll('app-status-message button').length` → `0`).
6. Tab 2 → `שליחה` → `PUT` **409** (same type) → the same heading with `השינויים שלכם לא נשמרו — הרשימה ששלחתם קודם נשארה כפי שהייתה.`.
7. Still in tab 2 press `EN` → the screen stays: `The window just closed` / `Your changes were not saved — the list you sent earlier stays as it was.` Press `HE` back. Close tab 2.
8. Nothing partial was saved:
   ```bash
   docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us25_smoke -c "
   select st.name, s.target_count, r.rank, r.session_type, r.constraint_text, s.revised_at_utc is null as never_revised
   from submissions s join students st on st.id = s.student_id join publications p on p.id = s.publication_id
   join slot_requests r on r.submission_id = s.id
   where p.week_start = '$(date -u -d "$WEEK + 7 days" +%F)' order by st.name, r.rank;"
   ```
   Expected: exactly one row (`Smoke Student A`, target 1, `session_type` 20, `only after 16:00`, `never_revised` `t`), unchanged since task 3. E has none.
9. Reload tab 1 → slice 1's closed screen `ההגשה נסגרה`, because the page now loads after the close.

- [ ] **Step 7: English**

Reload `/s/{LINK}` and press `EN`. Identify A → `Welcome back, Smoke Student A.` / `You already sent 3 picks on …, so we've loaded them. You can edit your submission until the window closes (…).`, button `Edit my submission`. Walk to the review and submit → `Changes saved`. Identify E once → `Found you — Smoke Student E.` with `Continue`.

- [ ] **Step 8: Hebrew/RTL mechanics at 375px** (`javascript_tool`)

On the ID step with the welcome-back banner, the review with the replaces and dropped notices both shown (repeat Step 4.1–4.3 if needed, then restore the slot), the confirmation `השינויים נשמרו`, and the window-closed screen:
- `document.documentElement.dir` → `"rtl"`; `document.documentElement.scrollWidth <= window.innerWidth` → `true`.
- The banner and notices wrap inside the column (no text overflow: `[...document.querySelectorAll('p-message')].every(m => m.getBoundingClientRect().right <= window.innerWidth)` → `true`).
- The footer button is ≥ 40px tall: `document.querySelector('.wizard-step__footer button').getBoundingClientRect().height >= 40` → `true`.
- The `{savedAt}` / `{closesAt}` times render as Jerusalem wall-clock (compare with the `lastSavedAtUtc` / `windowEndUtc` instants: +2h or +3h depending on DST).

- [ ] **Step 9: PII**

- `read_network_requests` (`urlPattern: "by-link"`): every URL is `/api/submissions/by-link/{token}[/identify]` with no digits of an ID. The ID appears only in request payloads, and the identify **response** contains no national ID, phone or id other than slot ids.
- `javascript_tool`: `Object.entries(localStorage)` → only `app_lang` (and `auth_token` if signed in), with no national ID and no picks; `sessionStorage.length` → `0`; `location.href` → `/s/{token}` only.

If any check fails, fix it in the owning task's files (write a failing spec or test first where the behaviour is testable), re-run the suites, and commit the fix separately before continuing.

- [ ] **Step 10: Link this plan from the roadmap**

In `docs\modules\student-form\README.md`, change the slice-5 table row's first cell from `5. Edit & close race` to:

```markdown
5. Edit & close race — [plan](us-25-43-42-edit-and-close-race-plan/README.md)
```

- [ ] **Step 11: Full check + commit**

```bash
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES and builds clean, with no new warnings and `No changes have been made to the model since the last migration.` (this slice has no migration).

```bash
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 5 plan from the roadmap"
```

- [ ] **Step 12: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server (and the `client` server).
2. Remove the `api-smoke` entry from `.claude\launch.json`, restoring the file to its pre-task-2 local state. Never stage it.
3. Drop the throwaway database, which only holds this slice's smoke data: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us25_smoke`.

- [ ] **Step 13: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 26-us-25-43-42-edit-and-close-race
gh pr create --title "US-25/43/42: Returning students edit their submission; clean window-closed rejection" --body "Closes #26
Closes #43
Closes #42

## Summary
- Identify returns the student's saved submission (target, ranked slot requests with session type and constraint, last-saved time) in place of \`hasSubmission\`; no submission or slot-request id leaves the anonymous endpoint.
- The student form loads it when a returning student enters their ID: a welcome-back banner with **Edit my submission**, the saved target and ranked picks in the wizard, the roster profile read-only as before. Picks whose slot is no longer offered are dropped with a notice.
- Every resubmit is a \`PUT\` that replaces the list; the confirmation says **Changes saved**. Covered from the domain to the browser, including several edits in one visit and re-entry.
- The window now ends at its end time, not when the close job runs: \`Submission.Create/Revise\` refuse at or after \`EndUtc\` via \`Publication.IsOpenAt\`. A refused revision leaves the earlier version untouched.
- A window-closed 409 carries the ProblemDetails type \`problems/submission-window-closed\`; the form shows a dedicated **The window just closed** screen (nothing saved; for an edit, the earlier list stands). Other rejections keep the inline message and Check the form again.
- Verified in Hebrew (RTL) and English at 375px.

## Notes
- Supersedes slice 3's state-only window rule (its decision 3 / open items 2–4).
- Reorder of the ranked list is still slice 4 (#38); until then a rank changes by removing and re-adding a pick.
- No migration, no new endpoint, no new packages.

## Test plan
- [x] Domain: \`SubmissionWindow.HasEndedBy\`, \`Publication.IsOpenAt\`, create/revise refused at the end time, refused revision keeps the previous version, repeated revisions replace
- [x] Application: create/revise refused once the end time passed while still Open (no add, no commit), revise any number of times
- [x] API smoke on a throwaway DB: saved submission after create and two revises (rank order, types, constraints), other students see none, window-closed type on POST/PUT after close, other 409s untyped, stored rows unchanged
- [x] Client (Vitest): loading a saved submission (drops, target kept), window-closed problem check, welcome back, saved target/picks, PUT with the edited list, repeated edits, recheck keeps edits, ID switch starts fresh, window-closed screen for new and edited lists
- [x] Browser: returning student, edits and re-entry, dropped pick, other-tab conflict, window closing mid-submit in two tabs, in HE + EN at 375px, RTL mechanics, PII absent from URLs, storage and responses

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-25 AC (existing submission for this publication + national ID on the same link → roster profile read-only and the submission loaded for editing: target, ranked picks, constraints) → task 2 (identify returns target, rank-ordered slot requests with session type and constraint; Step 5 checks 2 and 4), task 4 `loadedSubmissionOf`, task 5 "starts the target at the saved count", "shows the saved picks ranked … session type and constraint", "shows the roster details read-only…", welcome back; Step 2 here. Mockup `SIdEditing` → task 5 banner and button.
  - US-43 AC (re-enter the ID, change target/picks/ranks/constraints, resubmit → updated, replacing the previous version as the single submission) → task 1 `Revise__Replaces_Every_Earlier_Version`, `Revises_Any_Number_Of_Times`; task 2 Step 5 (create, revise, revise) and Step 6 (only the latest rows); task 5 "replaces the saved submission…", "edits again from the confirmation any number of times…"; Step 3 here (two edits, re-entry). Ranks: remove and re-add until slice 4 (README open item 1).
  - US-42 AC (loaded while open, end time passed before finishing, submit → rejected with a clear window-closed message, no partial data saved) → task 1 (end-time rule, refused revision keeps the previous version, interactor asserts no add/commit), task 3 (typed 409, Step 5 stored rows unchanged), task 4 `isWindowClosedProblem`, task 6 screen for new and edited lists; Step 6 here. Mockup `SDoneErr` → task 6.
  - Roadmap slice 5 ("loads their submission for editing, edits any number of times until close, clean window-closed rejection screen") → tasks 2, 5, 6. Roadmap decision 2 ("POST identify (national ID → profile + grid + existing submission)") → task 2. Decision 1 (stateless; ID in bodies only) → Global Constraints, Step 9. Decision 3 (`Revise` full replace) → task 1. Decision 4 (one route, store-held wizard; refresh restarts at the ID step with the submission loaded server-side) → task 5 seeding, Step 3.5.
  - Slice 3 carry-overs: open item 2 (close-job lag) → task 1; open item 3 (409 reasons indistinguishable) → task 3; open item 4 (returning student starts empty) → tasks 2, 5; decision 9 (dedicated window-closed screen in slice 5) → task 6; decision 17 (in-visit edit via PUT) → task 5 "edits again…".
  - Requirements §7 validation rule "window closed between load and submit → reject with clear message" → task 6; §8.2 (self-service edits, accepted identity risk) → README decision 1, open item 5; §8.3 (instants UTC, shown as Jerusalem wall-clock) → `lastSavedAtUtc` via `formatWindowInstant`, Step 8; §10 (mobile-first) → Step 8.
- **Placeholder scan:** every code step has full contents or an exact, anchored edit. The only conditional instructions are named fallbacks (task 2's in-memory mapping if EF cannot translate the nested `OrderBy`; task 6's placement if `submitted` is no longer the last JSON key).
- **Type consistency:** C# `SubmissionWindow.HasEndedBy(DateTimeOffset)`, `Publication.IsOpenAt(DateTimeOffset)`, `SubmissionForIdentifyStudentResponse { TargetCount, LastSavedAtUtc, SlotRequests }`, `SlotRequestForIdentifyStudentResponse { SlotId, SessionType, Constraint }`, `ProblemTypes.SubmissionWindowClosed`. The JSON (`submission`, `targetCount`, `lastSavedAtUtc`, `slotRequests[].slotId/sessionType/constraint`, `type: "problems/submission-window-closed"`) matches the TypeScript DTOs and `SUBMISSION_WINDOW_CLOSED_PROBLEM` (task 4). The store members used by templates and specs (`welcomeBack`, `droppedPickCount`, `submittedTitleKey`, `closedMidSubmitBodyKey`, `captionParams`) are defined in tasks 5–6, and `sentAsRevision` is defined in task 5 before task 6 reads it. The component inputs `welcomeBack` / `droppedPickCount` are bound in the page in task 5. The translation keys used in templates are exactly those added in tasks 5 and 6, in both files.
- **Judgment calls to confirm while implementing:** the switch in `ApiExceptionFilter` relies on a target-typed nullable tuple (C# 9+, and the repo is on .NET 10); if the compiler still asks for a cast, cast the `null` elements to `(string?)null`. If jsdom's `HttpErrorResponse` normalises `error` differently from the browser, `isWindowClosedProblem` still only reads `error.error?.type`, and task 6's page specs construct the error exactly as Angular's `HttpClient` does.
