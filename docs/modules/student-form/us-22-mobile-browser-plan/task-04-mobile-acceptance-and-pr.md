# Task 4 of 4: Mobile acceptance run (320 and 375px, Hebrew and English, every step and status screen), real-device check, roadmap link, PR

> Part of [US-22: Student Form Fully Usable on a Mobile Browser](README.md). Requires tasks 1–3 committed. Work on branch `23-us-22-mobile-browser`.

**Files:**
- Modify: `docs\modules\student-form\README.md` (link this plan from the slice-6 row)
- Local only, never staged: `.claude\launch.json` (an `api-smoke` entry added in Step 1, removed in Step 11)

**Interfaces:**
- Consumes: the client from tasks 1–3, the unchanged API (`GET api/submissions/by-link/{token}`, `POST …/identify`, `POST`/`PUT api/submissions/by-link/{token}`, admin `POST api/week-schedules`, `GET api/publications/by-week`, `POST api/publications/{id}/publish`).
- Produces: a verified slice and a PR closing #23.

Verify with `javascript_tool`, `get_page_text`, `read_page`, `find` and `read_network_requests`. Project memory: `screenshot` can hang or come back half-rendered or tiled on this PrimeNG app, so `await document.fonts.ready` (plus ~1.5s) first and trust the measurements over the picture.

**The audit helper.** Paste once per page load into `javascript_tool`. It is the acceptance criterion in code: no sideways page scroll, nothing visible past either viewport edge, no clipped horizontal overflow, every control at least 40×40px, and no input under 16px (iOS zooms on focus below that). It only looks inside the student shell, so the admin-only toast container is ignored (README decision 6), and it skips visually hidden screen-reader text (`.p-hidden-accessible` and the review's `clip-path: inset(50%)` live region), which is clipped on purpose.

**Driving the pane.** When the browser pane is not drawn on screen, `computer` clicks and typing do not reach the page. Drive the flow with DOM events instead (`input.focus(); input.value = …; input.dispatchEvent(new Event('input', { bubbles: true }))`, `button.click()`). The audit measures layout, which does not depend on how the event was raised. Physical touch is Step 7's job. The ID step looks the ID up as soon as nine digits are typed, so the found / welcome-back message appears without pressing `המשך`.

```js
const visuallyHidden = el => el.closest('.p-hidden-accessible') || getComputedStyle(el).clipPath === 'inset(50%)';
window.audit = () => {
    const width = window.innerWidth;
    const label = el => el.tagName.toLowerCase() + [...el.classList].map(name => '.' + name).join('');
    const shown = [...document.querySelectorAll('app-student-shell *')].filter(el => {
        const box = el.getBoundingClientRect();
        return box.width > 0 && box.height > 0 && !visuallyHidden(el);
    });
    const outside = shown
        .filter(el => { const box = el.getBoundingClientRect(); return box.left < -0.5 || box.right > width + 0.5; })
        .map(label);
    const clipped = shown
        .filter(el => el.scrollWidth > el.clientWidth + 1 && getComputedStyle(el).overflowX !== 'visible')
        .map(label);
    const smallTargets = shown
        .filter(el => el.matches('button, a[href], input:not([type=radio]), textarea, label:has(input[type=radio])'))
        .filter(el => { const box = el.getBoundingClientRect(); return box.width < 40 || box.height < 40; })
        .map(label);
    const zoomingInputs = shown
        .filter(el => el.matches('input, textarea, select') && parseFloat(getComputedStyle(el).fontSize) < 16)
        .map(label);
    return { sidewaysScroll: document.documentElement.scrollWidth > width, outside, clipped, smallTargets, zoomingInputs };
};
```

**A clean result** is `{ sidewaysScroll: false, outside: [], clipped: [], smallTargets: [], zoomingInputs: [] }`. "Audit" below means: call `audit()` and expect exactly that. If an audit is not clean, stop: fix it in the owning component (write a failing spec first where jsdom can see it), re-run the suites, commit the fix separately with its own message, then repeat the step.

- [ ] **Step 1: Bring up the stack against a throwaway database (bash / Git Bash)**

⚠️ The seed uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`).

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history) and create the throwaway database:
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us22_smoke
   ```
   If `createdb` reports that it exists from an earlier run, drop it first with `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us22_smoke`. It only ever holds this smoke data.
2. Add a **local-only** entry to the `configurations` array of `.claude\launch.json`. Never stage this file:
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us22_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first, since both use port 5080). Startup applies every migration to the empty database and seeds the dev admin. `preview_logs` must show no migration error.
4. Seed one teacher, a car, next week's grid (every slot open), a two-student roster and an open publication. The values are deliberately long, to stress the layout: a long Hebrew teacher name, a long Hebrew student name, a long car name. The national IDs are synthetic (valid check digits). Never use real IDs in test data.

   `curl` here is the Windows binary: a Hebrew string passed as an argument arrives as `?????`, and `-F file=@/tmp/…` fails with status `000`. So every Hebrew payload goes into a file, and `curl` runs from that directory with relative `@file` paths.

