# Task 6 of 6: End-to-end verification (two students, one link, HE + EN, 375px) + PR

> Part of [US-50/51/27: Identity & Routing](README.md). Requires tasks 1–5 committed. Work on branch `52-us-50-51-27-identity-and-routing`.

**Files:**
- Modify: `docs\modules\student-form\README.md` (link this plan from the slice-2 row)
- Local only, never staged: `.claude\launch.json` (remove the `api-smoke` entry at the end)

**Interfaces:**
- Consumes: the smoke database, teachers, roster and open publication from task 2 Steps 3–4 (`LINK`); the client from tasks 3–5.
- Produces: verified slice + PR closing #52, #53, #28.

- [x] **Step 1: Bring up the stack**

1. `api-smoke` running against `drivinglessons_us50_smoke` (task 2 Step 3). Confirm the publication is still open: `curl -s http://localhost:5080/api/submissions/by-link/$LINK` shows `"state":"open"`. If the shell from task 2 is gone, recover the link as the admin: `curl -s "http://localhost:5080/api/publications/by-week?week=2026-10-04" -H "Authorization: Bearer <token>"` → `linkToken`. If the window has closed, extend or reopen it (`POST api/publications/{id}/extend-window` / `…/reopen` with `{"newEndUtc":"…"}`), or re-run task 2 Steps 3–4 on a fresh smoke database.
2. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080).
3. Browser pane: `resize_window` preset **mobile** (375×812), reload. Start **logged out** (`localStorage.removeItem('auth_token')`) and in **Hebrew** (the default).

Verify with `get_page_text`, `read_page`, `find`, `javascript_tool` and `read_network_requests` (project memory: `screenshot` can hang on this PrimeNG app — `await document.fonts.ready` first, and fall back to DOM/`getComputedStyle` evidence if it still times out). Reload `/s/{LINK}` between students so each starts from a fresh store (README decision 8).

National IDs (synthetic, from the task-2 roster): **A** `000000018` → Smoke Cohen · **B** `000000026` → Smoke Levi · **C** `000000034` → Smoke NoWeek (no grid) · **D** `000000042` → deactivated · unknown `000000059` · bad check digit `000000019`.

- [x] **Step 2: Student A, Hebrew — the whole slice-2 path (US-50, US-27, US-51)**

1. `/s/{LINK}`: `שלב 1 מתוך 3`, heading `הזינו מספר תעודת זהות`, the ID field with hint `9 ספרות…`, **no caption bar**, `המשך` disabled. `document.querySelector('#national-id').inputMode` → `"numeric"`.
2. Type `000 000 018` (with spaces): within a moment `מצאנו אתכם — Smoke Student A.` and `…לשבוע 41 אצל Smoke Cohen.`; `המשך` enabled. **US-50: no password, no verification step.**
3. Click `המשך` → details: `שלב 2 מתוך 3`, caption `שבוע 41 · …`, lead `אתם מגישים זמינות עבור`, avatar `SC`, h1 **Smoke Cohen**, card rows תלמיד/ה = Smoke Student A, תיבת הילוכים = **אוטומטית**, רכב = Smoke Auto, שבוע = `שבוע 41 · …`. `find` for `combobox` / `radio` / a second textbox returns nothing — **US-27: read-only, no teacher choice, no transmission question.**
4. Click `המשך` → slots: `שלב 3 מתוך 3`, heading `השבוע של Smoke Cohen`, caption `שבוע 41 · … · Smoke Cohen` (**US-27 "prominently"**), six day groups ראשון … שישי with dates (4.10 … 9.10), Sunday–Thursday four chips (`בוקר`, `צהריים`, `אחה״צ`, `ערב` with `07:00–12:00` …), Friday two chips with `בוקר וצהריים בלבד`, **no striped chip** (Cohen has everything open).

- [x] **Step 3: Everyone else on the same link**

