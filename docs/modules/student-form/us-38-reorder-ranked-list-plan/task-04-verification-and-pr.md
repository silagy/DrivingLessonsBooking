# Task 4 of 4: End-to-end verification (arrows and drag, POST and PUT, stored ranks, HE and EN at 375px), roadmap link, PR

> Part of [US-38: Reorder the Ranked List](README.md). Requires tasks 1–3 committed. Work on branch `38-us-38-reorder-ranked-list`.

**Files:**
- Modify: `docs\modules\student-form\README.md` (link this plan from the slice-4 row)
- Local only, never staged: `.claude\launch.json` (an `api-smoke` entry added in Step 1, removed in Step 10)

**Interfaces:**
- Consumes: the client from tasks 1–3; the unchanged API (`POST`/`PUT api/submissions/by-link/{token}`, `POST …/identify`).
- Produces: a verified slice and a PR closing #38.

Verify with `get_page_text`, `read_page`, `find`, `javascript_tool` and `read_network_requests`. Project memory: `screenshot` can hang or come back half-rendered on this PrimeNG app, so `await document.fonts.ready` (plus ~1.5s) first and fall back to DOM / `getComputedStyle` evidence. Two helpers to paste into `javascript_tool` when needed:

```js
const reviewRows = () => [...document.querySelectorAll('.review__item')].map(row => [
    row.querySelector('.review__rank').textContent.trim(),
    row.querySelector('.review__slot').textContent.trim().replace(/\s+/g, ' '),
    row.classList.contains('review__item--preferred') ? 'preferred' : 'backup',
]);
const gridBadges = () => [...document.querySelectorAll('[data-slot-id]')]
    .filter(chip => chip.querySelector('.slot-chip__rank'))
    .map(chip => [chip.querySelector('.slot-chip__rank').textContent.trim(), chip.textContent.trim().replace(/\s+/g, ' ')]);
```

- [ ] **Step 1: Bring up the stack against a throwaway database (bash / Git Bash)**

⚠️ The seed uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`).

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history) and create the throwaway database:
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us38_smoke
   ```
   If `createdb` reports that it exists from an earlier run, drop it first with `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us38_smoke`. It only ever holds this smoke data.
2. Add a **local-only** entry to the `configurations` array of `.claude\launch.json`. Never stage this file:
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us38_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first, since both use port 5080). Startup applies every migration to the empty database and seeds the dev admin. `preview_logs` must show no migration error.
4. Seed one teacher, a car, next week's grid (every slot open), a one-student roster and an open publication. The national ID is synthetic (`000000018`, valid check digit). Never use real IDs in test data.

```bash
API=http://localhost:5080
WEEK=$(date -u -d 'next sunday' +%F)
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"

COHEN_ID=$(curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"name":"Smoke Cohen","contactEmail":"smoke.cohen@example.com"}' | json id)
curl -s -o /dev/null -w "car: %{http_code}\n" -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"name":"Smoke Auto","type":"Yaris","transmission":"automatic"}'
curl -s -o /dev/null -w "week schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"teacherId\":\"$COHEN_ID\",\"weekStart\":\"$WEEK\"}"

ROSTER_DIR=$(mktemp -d)
cat > "$ROSTER_DIR/roster.csv" <<'CSV'
שם מלא,תעודת זהות,טלפון,מורה,רכב
Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
CSV
curl -s -w "  ← import: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 5
echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
echo "WEEK=$WEEK LINK=$LINK PUB_ID=$PUB_ID"
```

Expected: `car: 201`, `week schedule: 201`, `import: 201` with `"added":1`, `publish: 204`, then `link state: open` (if it still says `published`, wait a few seconds for Quartz to fire the past-due open job), and the `WEEK=… LINK=… PUB_ID=…` line. **Write these values down.**

The stored-ranks query used in Steps 3 and 4:

```bash
ranks() { docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us38_smoke -c "
select r.rank, sl.day, sl.\"window\", r.session_type, r.constraint_text, s.target_count, s.revised_at_utc is not null as revised
from submissions s join slot_requests r on r.submission_id = s.id join slots sl on sl.id = r.slot_id
order by r.rank;"; }
```

5. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080). Browser pane: `resize_window` preset **mobile** (375×812), reload. Start **logged out** (`localStorage.removeItem('auth_token')`) and in **Hebrew** (the default).

