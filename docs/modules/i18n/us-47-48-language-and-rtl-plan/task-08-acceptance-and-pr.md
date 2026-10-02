# Task 8 of 8: Both-languages acceptance sweep of every screen, rule-violation toasts end to end, PR

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–7 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Local only, never staged: `.claude\launch.json` (an `api-smoke` entry added in Step 1, removed in Step 10)
- No source change, unless a check fails (then: fix in the owning file, with a failing spec first where jsdom can see it, and commit the fix on its own)

**Interfaces:**
- Consumes: everything from tasks 1–7: `code` on 404/409 bodies, `errors.*` keys, `LanguageService.locale` / `isRtl`, PrimeNG labels, tab title, toggle `aria-label` / `lang`, Israeli formats, history range key, toast corner, button-icon order, date-picker navigation, popover anchor, `match-parent` alignment, `--app-caps-tracking`. The unchanged API: `POST api/auth/login`, `POST api/teachers`, `POST api/cars`, `POST api/week-schedules`, `POST api/roster-imports`, `GET api/publications/by-week`, `POST api/publications/{id}/publish`, `POST api/publications/{id}/extend-window`, `GET api/submissions/by-link/{token}`.
- Produces: a verified slice and a PR closing #47 and #48.

Verify with `javascript_tool`, `get_page_text`, `read_page`, `find` and `read_network_requests`. Project memory: `screenshot` can hang or come back half-rendered on this PrimeNG app, so `await document.fonts.ready` (plus ~1.5s) first and trust the measurements over the picture. Open PrimeNG overlays (date picker, popover, select) with `computer` `left_click`, not a scripted `.click()`: when the pane is not drawn their enter animation can leave them at zero size.

**The audit helper.** Paste once per page load into `javascript_tool`. It is US-47's acceptance criterion in code: no visible text node and no `placeholder` / `aria-label` / `title` / `alt` in the other language's script, no raw translation key, and a tab title in the right script. `data` lists the seeded values (names, emails, file names) that are legitimately in either script.

```js
window.i18nAudit = (data = []) => {
    const lang = document.documentElement.lang;
    const wrongScript = lang === 'he' ? /[A-Za-z]{2,}/ : /[֐-׿]/;
    const keyLike = /^[a-z][A-Za-z]*(\.[A-Za-z0-9]+)+$/;
    const allowed = new Set(['WeekDrive', 'EN', 'עב', 'English', 'עברית', 'dropdown trigger', ...data]);
    const cleaned = text => text.replace(/\bCSV\b|\bv\d+\b|https?:\/\/\S+/g, '');
    const offends = text => !allowed.has(text) && (wrongScript.test(cleaned(text)) || keyLike.test(text));
    const visible = el => { const box = el.getBoundingClientRect(); return box.width > 0 && box.height > 0; };
    const findings = new Set();
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const text = node.textContent.trim();
        if (text && visible(node.parentElement) && offends(text)) {
            findings.add('text: ' + text.slice(0, 80));
        }
    }
    for (const el of document.querySelectorAll('[placeholder], [aria-label], [title], [alt]')) {
        for (const name of ['placeholder', 'aria-label', 'title', 'alt']) {
            const value = el.getAttribute(name)?.trim();
            if (value && offends(value)) {
                findings.add(name + ': ' + value.slice(0, 80));
            }
        }
    }
    return { lang, dir: document.documentElement.dir, titleInScript: !wrongScript.test(document.title), findings: [...findings] };
};
```

`EN` / `עב` / `English` / `עברית` are the toggle's endonyms (roadmap decision 3), `dropdown trigger` is PrimeNG's hardcoded select-chevron label (README open item 2), and `CSV`, version pills (`v1`) and share links are allowed inside text. **A clean result** is `{ lang, dir, titleInScript: true, findings: [] }` with `dir` = `rtl` for `he` and `ltr` for `en`. "Audit" below means: call `i18nAudit(SEED)` (Step 2 defines `SEED`) and expect exactly that. If an audit is not clean, stop: fix it in the owning file, re-run the suites, commit the fix separately, then repeat the step.