```bash
API=http://localhost:5080
WEEK=$(date -u -d 'next sunday' +%F)
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }
SEED_DIR=$(mktemp -d) && cd "$SEED_DIR"

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"

printf '%s' '{"name":"יהונתן בן-שמעון אברמוביץ׳","contactEmail":"smoke.teacher@example.com"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" -d @teacher.json | json id)
curl -s -o /dev/null -w "car: %{http_code}\n" -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"name":"Toyota Yaris Hybrid Automatic 2024","type":"Yaris","transmission":"automatic"}'
curl -s -o /dev/null -w "week schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$WEEK\"}"

printf '%s\n' \
  'שם מלא,תעודת זהות,טלפון,מורה,רכב' \
  'אלכסנדרה מונטגומרי-רוזנברג שטיינהרט,000000018,050-0000001,יהונתן בן-שמעון אברמוביץ׳,Toyota Yaris Hybrid Automatic 2024' \
  'Smoke Student B,000000026,050-0000002,יהונתן בן-שמעון אברמוביץ׳,Toyota Yaris Hybrid Automatic 2024' > roster.csv
curl -s -w "  ← import: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" -F "file=@roster.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 5
echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
echo "WEEK=$WEEK LINK=$LINK PUB_ID=$PUB_ID"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us22_smoke -c "select name from teachers;"
```

Expected: `car: 201`, `week schedule: 201`, `import: 201` with `"added":2,…,"failed":0`, `publish: 204`, `link state: open` (if it still says `published`, wait a few seconds for Quartz to fire the past-due open job), the `WEEK=… LINK=… PUB_ID=…` line, and the teacher name in Hebrew, **not** `?????`. If the import reports `"failed":2` with `unknownTeacher`, the teacher name was mangled: check `teacher.json` and the `select`. **Write down `LINK` and `PUB_ID`.** Keep this shell open: Step 8 uses `API`, `AUTH`, `json` and `WEEK` again.

- [ ] **Step 2: Client, viewport meta, audit helper**

1. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080). Navigate to `/s/{LINK}`.
2. Start **logged out** (`localStorage.removeItem('auth_token')`), in **Hebrew** (the default), then `resize_window` preset **mobile** (375×812) and reload.
3. `document.querySelector('meta[name=viewport]').content` → `width=device-width, initial-scale=1, interactive-widget=resizes-content` (task 3).
4. Paste the audit helper.

- [ ] **Step 3: First submission, Hebrew, 375×812 (the AC as written)**

Student **A** `000000018`. Audit after every numbered action.