- [ ] **Step 2: First submission, Hebrew, the arrows (US-38 AC, Review Focus 1, 2, 6, 8)**

1. `/s/{LINK}` → type `000000018` → `המשך` → details → `המשך` → target: press `יותר שיעורים` once → `2` → `לבחירת שעות`.
2. Pick, in this order: `ראשון · בוקר` (in the sheet choose `כפול` and type the constraint `רק אחרי 9:00`, then add), `שני · צהריים`, `רביעי · אחה״צ`, `חמישי · ערב`. `gridBadges()` → `1`–`4` in that order. `לסקירת הרשימה`.
3. Review: `reviewRows()` → the four rows in pick order, ranks 1–2 `preferred`, 3–4 `backup`. The hint `גררו בחירה בעזרת הידית, או השתמשו בחיצים, כדי לשנות את הדירוג שלה.` is shown. Every row has a grip and two arrows. On rank 1 the up arrow is disabled, on rank 4 the down arrow is disabled:
   ```js
   [...document.querySelectorAll('.review__item')].map(row => [
       row.querySelector('.review__grip') !== null,
       row.querySelector('.review__move-up button').disabled,
       row.querySelector('.review__move-down button').disabled,
   ])
   ```
   → `[[true,true,false],[true,false,false],[true,false,false],[true,false,true]]`.
4. Accessible names: `read_page` on the list shows the rank-4 up button as `העברת בחירה 4 (חמישי · ערב) למעלה`.
5. **Keyboard:** `javascript_tool` focuses the rank-4 up button (`document.querySelectorAll('.review__move-up button')[3].focus()`), then `computer` `key` `Enter` three times. After each press: `document.activeElement.getAttribute('aria-label')` names `חמישי · ערב`. After the third press, it is the **down** button (`…למטה`), because the up button is now disabled. `reviewRows()` → `חמישי · ערב` 1 `preferred`, `ראשון · בוקר` 2 `preferred`, `שני · צהריים` 3 `backup`, `רביעי · אחה״צ` 4 `backup`. `document.querySelector('.review__announcement').textContent.trim()` → `הבחירה עברה לדירוג 1.`, and the element has `aria-live="polite"`.
6. `ראשון · בוקר` still shows `כפול` and `"רק אחרי 9:00"` on its row (now rank 2).

If any check fails, fix it in the owning task's files (write a failing spec first where the behavior is testable), re-run the suites, and commit the fix separately before continuing.

- [ ] **Step 3: Drag, back to the grid, submit, stored ranks (Review Focus 3)**

1. **Drag** rank 4 (`רביעי · אחה״צ`) onto rank 1 by its grip. Read both grips' centers:
   ```js
   [...document.querySelectorAll('.review__grip')].map(grip => { const r = grip.getBoundingClientRect(); return [Math.round(r.x + r.width / 2), Math.round(r.y + r.height / 2)]; })
   ```
   then `computer` `left_click_drag` from grip 4's center to a point 8px above grip 1's center. `reviewRows()` → `רביעי · אחה״צ`, `חמישי · ערב`, `ראשון · בוקר`, `שני · צהריים`, the first two `preferred`. The announcement reads `הבחירה עברה לדירוג 1.`.
   If the pane's synthetic drag does not move anything, retry with a `left_click_drag` to an intermediate point, then from there to the target. If it still fails, record "pane drag not exercised" for the PR notes (the spec in task 3 covers the drop path) and continue. Do not change code for it.
2. **Touch scrolling is not hijacked:** `getComputedStyle(document.querySelector('.review__grip')).touchAction` → `none`; `getComputedStyle(document.querySelector('.review__details')).touchAction` → `auto`.
3. Press `חזרה` (back) → slots step: `gridBadges()` → `1` רביעי אחה״צ, `2` חמישי ערב, `3` ראשון בוקר, `4` שני צהריים. Add `ראשון · ערב` → its badge is `5`. `לסקירת הרשימה` → five rows, the new one last. The announcement is empty (it was reset on entering the review).
4. `שליחה` → `read_network_requests` (`urlPattern: "by-link"`): one **`POST`** → **204**, payload `targetCount` 2 and `slotRequests` in the on-screen order, with the Double + `רק אחרי 9:00` on the third entry and **no `rank` field** anywhere. Confirmation heading `נשלח`, body `5 בחירות לשבוע … אצל Smoke Cohen…`.
5. `ranks` → five rows, rank 1–5 = Wednesday afternoon, Thursday evening, Sunday morning (Double, `רק אחרי 9:00`), Monday noon, Sunday evening; `target_count` 2; `revised` `f`.