- [ ] **Step 1: Bring up the stack against a throwaway database (bash / Git Bash)**

⚠️ The seed uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`).

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history) and create the throwaway database:
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us47_smoke
   ```
   If `createdb` reports that it exists from an earlier run, drop it first with `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us47_smoke`.
2. Add a **local-only** entry to the `configurations` array of `.claude\launch.json`. Never stage this file:
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us47_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first; both use port 5080). Startup applies every migration and seeds the dev admin. `preview_logs` must show no migration error.
4. Seed two teachers (one Hebrew name, one Latin name whose email **starts with digits**, Review Focus 5), a car with a Latin name, next week's grid for the Hebrew teacher, a two-student roster (one Hebrew name, one English name; synthetic IDs with valid check digits) and an open publication. `curl` is the Windows binary, so every Hebrew payload goes into a file and `curl` runs from that directory with relative `@file` paths.

```bash
API=http://localhost:5080
WEEK=$(date -u -d 'next sunday' +%F)
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }
SEED_DIR=$(mktemp -d) && cd "$SEED_DIR"

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"

printf '%s' '{"name":"יהונתן בן-שמעון","contactEmail":"smoke.teacher@example.com"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" -d @teacher.json | json id)
curl -s -o /dev/null -w "teacher B: %{http_code}\n" -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"name":"Smoke Teacher B","contactEmail":"2026.smoke@example.com"}'
curl -s -o /dev/null -w "car: %{http_code}\n" -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"name":"Toyota Yaris Hybrid 2024","type":"Yaris","transmission":"automatic"}'
curl -s -o /dev/null -w "week schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$WEEK\"}"

printf '%s\n' \
  'שם מלא,תעודת זהות,טלפון,מורה,רכב' \
  'אלכסנדרה מונטגומרי,000000018,050-0000001,יהונתן בן-שמעון,Toyota Yaris Hybrid 2024' \
  'Smoke Student B,000000026,050-0000002,יהונתן בן-שמעון,Toyota Yaris Hybrid 2024' > roster.csv
curl -s -w "  ← import: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" -F "file=@roster.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 5
echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
echo "WEEK=$WEEK LINK=$LINK PUB_ID=$PUB_ID TEACHER_ID=$TEACHER_ID"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us47_smoke -c "select name from teachers;"
```

Expected: `teacher B: 201`, `car: 201`, `week schedule: 201`, `import: 201` with `"added":2,…,"failed":0`, `publish: 204`, `link state: open` (if it still says `published`, wait a few seconds for Quartz), the `WEEK=… LINK=… PUB_ID=… TEACHER_ID=…` line, and both teacher names readable, **not** `?????`. **Write down `LINK`, `PUB_ID`, `WEEK` and `TEACHER_ID`.** Keep this shell open: Step 5 uses `API`, `AUTH`, `json`, `PUB_ID`, `TEACHER_ID` and `WEEK`.

- [ ] **Step 2: Static checks (repo root)**