1. ID step, empty field → audit. Tap the field (`find` "תעודת זהות" → `computer` `left_click` the ref), type `000000018`, tap `המשך` → the found message `מצאנו אתכם — אלכסנדרה מונטגומרי-רוזנברג שטיינהרט…` → audit.
2. `המשך` → details: teacher `יהונתן בן-שמעון אברמוביץ׳`, student, `אוטומטית`, `Toyota Yaris Hybrid Automatic 2024`, week → audit. Every value sits inside the card: `[...document.querySelectorAll('.details__card dd')].every(dd => dd.getBoundingClientRect().right <= document.querySelector('.details__card').getBoundingClientRect().right)` → `true`.
3. `המשך` → target: tap `יותר שיעורים` once → `2` → audit.
4. `לבחירת שעות` → slots → audit (the slot list is taller than the screen, so scroll to the bottom with `computer` `scroll` and audit again).
5. Tap `ראשון · אחה״צ` → the sheet opens → audit. Tap `כפול`, tap the constraint field, type `only after 16:00, and please pick me up from work at the corner of Herzl and Weizmann` → audit. Tap `הוספה כבחירה מס׳ 1`.
6. Add `שני · צהריים`, `שלישי · צהריים`, `שישי · בוקר` the same way (tap the chip, then the add button) → audit.
7. `לסקירת הרשימה` → review: four rows, ranks 1–2 preferred → audit. Tap the rank-4 **up** arrow once (`find` the button named `העברת בחירה 4 … למעלה`, `computer` `left_click` its ref) → `שישי · בוקר` is rank 3 → audit.
8. `שליחה` → `read_network_requests` (`urlPattern: "by-link"`): one **`POST`** → **204** → confirmation `נשלח` → audit.

- [ ] **Step 4: Returning student, Hebrew, 320×568, and the constraint's direction (Review Focus 1, 4)**

`resize_window` width **320**, height **568**, reload, paste the audit helper.

1. Type `000000018` → `המשך` → the welcome-back banner `ברוכים השבים, …` and the button `עריכת ההגשה` → audit.
2. `עריכת ההגשה` → details → audit → `המשך` → target `2` → audit → `לבחירת שעות` → slots, the four rank badges showing → audit.
3. Tap `ראשון · אחה״צ` (rank 1) → the sheet opens with `כפול` selected and the English constraint → audit. D1 in the textarea (task 1):
   ```js
   const field = document.querySelector('#pick-constraint');
   [field.getAttribute('dir'), getComputedStyle(field).direction]
   ```
   → `["auto", "ltr"]`: the English text lays out left-to-right inside the Hebrew page. Tap `ביטול`.
4. `לסקירת הרשימה` → review with the replaces notice `כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.` → audit. D1 on the review:
   ```js
   const quote = document.querySelector('.review__constraint');
   [quote.getAttribute('dir'), getComputedStyle(quote).direction, getComputedStyle(document.querySelector('.review__meta')).direction]
   ```
   → `["auto", "ltr", "rtl"]`. Screenshot proof (fonts ready first): the quote marks open before `only` and close after `Weizmann`. Before task 1 the closing mark rendered before `only` (README D1).
5. Every time range is on one line (task 2): `[...document.querySelectorAll('.review__time')].map(time => time.getClientRects().length)` → all `1`.
6. Move rank 1 **down** once with its arrow → `שני · צהריים` is rank 1. `שליחה` → one **`PUT`** → **204** → `השינויים נשמרו` → audit.

- [ ] **Step 5: First submission, English, 320×568, the Hebrew constraint and the time ranges (Review Focus 1, 2)**

Reload, press `EN`, paste the audit helper. `document.documentElement.dir` → `"ltr"`. Student **B** `000000026`. Audit after every numbered action.

1. ID step → audit. Type `000000026` → `Continue` → `Found you — Smoke Student B.` → audit. `Continue` → details → audit. `Continue` → target `1` → audit. `Pick slots` → slots → audit (top and bottom).
2. Tap `Sunday · Afternoon` → the sheet → audit. Tap `Double`, type the constraint `רק אחרי 16:00 ולאסוף אותי מהעבודה ברחוב הרצל` → audit, and:
   ```js
   const field = document.querySelector('#pick-constraint');
   [field.getAttribute('dir'), getComputedStyle(field).direction]
   ```
   → `["auto", "rtl"]`. Tap `Add as pick #1`.