- [ ] **Step 4: Returning student reorders the saved list (Review Focus 5)**

1. Reload `/s/{LINK}`, type `000000018` → welcome-back banner → `עריכת ההגשה` → details → target `2` → slots (badges as in Step 3.5) → `לסקירת הרשימה`.
2. `reviewRows()` → the saved order of Step 3.5. Move `ראשון · ערב` (rank 5) to rank 2: press its up arrow three times (or drag it by its grip). `reviewRows()` → Wednesday afternoon, Sunday evening, Thursday evening, Sunday morning, Monday noon. The first two are `preferred`, so Sunday evening was promoted from backup and Thursday evening dropped to backup. The notice `כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.` is shown.
3. `שליחה` → one **`PUT`** `/api/submissions/by-link/{LINK}` → **204** (never a `POST`), payload in the new order. Heading `השינויים נשמרו`.
4. `ranks` → the five rows in the new order, the Double + constraint now on rank 4, `revised` `t`.
5. **Re-entry:** reload, identify again → `עריכת ההגשה` → … → review: `reviewRows()` matches step 4. Go back to the slots step: `gridBadges()` matches too.

- [ ] **Step 5: English**

Reload `/s/{LINK}` and press `EN`. Identify A → `Edit my submission` → walk to the review. The hint reads `Drag a pick by its handle, or use the arrows, to change its rank.` The rank-2 up button's accessible name is `Move pick 2 (Sunday · Evening) up`. Move rank 3 down once → the announcement reads `Moved to rank 4.` `document.documentElement.dir` → `"ltr"`. Leave without submitting (reload).

- [ ] **Step 6: Hebrew/RTL mechanics at 375px** (`javascript_tool`, Hebrew, on the review with five picks)

- `document.documentElement.dir` → `"rtl"`; `getComputedStyle(document.querySelector('.review__list')).direction` → `"rtl"`.
- The grip is on the **right**, at the inline-start, and the arrows on the left: `const row = document.querySelector('.review__item'); row.querySelector('.review__grip').getBoundingClientRect().left > row.querySelector('.review__rank').getBoundingClientRect().left` → `true`, and `row.querySelector('.review__moves').getBoundingClientRect().right < row.querySelector('.review__details').getBoundingClientRect().left + 1` → `true`.
- Arrows are not mirrored: `getComputedStyle(row.querySelector('.review__move-up .pi')).transform` → `"none"`.
- Touch targets ≥ 40px: `[...document.querySelectorAll('.review__grip, .review__moves button')].every(el => { const r = el.getBoundingClientRect(); return r.width >= 40 && r.height >= 40; })` → `true`.
- No horizontal scroll and nothing spills out of a card: `document.documentElement.scrollWidth <= window.innerWidth` → `true`; `[...document.querySelectorAll('.review__item')].every(row => row.scrollWidth <= row.clientWidth)` → `true`.
- The longest row (e.g. `רביעי · אחה״צ 15:00–18:00` with a constraint) wraps inside `.review__details`, and the time stays left-to-right (`dir="ltr"` on `.review__time`).
- Grep the component styles for physical properties: `grep -nE "(margin|padding|border)-(left|right)|\bleft:|\bright:" client/src/app/features/student-form/ui/components/review-step/review-step.component.scss` → no output.
- Final proof: `await document.fonts.ready`, wait ~1.5s, `computer` `screenshot` of the Hebrew review with five picks. If it hangs or is half-rendered, rely on the checks above.

- [ ] **Step 7: PII**

`read_network_requests`: the national ID appears only in request **bodies** (identify, POST, PUT), never in a URL. `Object.keys(localStorage)` holds no national ID. `preview_logs` for `api-smoke` does not contain `000000018`.

- [ ] **Step 8: Link this plan from the roadmap**

In `docs\modules\student-form\README.md`, change the slice-4 table row's first cell from `4. Reorder` to:

```markdown
4. Reorder — [plan](us-38-reorder-ranked-list-plan/README.md)
```

- [ ] **Step 9: Full check + commit**