1. **Student B** (reload first) — `000000026` → found `Smoke Student B … Smoke Levi`; details show **Smoke Levi**, **ידנית**, Smoke Manual; slots: `השבוע של Smoke Levi`, and **Sunday morning is striped** (`document.querySelectorAll('.slot-chip--unavailable').length` → `1`, and it is the first chip of ראשון). **US-51: same link, B sees Levi's grid, A saw Cohen's.**
2. **Short ID** — type `18`: no lookup yet (the Network list has no new `identify` request) and `המשך` enabled; press Enter → found Student A. (**Review Focus 3**)
3. **Paste with bidi marks** — `javascript_tool`: set the field to `'‎000000018‏'` and dispatch an `input` event → found Student A. (**Review Focus 3**)
4. **Student C** — `000000034` → found; details show **Smoke NoWeek** plus the warning `עדיין אין שעות לשבוע הזה.` / `…פנו לבית הספר.`, and **no `המשך` button** (the footer is gone). (**Review Focus 2**)
5. **Unknown** — `000000059` → `אינכם מופיעים אצלנו.` / `…פנו לבית הספר כדי שיוסיפו אתכם.`, the field is marked invalid, `המשך` disabled, nothing below the ID step renders. **US-50: no form for non-members.**
6. **Deactivated D** — `000000042` → the same not-on-file message. (**Review Focus 1**)
7. **Bad check digit** — `000000019` → `זה לא נראה כמו מספר תעודת זהות תקין…`, `המשך` disabled.
8. **Letters** — type `00000001a` → no lookup, `המשך` disabled (the input keeps what was typed).
9. **Edit after a result** — type `000000018` (found), then delete the last digit → the found banner disappears immediately and `המשך` is enabled only as a lookup (8 digits). Type `000000026` quickly after `000000018`: the banner ends on **Smoke Student B**, never A. (**Review Focus 5**)
10. **PII never leaves the body** (**Review Focus 4**):
    - `read_network_requests` with `urlPattern: "identify"` → every URL is `/api/submissions/by-link/{LINK}/identify` — no digits of any ID; the request payload is `{"nationalId":"…"}`.
    - `javascript_tool`: `Object.entries(localStorage)` → only `app_lang` (and `auth_token` if you signed in earlier) — no national ID; `sessionStorage.length` → `0`; `location.href` → `/s/{LINK}` only.
    - The not-on-file response body (`read_network_requests` → that request's body) has `detail` `No active student on the roster matches this national ID.` — no ID.
11. **Lookup failure + retry** — `preview_stop` the `api-smoke` server, type `000000018` → `לא הצלחנו לבדוק את תעודת הזהות כרגע…`, `המשך` enabled. Start `api-smoke` again, click `המשך` → found Student A without a page reload.
12. **Stale admin session** — `localStorage.setItem('auth_token', 'not-a-real-jwt')`, reload, identify A → still works (anonymous endpoint, no redirect to `/login`). Remove the key.

- [x] **Step 4: Hebrew/RTL mechanics at 375px** (`javascript_tool`)

On each of the three steps (use Student B so the grid has a striped chip):
- `document.documentElement.dir` → `"rtl"`.
- `document.documentElement.scrollWidth <= window.innerWidth` → `true` (no horizontal scroll).
- `document.querySelector('.wizard-step__footer button')?.getBoundingClientRect().height` → ≥ 40 (identify, details); `#national-id` height ≥ 40; `[...document.querySelectorAll('.slot-chip')].every(c => c.getBoundingClientRect().height >= 40)` → `true`.
- Details: the avatar sits at the inline-start (**right**) edge of the teacher row; in the card, labels at the right and bold values at the left.
- Slots: ראשון chips run from the right edge; `בוקר וצהריים בלבד` sits at the inline end (left) of the שישי header; the time labels read `07:00–12:00` left-to-right.
- The footer stays pinned while scrolling the slots list only if the list overflows — at 375×812 the identify and details footers sit at the bottom of the viewport.

- [x] **Step 5: English**

Click `EN` on the slots step → `dir` becomes `"ltr"`, the page **stays on the slots step** (`Step 3 of 3`, `Smoke Levi's week`, `Sunday … Friday`, `morning & noon only`), and dates re-render (`10/4` …) without a reload. Reload and walk Student A once in English: `Enter your national ID` → `Found you — Smoke Student A.` → `You are submitting availability for` / **Smoke Cohen** / `Automatic` → `Smoke Cohen's week`. Check the unknown-ID message once in English (`We don't have you on file.`).

- [x] **Step 6: Desktop and admin unaffected**

- `resize_window` preset `desktop`: the shell is the centered 30rem column; the details card and chips do not stretch edge to edge.
- Sign in as the admin (`admin@local.dev` / `DevAdmin#2026` — the dev seed from `appsettings.Development.json`), open **Roster**: the four smoke students are listed, D dimmed as inactive. Open **Publications** for week 41: the share link box still shows `{origin}/s/{LINK}`.
- Sign out and reset the viewport to `desktop`.

If any check fails, fix it in the owning task's files (with a failing spec first where the behavior is testable), re-run `npm test -- --watch=false`, and commit the fix separately before continuing.

- [x] **Step 7: Link this plan from the roadmap**

In `docs\modules\student-form\README.md`, change the slice-2 table row's first cell from `2. Identity & routing` to:

```markdown
2. Identity & routing — [plan](us-50-51-27-identity-routing-plan/README.md)
```

- [ ] **Step 8: Full check + commit**

```bash
dotnet build
dotnet test
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES / builds clean, no new warnings.

```bash
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 2 plan from the roadmap"
```

- [ ] **Step 9: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server.
2. Remove the `api-smoke` entry from `.claude\launch.json` (restore the file to its pre-task-2 local state; never stage it).
3. Drop the throwaway database — it holds only the smoke data created in task 2: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us50_smoke`.

- [ ] **Step 10: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 52-us-50-51-27-identity-and-routing
gh pr create --title "US-50/51/27: Student identifies by national ID and sees their teacher's grid" --body "Closes #52
Closes #53
Closes #28

## Summary
- Anonymous \`POST api/submissions/by-link/{token}/identify\` (national ID in the body, never the URL) admits active roster students and returns their teacher, car transmission and that teacher's week grid; unknown or deactivated IDs 404, malformed IDs 409, and no response or error echoes the ID
- The open student link is now a wizard: national-ID step (auto lookup at 9 digits; found / not on file / invalid / retry), read-only details confirming the roster teacher, car and transmission, and the assigned teacher's week grid day by day with unavailable slots striped
- A student whose teacher has no grid this week sees their teacher plus a contact-your-school notice instead of an empty grid
- Verified in Hebrew (RTL) and English at 375px with two students on the same link routed to different teachers

## Notes
- Identify is a read; it sits on the new \`SubmissionCommandController\` only because it is a POST (keeps PII out of URLs). Slice 3 adds the submission commands there.
- The slice-1 \"Submissions are open\" panel is replaced by the ID step.

## Test plan
- [x] Application: identify interactor — found, unknown link, not on roster, malformed IDs (409 family), not-found message has no ID
- [x] API smoke on a throwaway DB: A→Cohen / B→Levi on one link, short ID padded, no-grid teacher → empty slots, deactivated → 404, unknown → 404 without echo, bad check digit / inner spaces → 409, empty body → 400, stale bearer → 200, admin still 401
- [x] Client (Vitest): ID input mask incl. bidi marks, initials, wizard numbering, slot-day grouping, API posts the ID in the body only, page flows for every identify outcome and both grids
- [x] Browser: every step and outcome in HE + EN at 375px, RTL mechanics, PII absent from URLs and storage, desktop layout, admin unaffected

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-50 AC → roster member proceeds with no password (Step 2.2–2.3, task 4 spec "looks up a complete ID"); non-member sees "contact your school" and no form (Step 3.5, task 4 spec "keeps an ID that is not on the roster…"); deactivated treated as non-member (Step 3.6, task 2 Step 5). Requirements §7.3 unknown-ID message, validation rule "National ID present in the roster" → backend 404 path (task 1).
  - US-51 AC → one publication, A→Cohen and B→Levi on the same link with teacher + transmission read-only (task 2 Step 5 items 4–5, task 5 spec `it.each`, Step 2.4 + 3.1). Requirements §7.6 "week grid for their assigned teacher, Unavailable visibly blocked" → striped chips + hidden label (task 5); "unselectable" holds trivially — nothing is selectable until slice 3.
  - US-27 AC → details step shows "You are submitting availability for {teacher}" with transmission, read-only, no selection/mismatch/abort/transmission question (task 4 Step 7, Step 2.3); teacher also in the caption from the grid step on.
  - Roadmap slice 2 → "National-ID step (found / welcome-back / unknown states)": found + unknown here; **welcome-back needs the Submission aggregate and is carried to slice 5** (README open item 4). "Roster resolves teacher + transmission shown read-only" (tasks 1, 4). "The assigned teacher's week grid loads" (tasks 1, 5).
  - Requirements §8.2 (no passwords, accepted impersonation risk), §8.4 (HE/EN, RTL — Steps 4–5), §10 (mobile, unguessable links — token-only URLs, 375px checks).
- **Placeholder scan:** every code step carries full file contents or an exact edit; the only intentionally deferred behaviors (picking, welcome-back) are named with their owning slice.
- **Type consistency:** `IdentifyStudentResponse` / `SlotForIdentifyStudentResponse` fields match between C# (task 1), the smoke output (task 2), and TypeScript (task 3); store members used in tasks 4–5 templates (`identifyStatus`, `student`, `studentName`, `teacherName`, `teacherInitials`, `hasAvailability`, `slotDays`, `currentStep`, `stepNumber`, `stepCount`, `captionKey`, `captionParams`, `changeNationalId`, `requestLookup`, `continueToDetails`, `continueToSlots`) are all defined in task 4 Step 4 / task 5 Step 6; translation keys used in templates are exactly those added in task 4 Step 3 and task 5 Step 2.
- **Judgment calls to confirm while implementing:** `p-button`'s `type`/`fluid`/`(onClick)` and `p-message` content projection are the PrimeNG 21 APIs used elsewhere in the app — existing usages win if the installed version differs. If the unit-test DOM does not submit a form when its submit button is clicked, dispatch `submit` on the form in `clickContinue` instead (as `submitIdentify` already does). If Quartz does not open the publication within a few seconds in task 2, check `PublicationReconciliationHostedService`/scheduler logs before suspecting the endpoint.