3. Add `Wednesday · Afternoon` and `Thursday · Evening` → `Review my list` → review → audit.
4. **D2 (task 2):** this is the case the planning audit measured. `[...document.querySelectorAll('.review__time')].map(time => time.getClientRects().length)` → `[1, 1, 1]`. `document.querySelector('.review__time').textContent` → `15:00–18:00`.
5. **D1 reversed:** `[document.querySelector('.review__constraint').getAttribute('dir'), getComputedStyle(document.querySelector('.review__constraint')).direction]` → `["auto", "rtl"]`. Screenshot: the Hebrew text reads right-to-left with its quote marks at both ends.
6. Reorder with the arrows (rank 3 up once) → audit. `Submit` → one **`POST`** → **204** → `Submitted` → audit.

- [ ] **Step 6: English 375×812 pass and the RTL/LTR mechanics**

`resize_window` preset **mobile**, reload (still English), paste the audit helper. Identify **B** → `Edit my submission` → walk details → target → slots → review, auditing each screen. Leave without submitting (reload). Then press `עב` and repeat for **B** in Hebrew at 375px up to the review, auditing each screen. On that Hebrew review:

- `document.documentElement.dir` → `"rtl"`; the grip is on the right and the arrows on the left (slice 4), and no time label is mirrored: `[...document.querySelectorAll('.review__time, .pick-sheet__time, .slot-chip__time')].every(time => time.getAttribute('dir') === 'ltr')` → `true`.
- No desktop-only interaction in the student form's code. Run from the repo root:
  ```bash
  grep -rnE "\(dblclick\)|\(mouseenter\)|\(mouseover\)|\(mouseleave\)|\(contextmenu\)|:hover|keydown|keyup" client/src/app/features/student-form --include=*.html --include=*.scss --include=*.ts | grep -v "\.spec\.ts"
  ```
  → exactly one line, `pick-sheet.component.ts: host: { '(document:keydown.escape)': 'cancelled.emit()' }`. Escape is a shortcut, never the only way: the backdrop and `ביטול` close the sheet too (planning audit).
- Reload to leave without submitting.

- [ ] **Step 7: Real phone over the LAN (human-run; Review Focus 3, slice 4 open item 1)**

Ask your human partner whether a phone is available. If not, write "real-device check not run" in the PR notes and go to Step 8. Otherwise, your human partner does this; the agent never changes firewall or system settings:

1. Stop the pane's `client` server (`preview_stop`). In a terminal in `client\`: `npx ng serve --host 0.0.0.0`. If Windows asks whether to allow Node.js on private networks, your human partner decides.
2. Find the PC's LAN address (`ipconfig` → the Wi-Fi adapter's IPv4 address). On the same Wi-Fi, send the link `http://{PC-IP}:4200/s/{LINK}` **to themselves on WhatsApp** and open it from the chat (the real channel: on Android it opens in Chrome or a Chrome Custom Tab, on iPhone in Safari).
3. As student **B** (`000000026`): the numeric keypad opens for the ID; walk every step to the review; in the pick sheet, tap the constraint field: **on Android the sheet rises above the keyboard and Save stays visible without closing the keyboard** (D3, task 3). On iPhone, Save is reachable after tapping Done (open item 2). Drag a pick by its grip with a finger (slice 4 open item 1), use the arrows, and try a sideways swipe on every screen: the page never scrolls sideways. Pinch-zoom works. Read the chip times (open item 3).
4. Submit (`PUT`, B already has a list). Record the device, OS and browser and each check's result for the PR notes.
5. Stop the terminal server and `preview_start {name:"client"}` again.

- [ ] **Step 8: Status screens at 320×568, both languages (Review Focus 5)**

`resize_window` width **320**, height **568**. For each screen: audit in Hebrew, press `EN`, audit again, press `עב`.