1. Every exception has its key in both files: re-run the script from task 2 Step 7. Expected: `en codes: 56 missing: [] extra: []` and `he codes: 56 missing: [] extra: []`.
2. Keys mirrored: re-run the script from task 4 Step 9. Expected: `en 350 he 350 only en [] only he []`.
3. No directional glyph in a template, a TypeScript file or a translation (a Node script, because Git Bash's `grep -P` is not reliably UTF-8):
   ```bash
   node -e "
   const fs = require('fs'), path = require('path');
   const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]);
   const files = [...walk('client/src/app'), ...walk('client/public/i18n')].filter(f => /\.(html|ts|json)$/.test(f) && !f.endsWith('.spec.ts'));
   const hits = files.flatMap(f => fs.readFileSync(f, 'utf8').split('\n').map((line, i) => [f, i + 1, line]))
     .filter(([, , line]) => /[←-⇿‹›«»▶◀]/.test(line));
   console.log(hits.length ? hits.map(([f, n, l]) => f + ':' + n + ': ' + l.trim()).join('\n') : 'no directional glyphs');
   "
   ```
   Expected: `no directional glyphs`.
4. No physical layout property in app styles except the PrimeNG resets:
   ```bash
   grep -rnE "(margin|padding|border)-(left|right)|(^|[^-])(left|right)\s*:|text-align:\s*(left|right)|float:" client/src --include=*.scss
   ```
   Expected: exactly the two `margin-left: 0;` and the one `left: auto;` lines in `client/src/styles/_global.scss` (task 6).
5. No formatter on the bare language: `grep -rn "language.lang()" client/src/app --include=*.ts | grep -v "\.spec\.ts"` → only the two template lines in `language-toggle.component.ts`.

In the pane, define the seeded values once per page load, before the audit helper:

```js
window.SEED = ['יהונתן בן-שמעון', 'Smoke Teacher B', 'smoke.teacher@example.com', '2026.smoke@example.com',
    'Toyota Yaris Hybrid 2024', 'Yaris', 'אלכסנדרה מונטגומרי', 'Smoke Student B', 'admin@local.dev', 'AD', 'SB', 'roster.csv'];
```

- [ ] **Step 3: Client and sign-in**

1. `preview_start {name:"client"}` → `http://localhost:4200`. `resize_window` width **1280**, height **800**.
2. Open `/login`. `localStorage.removeItem('app_lang')` and reload: Hebrew, `dir` `rtl`, `document.title` → `מערכת תכנון שיעורי נהיגה`. The toggle: `document.querySelector('.lang').getAttribute('aria-label')` → `שפה`; `[...document.querySelectorAll('.lang__opt')].map(b => b.lang)` → `["en", "he"]`. Paste `SEED` and the audit helper → audit.
3. Sign in with the dev admin (local test credentials from `docs\development\running-the-project.md`).

- [ ] **Step 4: A returning English user, cold (Review Focus 3)**

1. `localStorage.setItem('app_lang', 'en')` and reload `/`. Before touching anything: `[document.documentElement.lang, document.documentElement.dir, document.title]` → `["en", "ltr", "Driving Lessons Planner"]`. Paste `SEED` and the audit helper → audit.
2. Open `/publications`, pick `יהונתן בן-שמעון` and next week (`WEEK`) if not already selected, press `Extend deadline`, and click the date field (`computer` `left_click`). `document.querySelector('.p-datepicker-prev-button')?.getAttribute('aria-label')` → `Previous Month`; the dialog's close button: `document.querySelector('.p-dialog-close-button, .p-dialog-header-icon')?.getAttribute('aria-label')` → `Close`. Escape the panel, `Cancel` the dialog.
3. Press `עב`. The same two checks → `החודש הקודם` and `סגירה` (re-open the dialog and the panel first), and `document.title` → `מערכת תכנון שיעורי נהיגה`. `Cancel` the dialog.

- [ ] **Step 5: Rule-violation toasts in the selected language, end to end (L1)**

1. **Through the UI, Hebrew.** On `/roster`, feed the hidden file input a CSV with the wrong columns (the roster upload button opens a native file dialog the pane cannot drive):
   ```js
   const input = document.querySelector('input[type=file]');
   const file = new File(['name,phone\nSomeone,0500000000\n'], 'wrong-columns.csv', { type: 'text/csv' });
   const transfer = new DataTransfer(); transfer.items.add(file);
   input.files = transfer.files; input.dispatchEvent(new Event('change', { bubbles: true }));
   ```
   The import fails before touching any student (the file is rejected whole). `read_network_requests` (`urlPattern: "roster-imports"`): one `POST` → **409**, response body with `"code":"rosterFileMustContainRequiredColumns"`. The toast reads `בקובץ חסרות עמודות חובה. השורה הראשונה צריכה לכלול: שם מלא, תעודת זהות, טלפון, מורה, רכב.`, contains no `Roster file must` and no GUID, and sits top-left: `[document.querySelector('.p-toast').style.left, document.querySelector('.p-toast').style.right]` → `["20px", ""]`.
2. Press `EN` and feed the same file again → the toast reads `The file is missing required columns. Its first row must include: שם מלא, תעודת זהות, טלפון, מורה, רכב.` and sits top-right (`["", "20px"]`). The Hebrew column names inside it are the literal CSV headers (task 2 Step 6); the audit flags them while the toast is visible, so audit this page after the toast has gone. Press `עב`.
3. **Through the API, two more codes.** In the Step-1 shell:
   ```bash
   curl -s -X POST $API/api/week-schedules -H "$AUTH" -H "Content-Type: application/json" \
     -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$WEEK\"}"; echo
   curl -s -X POST $API/api/publications/$PUB_ID/extend-window -H "$AUTH" -H "Content-Type: application/json" \
     -d "{\"newEndUtc\":\"$(date -u -d '-1 hour' +%Y-%m-%dT%H:%M:%SZ)\"}"; echo
   curl -s -o /dev/null -w "login: %{http_code}\n" -X POST $API/api/auth/login -H "Content-Type: application/json" \
     -d '{"email":"admin@local.dev","password":"not-the-password"}'
   ```
   Expected: the first body has `"status":409` and `"code":"weekScheduleAlreadyExists"`, the second `"status":409` and `"code":"windowExtensionMustBeLater"`, and `login: 401`. Both 409 bodies still carry their English `detail` (for developers, README decision 4).

- [ ] **Step 6: Every admin screen, Hebrew then English (US-47 AC and US-48 AC)**

For each screen below: open it in Hebrew, paste `SEED` and the audit helper, audit, run the RTL checks listed, then press `EN`, audit again and run the English checks, then press `עב`.

1. **Dashboard** `/`: audit (HE, EN).
2. **Cars & teachers** `/teachers`:
   - Audit (HE, EN).
   - Section headings have no tracking in Hebrew (task 7 Step 5.5).
   - The teacher cards' emails line up with their names, including `2026.smoke@example.com`, which starts with digits: task 7 Step 5.3, and in Hebrew `document.querySelectorAll('.card__email')[1].textContent.trim()` → `2026.smoke@example.com` (not reordered).
   - The popover opens from its trigger: task 6 Step 7.1 in Hebrew, 7.2 in English. With it open, audit (HE, EN).
   - Press `הוספת מורה` / `Add teacher`: the dialog's close-button `aria-label` is `סגירה` / `Close`; audit (HE, EN); `ביטול` / `Cancel`.
   - Press a car's delete icon: the confirm dialog's message shows the Latin car name in its own direction (`Toyota Yaris Hybrid 2024` reads left to right inside the Hebrew sentence); audit (HE, EN); cancel.
3. **Roster** `/roster`:
   - Audit (HE, EN).
   - The upload icon sits before its label in reading order (task 5 Step 6.3).
   - The ID and phone cells line up with their columns (task 7 Step 5.4).
   - The "processed" timestamp is 24-hour in English: `/\b\d{1,2}:\d{2}\b/.test(document.body.innerText) && !/\b(AM|PM)\b/.test(document.body.innerText)` → `true`.
4. **Weekly prep** `/week-schedules`, pick `יהונתן בן-שמעון` and `WEEK`:
   - Audit (HE, EN).
   - Day dates are day-first in both: `[...document.querySelectorAll('.week-grid__day-date')].map(d => d.textContent.trim())` → `D.M` in Hebrew (e.g. `11.10`) and `DD/MM` in English (e.g. `11/10`), never month-first.
   - In Hebrew, Sunday is at the inline-start edge: `document.querySelector('.week-grid__day-head').getBoundingClientRect().left > [...document.querySelectorAll('.week-grid__day-head')].at(-1).getBoundingClientRect().left` → `true`.
   - Window times read left to right in both: `07:00–12:00`.
   - The week picker labels are day-first in English (for example `11 Oct – 16 Oct 2026`).
5. **Publications** `/publications`, same teacher and week (the open publication):
   - Audit (HE, EN).
   - The stamp reads `הנתונים נכון ל-…` / `Data as of …` with a 24-hour time.
   - Press `הארכת מועד` / `Extend deadline`. Task 5 Step 6.4 (date-picker navigation) in both. Pick any day in the open panel: the field then shows the day before the month (for example `ה׳, 15 אוק׳ 14:30` / `Thu, 15 Oct 14:30`). Audit (HE, EN) with the panel open. Cancel, which saves nothing.
6. **History** `/publications/history`:
   - Audit (HE, EN).
   - The week cell reads like `11 באוק׳ – 16 באוק׳ 2026` / `11 Oct – 16 Oct 2026`, not an ISO date (`/\d{4}-\d{2}-\d{2}/.test(document.querySelector('.history__card tbody td').textContent)` → `false`).
   - The window cell joins the two times with `–` and contains no arrow: `!/[→←]/.test(document.querySelector('.history__window').textContent)` → `true`.
7. **Login** (sign out): audit (HE, EN); the email field is `dir="ltr"`; task 5 Step 6.2 (arrow).

- [ ] **Step 7: Student form, both languages, switching mid-flow (US-47 "the student form")**

The US-22 slice ran the full student matrix at 320/375px on 2 October 2026. Here the question is US-47's: does switching language on any step change every string and keep the student's work?

1. `localStorage.removeItem('auth_token')`, `resize_window` preset **mobile**, open `/s/{LINK}`. Hebrew. Paste `SEED` and the audit helper → audit.
2. Type `000000026` (synthetic, `Smoke Student B`) → `המשך` → details → audit → `המשך` → target `1` → `לבחירת שעות` → slots → audit. The day labels are `D.M` (`11.10`).
3. Pick `ראשון · אחה״צ`, type the constraint `pick me up from work`, add it. Open the review (`לסקירת הרשימה`) → audit.
4. Press `EN` **on the review**. The same pick is still rank 1 with its constraint: `document.querySelector('.review__constraint').textContent.trim()` → `pick me up from work`. Audit. Press `Back` to the slots: day labels are `DD/MM` (`11/10`); audit. Press `עב` → audit.
5. Leave without submitting (reload). `resize_window` preset **desktop**.

- [ ] **Step 8: PII**

`read_network_requests`: the national ID `000000026` appears only in request **bodies**, never in a URL. `Object.keys(localStorage)` holds no national ID (`app_lang` and, after admin sign-in, `auth_token` only). `preview_logs` for `api-smoke` does not contain `000000018` or `000000026`.

- [ ] **Step 9: Full check**

Run (repo root): `dotnet build` and `dotnet test`. Expected: the existing warnings only, every test PASSES.

Run (in `client\`): `npm test -- --watch=false` and `npm run build`. Expected: every spec PASSES, the build is clean.

`git status` → clean apart from `.claude/launch.json`. If Steps 2–8 led to fixes, each is already its own commit.

- [ ] **Step 10: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` and `client` servers.
2. Remove the `api-smoke` entry from `.claude\launch.json`. `git diff .claude/launch.json` must be empty afterwards.
3. Drop the throwaway database: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us47_smoke`. Remove the seed directory: `rm -rf "$SEED_DIR"`.
4. `resize_window` preset **desktop**.

- [ ] **Step 11: Push and open the PR** (only once your human partner has approved pushing)

```bash
git fetch origin
git rebase origin/main
git log --oneline origin/main..HEAD
```

Expected: only this slice's commits (the plan, tasks 1–7, and any fixes from this task). Re-run `dotnet test` and, in `client\`, `npm test -- --watch=false` and `npm run build` if the rebase brought anything in.

```bash
git push -u origin 47-us-47-48-language-and-rtl
gh pr create --repo silagy/DrivingLessonsBooking --title "US-47 + US-48: Hebrew/English toggle with no leftovers, mirror-correct RTL" --body "Closes #47
Closes #48

## Summary
- **Rule violations in the selected language.** Every 404/409 now carries the broken rule as \`code\` (the exception name, camelCase). The client maps it to an \`errors.*\` key, with all 56 codes translated in both languages, and never shows the English \`detail\` again. A code without a key falls back to a translated generic message.
- **PrimeNG speaks Hebrew.** Date picker, select, dialog and toast labels are translated, and switching back restores English.
- **The browser tab is named in the selected language.** The toggle has an accessible name and per-option \`lang\`.
- **Israeli formats in English.** \`en-IL\` / \`he-IL\` everywhere: day before month, 24-hour clock, matching the slot windows. The history week column is formatted and its window range uses a direction-neutral dash. The \"data as of\" stamp is one translated sentence.
- **Mirror-correct RTL fixes:** toasts top-left in Hebrew; button icons keep their logical side; date-picker previous/next follow the mirrored grid; the assign-teachers popover opens from its trigger; isolated LTR values (emails, IDs, phones) line up with their neighbours; no letter-spacing on Hebrew labels.

## Notes
- No migration, no new endpoint, no new package.
- API contract: \`ProblemDetails\` gains \`code\` on 404/409 (additive). \`detail\` is unchanged.
- Out of scope (open items): a Hebrew webfont, PrimeNG's hardcoded \`dropdown trigger\` label, dead translation keys, the admin header below ~900px in English.

## Test plan
- [x] Filter: 404/409 carry \`code\`, closed-window keeps its problem type, 401 carries none
- [x] ToastService: code → key, generic fallbacks, never \`detail\`
- [x] LanguageService: locale/direction, PrimeNG labels in both languages and back, tab title, saved language
- [x] Toggle: group label, per-option \`lang\`; toast corner by direction; week grid and student slots day-first in \`en-IL\`; popover placement
- [x] Every exception class has an \`errors.*\` key in both files; en/he keys mirrored
- [x] Browser, HE + EN: every admin screen and dialog audited for wrong-script text and labels; RTL placement of toasts, icons, date picker, popover, isolated values
- [x] Browser: rule-violation toast end to end in both languages (409 roster upload); \`code\` on API 409s
- [x] Browser: student form, language switched on the review step keeps the picks
- [x] National ID only in request bodies

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

Then bind the PR in the app (`ccd_pr` `get_status`, and `bind_pr` if it is not reported) and read its CI.

## Self-Review (done at planning time)

- **Spec coverage.**
  - US-47 "Given I am on any screen of the admin app or the student form" → Step 6 walks every admin route and dialog; Step 7 covers the student form, whose full matrix US-22 ran.
  - "a visible language toggle is present" → Step 3.2 (login), every Step-6 screen (shell), Step 7.1 (student header).
  - "When I switch the language" → `EN` / `עב` on every screen, mid-flow in Step 7.4.
  - "every user-visible string changes … with no hardcoded leftovers" → the audit (text, `aria-label`, `placeholder`, `title`, tab title), plus the server-message leak (Step 5) and PrimeNG's labels (Step 4).
  - US-48 "Given Hebrew is the selected language, When I view any screen (week grid, ranked picks, navigation, dialogs)" → Step 6.4 (grid), Step 7.3 (ranked picks), Step 6 shell on every screen (navigation), Step 6.2 / 6.5 dialogs and overlays.
  - "layout direction, alignment, icons, and ordering are mirror-correct" → toasts, icons, date picker, popover (tasks 5–6), alignment (task 7), Sunday-first ordering (Step 6.4).
  - "while numbers and times remain readable" → `07:00–12:00` (Step 6.4), the digit-leading email (Step 6.2), IDs (Step 6.3), day-first dates.
  - Requirements §8.4 and decision 14 → the whole slice. §8.3 → 24-hour Jerusalem instants (Step 6.3 / 6.5).
- **Placeholder scan:** every code step has full contents or an exact, anchored edit. The only conditional instructions are named fallbacks: the audit's stop-and-fix rule, the zero-size overlay note, and Step 11's approval gate.
- **Type consistency:** names used here match the tasks that define them: `LanguageService.locale` / `isRtl`, `AppLocale`, `PRIMENG_HE` / `PRIMENG_EN` (`core/primeng-translations.ts`), `ProblemDetails.code`, `rtlPopoverPlacement`, `--app-caps-tracking`, the keys `errors.*`, `shell.language`, `publications.history.windowRange`, `publications.dataAsOf`. The UI strings quoted are the current `en.json` / `he.json` values or the values tasks 2–4 add.