```bash
dotnet build
dotnet test
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES and builds clean, with no new warnings. The backend is untouched, so `dotnet test` only confirms nothing else moved.

```bash
git status --short
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 4 plan from the roadmap

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

`git status --short` before the `add` must show only `docs/modules/student-form/README.md` and the local `.claude/launch.json`. Do not stage the latter.

- [ ] **Step 10: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server and the `client` server.
2. Remove the `api-smoke` entry from `.claude\launch.json`. `git diff .claude/launch.json` must be empty afterwards.
3. Drop the throwaway database: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us38_smoke`.
4. `resize_window` preset **desktop**.

- [ ] **Step 11: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 38-us-38-reorder-ranked-list
gh pr create --repo silagy/DrivingLessonsBooking --title "US-38: Student reorders the ranked list before submitting" --body "Closes #38

## Summary
- The review step lets a student move any pick to a new rank before submitting: **Move up / Move down** buttons on every pick, and **drag by a grip handle** (Angular CDK drag-drop).
- Ranks renumber to the new order, the preferred/backup split follows the new positions, and session type and constraint travel with the pick. The grid badges show the new ranks, and a pick added afterwards goes to the end.
- That order is what the Submission carries, for a first submission (\`POST\`) and for an edit (\`PUT\`). The server already numbers slot requests by position, so there is no backend change.
- Accessible: the arrows are the single-pointer and keyboard path (WCAG 2.5.7), focus stays on the moved pick, and a polite live region announces the new rank. Reordering is locked while the list is being sent.
- Verified in Hebrew (RTL) and English at 375px, including the stored ranks after a reordered POST and a reordered PUT.

## Notes
- \`@angular/cdk\` is now declared in \`client/package.json\`. It was already installed and locked at 21.2.14 as PrimeNG's peer dependency, so the version does not change.
- Dragging starts only from the handle, so touch scrolling on the rest of the card still works. Real-device touch dragging was not exercised (jsdom and the browser pane use mouse events). The arrows are the guaranteed path.
- No migration, no new endpoint, no backend change.

## Test plan
- [x] Domain: \`movePick\` promotes, demotes, clamps, ignores an unknown slot, keeps session type and constraint, never mutates
- [x] Page spec: arrows reorder and renumber, preferred boundary moves, first/last disabled, accessible names, single pick shows no controls, focus kept at the top, live announcement, grid badges after reorder, locked while sending, returning student's PUT carries the new order
- [x] Page spec: drop at a new rank reorders and submits, drop in place is a no-op, handles only with 2+ picks, dragging locked while sending
- [x] Browser (375px, HE + EN): arrows by keyboard, drag by handle, back to the grid, POST and PUT payload order, stored \`rank\` column, RTL mechanics, 40px targets, no horizontal scroll

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

Then bind the PR in the app (`ccd_pr` `get_status`, and `bind_pr` if it is not reported) and read its CI.

## Self-Review (done at planning time)

- **Spec coverage:** the AC's "Given multiple picks" → the 2+ pick precondition (decision 7); "When I move a pick to a different position" → task 2 (arrows) and task 3 (drag); "ranks are renumbered" → task 2 "renumbers the ranks to the new order" and Step 2.5; "that order is what the Submission carries" → task 2 "promotes a later pick and submits the new order" (POST) and "reorders a saved list and replaces it in the new order" (PUT), Steps 3.5 and 4.4 (stored `rank`). Requirements §7 step 8 "may reorder before submitting" → the review step, before Submit. §5.6 "Rank: position" → decision 1, no rank on the wire. §10 mobile → Step 6. Roadmap "drag-reorder" → task 3.
- **Placeholder scan:** every code step has full contents or an exact, anchored edit. The only conditional instructions are named fallbacks: task 3's `dropped.emit` helper if `triggerEventHandler` does not reach the output, and Step 3.1's retry if the pane's synthetic drag does not move.
- **Type consistency:** `PickMove { slotId, toIndex }` and `movePick(picks, move)` (task 1) are used unchanged by the store's `reorderPick(move: PickMove)` and the component's `moved` output (task 2) and by `onDropped` (task 3). `canReorder` / `movedPickRank` / `isSubmitting` have the same names in the store, the component inputs, the page bindings and the templates. Spec hooks `data-pick-id`, `.review__move-up|down button`, `.review__grip`, `.review__announcement`, `.review__reorder-hint` match the templates.
```