1. **Invalid link:** `/s/not-a-real-token` → `הקישור אינו תקין` → audit (HE, EN).
2. **Not on the roster:** `/s/{LINK}`, type `000000034` (synthetic, valid check digit, not in the roster) → `המשך` → `אינכם מופיעים אצלנו.` → audit (HE, EN).
3. **Invalid ID:** clear the field, type `123456789` → `המשך` → `זה לא נראה כמו מספר תעודת זהות תקין. בדקו את הספרות ונסו שוב.` → audit (HE, EN).
4. **Window closed mid-submit:** a window can only be extended, never shortened (`extend-window` to an earlier end returns 409 `WindowExtensionMustBeLaterException`), so publish the following week with a four-minute window and let the close job end it. In the Step-1 shell:
   ```bash
   TEACHER_ID=$(docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us22_smoke -t -A -c "select id from teachers limit 1;")
   WEEK2=$(date -u -d "$WEEK +7 days" +%F)
   curl -s -o /dev/null -w "week2 schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
     -H "Content-Type: application/json" -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$WEEK2\"}"
   PUB2=$(curl -s "$API/api/publications/by-week?week=$WEEK2" -H "$AUTH")
   PUB2_ID=$(json id <<< "$PUB2"); LINK2=$(json linkToken <<< "$PUB2")
   curl -s -o /dev/null -w "publish week2: %{http_code}\n" -X POST $API/api/publications/$PUB2_ID/publish -H "$AUTH" \
     -H "Content-Type: application/json" \
     -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+4 minutes' +%Y-%m-%dT%H:%M:%SZ)\"}"
   echo "LINK2=$LINK2"
   ```
   → `week2 schedule: 201`, `publish week2: 204`. Right away, in the pane open `/s/{LINK2}`, identify **A**, walk to the slots, add one pick, open the review and **stop**. Wait until `curl -s $API/api/submissions/by-link/$LINK2 | json state` → `closed`. Then press `שליחה` → `POST` **409** → `החלון נסגר ממש עכשיו` → audit (HE, EN).
5. **Closed:** reload `/s/{LINK2}` → `ההגשה נסגרה` → audit (HE, EN).

- [ ] **Step 9: PII**

`read_network_requests`: the national IDs appear only in request **bodies** (identify, POST, PUT), never in a URL. `Object.keys(localStorage)` holds no national ID. `preview_logs` for `api-smoke` does not contain `000000018` or `000000026`.

- [ ] **Step 10: Link this plan from the roadmap, full check, commit**

In `docs\modules\student-form\README.md`, change the slice-6 table row's first cell from `6. Mobile acceptance` to:

```markdown
6. Mobile acceptance — [plan](us-22-mobile-browser-plan/README.md)
```

```bash
dotnet build
dotnet test
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES and builds clean, with no new warnings. The backend is untouched, so `dotnet test` only confirms nothing else moved.

```bash
git status --short
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 6 plan from the roadmap

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

`git status --short` before the `add` must show only `docs/modules/student-form/README.md` and the local `.claude/launch.json`. Do not stage the latter.

- [ ] **Step 11: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server and the `client` server.
2. Remove the `api-smoke` entry from `.claude\launch.json`. `git diff .claude/launch.json` must be empty afterwards.
3. Drop the throwaway database: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us22_smoke`. Remove the seed directory: `rm -rf "$SEED_DIR"`.
4. `resize_window` preset **desktop**.

- [ ] **Step 12: Rebase, push and open the PR** (only once your human partner has approved pushing)

PR [#69](https://github.com/silagy/DrivingLessonsBooking/pull/69) (slice 4) must be merged first: `gh pr view 69 --repo silagy/DrivingLessonsBooking --json state` → `"MERGED"`. If it is still open, stop and tell your human partner. Then:

```bash
git fetch origin
git rebase origin/main
git log --oneline origin/main..HEAD
```

Expected: only this slice's commits (the plan docs, tasks 1–3, the roadmap link, plus any audit fixes from this task). Slice 4's commits are already on `main` and drop out. Re-run `npm test -- --watch=false` and `npm run build` in `client\` after the rebase.

```bash
git push -u origin 23-us-22-mobile-browser
gh pr create --repo silagy/DrivingLessonsBooking --title "US-22: Student form is fully usable on a mobile browser" --body "Closes #23

## Summary
- Acceptance run of the whole student flow on a phone: national ID → read-only details → target → slot picking (bottom sheet) → ranking → submit, plus return-to-edit and every status screen, at **375px and 320px**, in **Hebrew (RTL) and English**. No horizontal scroll, nothing past the viewport edge, every control at least 40×40px, no input under 16px (no iOS zoom on focus), no hover- or keyboard-only interaction.
- Three fixes found by the audit:
  - A constraint typed in the other script than the page (English on the Hebrew form, or the reverse) rendered with its quote marks flipped. The textarea and the review now give it its own direction (\`dir=\"auto\"\`).
  - On a 320px phone the review's time range broke in the middle (\`15:00–\` / \`18:00\`). Time ranges now stay on one line.
  - On Android the keyboard covered the pick sheet's Save button and the sticky footer. The viewport meta now has \`interactive-widget=resizes-content\`, so the layout shrinks above the keyboard.

## Notes
- No backend change, no migration, no new endpoint, no new package, no new translation key.
- Real device: <device / OS / browser and results from Step 7, or \"not run — no phone available\">.
- iOS Safari ignores \`interactive-widget\`: with the keyboard open, Save is reachable after tapping Done.
- Chip time labels stay at the mockup's size (≈9px). A larger size does not fit a 320px chip. Flagged for design.

## Test plan
- [x] Page spec: the pick-sheet textarea and the review's constraint carry \`dir=\"auto\"\`
- [x] Browser, 375px HE: first submission end to end, audit clean on every screen, POST 204
- [x] Browser, 320px HE: returning student edit, constraint direction in textarea and review, time ranges on one line, PUT 204
- [x] Browser, 320px EN: first submission with a Hebrew constraint, time ranges on one line, POST 204
- [x] Browser, 375px EN and HE: returning walk-through, RTL mechanics, no desktop-only handlers
- [x] Browser, 320px HE + EN: invalid link, not on roster, invalid ID, window closed mid-submit (409), closed
- [x] National ID only in request bodies

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

Then bind the PR in the app (`ccd_pr` `get_status`, and `bind_pr` if it is not reported) and read its CI.

## Self-Review (done at planning time)

- **Spec coverage:** AC "Given I received the weekly link via WhatsApp" → Step 7.2 opens the link from a WhatsApp chat on a real phone. "I open it in a mobile browser (~375px viewport)" → Steps 3 and 6 at 375×812, plus 320×568 in Steps 4, 5 and 8 (decision 8). "national ID entry" → Steps 3.1, 4.1, 5.1, 8.2–8.3. "read-only details confirmation" → Step 3.2 with long values. "target count" → Step 3.3. "slot picking" → Steps 3.4–3.6 and 5.2–5.3, the bottom sheet included. "ranking" → the arrows in Steps 3.7, 4.6, 5.6, and dragging on the real phone in Step 7.3. "and submit" → POST in Steps 3.8 and 5.6, PUT in Step 4.6. "without horizontal scrolling" → the audit's `sidewaysScroll` / `outside` / `clipped` on every screen. "or desktop-only interactions" → the audit's `smallTargets`, the handler grep in Step 6, and touch in Step 7. Notes "no native app in v1 (§3)" → nothing installs. "no teacher-confirmation step (ADR 0003)" → the walk goes ID → details → target, with no teacher choice. Requirements §10 → the whole task. Roadmap slice 6 "closes last as the ~375px end-to-end acceptance check" → this task, after slices 1–5.
- **Placeholder scan:** every code step has full contents or an exact, anchored edit. The only conditional instructions are named fallbacks: the audit's stop-and-fix rule, Step 7's "not run" path when no phone is available, and Step 12's stop if #69 is not merged. The PR body's `<device / OS / browser …>` is filled from Step 7's record.
- **Type consistency:** the DOM hooks used here exist in the code or are added by tasks 1–3: `#pick-constraint`, `.review__constraint`, `.review__meta`, `.review__time`, `.pick-sheet__time`, `.slot-chip__time` (all with `dir="ltr"` on time spans, unchanged), `.details__card dd`, `meta[name=viewport]`. The expected attribute values match tasks 1 and 3 exactly (`dir="auto"`, `interactive-widget=resizes-content`). The UI strings quoted are the current `en.json` / `he.json` values.
