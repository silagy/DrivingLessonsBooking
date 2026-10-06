# Task 6 of 6: Verify in the app and open the PR

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires tasks 1 to 5 committed. Work on branch `93-students-screen`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 5 (`GET api/students/{id}`, `POST api/students`, `carTransmission` on find-students, `StudentNationalIdAlreadyInUseException` with `params.name`, `StudentsStore`, `StudentsPage`, `AddStudentDialog`, `pickCarFor`, `toNewStudent`, routes `/students`, `/students/import`, `/roster` redirect, nav key `shell.nav.students`, the `students.*`, `roster.back` and two `errors.*` keys).
- Produces: the PR that closes #93.

**Why:** the AC "All strings are translation keys, Hebrew first, RTL-correct" and the design comparison need the running app. This task walks every frame of design sections 1 (variant 1a), 2 (2a, 2b, 2g to 2n) and 3 (3a to 3i), in Hebrew (RTL) first, then English, then at 768px, and covers Review Focus 1, 3, 4 and 5 end to end (2 is covered by the task 2 smoke).

- [ ] **Step 1: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every suite passes; no pending model changes (this story adds no migration).

- [ ] **Step 2: Start the app on a throwaway database**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us93_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us93_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`); it migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us93_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed only the Teachers and Cars for now (Students come in step 4, after the empty state is checked). The design's fictional data: Teachers "רונית אברהם" (two Cars), "יעל כרמי" (two Cars, one shared with רונית), "אורן ביטון" (one Car) and "מיכל לוי" (no Cars). Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value lives inside a script file and payloads go by relative `@file` paths:

```bash
mkdir -p "<scratchpad>/verify-93" && cd "<scratchpad>/verify-93"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
auth() { echo "Authorization: Bearer $ADMIN"; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

cat > fleet.js <<'EOF'
const fs = require('fs');
const write = (file, body) => fs.writeFileSync(file, JSON.stringify(body));
write('teacher-ronit.json', { name: 'רונית אברהם', contactEmail: 'ronit@school.example' });
write('teacher-yael.json', { name: 'יעל כרמי', contactEmail: 'yael@school.example' });
write('teacher-oren.json', { name: 'אורן ביטון', contactEmail: 'oren@school.example' });
write('teacher-michal.json', { name: 'מיכל לוי', contactEmail: 'michal@school.example' });
write('car-corolla.json', { name: 'קורולה לבנה', type: 'קורולה', transmission: 'automatic' });
write('car-i20.json', { name: 'i20 כסופה', type: 'i20', transmission: 'manual' });
write('car-picanto.json', { name: 'פיקנטו אדומה', type: 'פיקנטו', transmission: 'automatic' });
write('car-mazda.json', { name: 'מאזדה 3 אפורה', type: 'מאזדה', transmission: 'manual' });
EOF
node fleet.js

create() { curl -s -X POST "$API/api/$1" -H "$(auth)" -H "Content-Type: application/json" -d @"$2" | json id; }
RONIT=$(create teachers teacher-ronit.json); YAEL=$(create teachers teacher-yael.json)
OREN=$(create teachers teacher-oren.json); MICHAL=$(create teachers teacher-michal.json)
COROLLA=$(create cars car-corolla.json); I20=$(create cars car-i20.json)
PICANTO=$(create cars car-picanto.json); MAZDA=$(create cars car-mazda.json)

for PAIR in "$COROLLA:$RONIT" "$I20:$RONIT" "$I20:$YAEL" "$PICANTO:$YAEL" "$MAZDA:$OREN"; do
  curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/${PAIR%%:*}/teachers/${PAIR##*:}" -H "$(auth)"
done

printf 'RONIT=%s\nYAEL=%s\nOREN=%s\nMICHAL=%s\nCOROLLA=%s\nI20=%s\nPICANTO=%s\nMAZDA=%s\n' \
  "$RONIT" "$YAEL" "$OREN" "$MICHAL" "$COROLLA" "$I20" "$PICANTO" "$MAZDA" | tee ids.txt
```

Expected: five `assign 204` (or `201`) lines and `ids.txt` with no empty value.

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200` and sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew).

Screenshots of this app can hang or come back half-rendered (memory note). Before each screenshot, run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. If a screenshot still hangs, verify through `read_page`, `get_page_text`, `read_network_requests` and `getComputedStyle`; no check below depends on a screenshot alone.

- [ ] **Step 3: Navigation, Roster and the empty state (frames 1a, 2k)**

- [ ] The top navigation reads "לוח בקרה · רכבים ומורים · תלמידים · הכנת שבוע · פרסומים · היסטוריה · משתמשים". Click "תלמידים": the URL is `/students`, the item is highlighted, `GET /api/students/find`, `GET /api/teachers/find` and `GET /api/cars/find` → `200`.
- [ ] 2k: no filters row; the card shows the user icon, "עדיין אין תלמידים", "הוסיפו תלמיד ידנית, או ייבאו את רשימת התלמידים מקובץ." and two buttons "ייבוא רשימת תלמידים" and "הוספת תלמיד".
- [ ] Click the header's "ייבוא רשימת תלמידים": the URL is `/students/import`, "תלמידים" stays highlighted, the back link "חזרה לתלמידים" sits above the title "ייבוא רשימת תלמידים" with the subtitle "קובץ CSV מוסיף תלמידים חדשים ומעדכן תלמידים קיימים לפי תעודת זהות. הוא אף פעם לא משבית תלמיד.", and the upload button still works (`roster.uploadCsv`). In RTL the back chevron points right (toward the inline start). Click the back link: `/students`.
- [ ] Type `http://localhost:4200/roster` in the address bar: it lands on `/students/import`.

```javascript
await document.fonts.ready;
const active = document.querySelector('.shell__nav-link--active');
({
  path: location.pathname,
  activeNav: active?.textContent.trim(),
  back: document.querySelector('.roster__back')?.textContent.trim(),
  backIconFlip: getComputedStyle(document.querySelector('.roster__back-icon')).transform,
  dir: document.documentElement.dir,
  rawKeys: document.body.innerText.match(/\b(students|roster|shell|errors)\.[A-Za-z.]+/g),
})
```

Expected on `/students/import`: `activeNav` "תלמידים", `back` "חזרה לתלמידים", `backIconFlip` a `matrix(-1, ...)`, `dir` "rtl", `rawKeys` null.

- [ ] **Step 4: Seed the Students**

Eight fictional Students from the design (Roi sits on Oren's own Car: the Car-not-of-Teacher row is #95, and the API refuses it now). The script computes each national ID's check digit, so every ID is valid:

```bash
cd "<scratchpad>/verify-93" && API=http://localhost:5080 && . ./ids.txt
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

cat > students.js <<'EOF'
const fs = require('fs');
const [ronit, yael, oren, corolla, i20, picanto, mazda] = process.argv.slice(2);
const checkDigit = (body) => {
  let sum = 0;
  for (let i = 0; i < 8; i++) {
    const product = Number(body[i]) * (i % 2 === 0 ? 1 : 2);
    sum += product > 9 ? product - 9 : product;
  }
  return String((10 - (sum % 10)) % 10);
};
const id = (body) => body + checkDigit(body);
const students = [
  ['noa', 'נועה מזרחי', '20537418', '050-1234567', ronit, corolla],
  ['omer', 'עומר שלו', '31245678', '052-7654321', ronit, i20],
  ['tamar', 'תמר שגיא', '31820457', '054-3321908', yael, picanto],
  ['itai', 'איתי פרץ', '20781543', '050-6612034', yael, i20],
  ['lia', 'ליה חדד', '31654029', '053-4401277', oren, mazda],
  ['roi', 'רועי אלמוג', '20933176', '058-2290415', oren, mazda],
  ['dana', 'דנה ששון', '31107854', '052-9038816', ronit, i20],
  ['yoni', 'יונתן קדם', '20466712', '054-7710352', yael, picanto],
];
const ids = [];
for (const [key, name, body, phone, teacherId, carId] of students) {
  fs.writeFileSync(`student-${key}.json`, JSON.stringify({ nationalId: id(body), name, phone, teacherId, carId, address: null, startDate: null, licenseType: null }));
  ids.push(`${key.toUpperCase()}_ID=${id(body)}`);
}
ids.push(`SHAKED_ID=${id('21450398')}`);
fs.writeFileSync('national-ids.txt', ids.join('\n') + '\n');
EOF
node students.js "$RONIT" "$YAEL" "$OREN" "$COROLLA" "$I20" "$PICANTO" "$MAZDA"

for KEY in noa omer tamar itai lia roi dana yoni; do
  curl -s -o /dev/null -w "$KEY %{http_code}\n" -X POST $API/api/students -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @student-$KEY.json
done
. ./national-ids.txt
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us93_verify -c "UPDATE students SET is_active = false WHERE national_id IN ('$DANA_ID', '$YONI_ID')"
cat national-ids.txt
```

Expected: eight `201` lines, `UPDATE 2`, and the national IDs printed (keep `NOA_ID` and `SHAKED_ID` for step 7). Reload the Students page.

- [ ] **Step 5: The list in Hebrew (frames 2a, 2b, 2g, 2h, 2l)**

```javascript
await document.fonts.ready;
const rows = [...document.querySelectorAll('.p-datatable-tbody > tr')];
({
  dir: document.documentElement.dir,
  title: document.querySelector('.students__title')?.textContent.trim(),
  headers: [...document.querySelectorAll('.p-datatable-thead th')].filter(th => getComputedStyle(th).display !== 'none').map(th => th.innerText.trim()),
  rows: rows.map(r => r.innerText.replace(/\s+/g, ' ').trim()),
  inactiveRows: rows.filter(r => r.classList.contains('students__row--inactive')).length,
  tags: [...document.querySelectorAll('app-transmission-tag')].map(t => t.innerText.trim()),
  nidDirection: getComputedStyle(document.querySelector('td bdi.students__ltr')).direction,
  footer: document.querySelector('.students__footer')?.textContent.trim(),
  status: document.querySelector('.students__status .p-togglebutton-checked')?.innerText.trim(),
  rawKeys: document.body.innerText.match(/\b(students|roster|shell|errors)\.[A-Za-z.]+/g),
  horizontalScroll: document.documentElement.scrollWidth > innerWidth,
})
```

Expected:
- [ ] 2a: `dir` "rtl", `title` "תלמידים"; `headers` "שם", "תעודת זהות", "טלפון", "מורה", "רכב", "סטטוס"; six rows (Noa, Omer, Tamar, Itai, Lia, Roi), Teacher by name then Student by name; `inactiveRows` 0; every row's status "פעיל"; Car cells read like "קורולה לבנה אוטומט" / "i20 כסופה ידני"; `nidDirection` "ltr" (the digits and the phone's dash never reorder); `footer` "6 תלמידים"; `status` "פעילים"; `rawKeys` null; no horizontal scroll.
- [ ] Filters row: Teacher select reads "כל המורים" at the inline start (right), the pill status control in the middle, the search field at the inline end (left) with its icon at the field's right edge (inline start).
- [ ] 2b: click "הכול": eight rows, Dana and Yoni last, muted (grey text, grey avatar, page-background tint, grey transmission tag) with "לא פעיל"; `footer` "8 תלמידים". "לא פעילים": only those two.
- [ ] 2g: open the Teacher filter: "כל המורים" first, then "אורן ביטון", "יעל כרמי", "מיכל לוי", "רונית אברהם" (Michal listed though she has no Cars). Pick "יעל כרמי" with "הכול": Tamar, Itai, then Yoni.
- [ ] 2h: back to "כל המורים" / "פעילים"; type the first four digits of `NOA_ID` in the search: only Noa. Type part of a Hebrew name, e.g. "שגיא": only Tamar. Clear the search.
- [ ] 2l: pick "מיכל לוי" and "הכול": the table header stays, then the search icon, "אין תלמידים שמתאימים לסינון", "נסו מורה אחר או מצב אחר." and "ניקוי הסינון"; no footer. Click it: back to "כל המורים", "פעילים", empty search, six rows.

- [ ] **Step 6: Loading and error (frames 2i, 2j)**

Stop the API (`TaskStop`) and reload the Students page: after the request fails, the card shows the red alert icon, "לא הצלחנו לטעון את התלמידים." and "ניסיון חוזר"; the filters stay. Start the API again (same command as step 2) and click "ניסיון חוזר": the six rows return. 2i (the spinner with "טוען תלמידים...") flashes by on a local API; check it with `read_network_requests` throttling if the pane supports it, otherwise rely on the store spec (`isLoading`) and note it in the PR.

- [ ] **Step 7: Add Student in Hebrew (frames 3a to 3h; Review Focus 1, 3, 4, 5)**

Click "הוספת תלמיד".
- [ ] 3a: header "הוספת תלמיד"; fields in the order תעודת זהות (hint "9 ספרות, כולל ספרת ביקורת", typed digits stay LTR), שם מלא, טלפון, מורה ("בחירת מורה"), רכב disabled with "יש לבחור מורה קודם", the "אופציונלי" sub-heading, כתובת, then תאריך התחלה and סוג רישיון side by side ("dd/mm/yyyy", "לדוגמה B"). "שמירה" disabled.
- [ ] Open the Teacher list: "מיכל לוי" shows the muted "אין רכבים" at the option's inline end (left).
- [ ] 3b: pick "יעל כרמי": the Car enables, empty, with "בחירת רכב"; its list is exactly "i20 כסופה ידני" and "פיקנטו אדומה אוטומט"; hint "מוצגים רק הרכבים של יעל כרמי.". Pick "i20 כסופה".
- [ ] Review Focus 4: switch the Teacher to "רונית אברהם" (who shares the i20): the Car empties anyway and lists "i20 כסופה" and "קורולה לבנה".
- [ ] 3c: pick "אורן ביטון": "מאזדה 3 אפורה ידני" is selected and the hint reads "זה הרכב היחיד של אורן ביטון, ולכן הוא נבחר.".
- [ ] 3d: pick "מיכל לוי": the Car is disabled with "בחירת רכב", the info message "למורה הזה עדיין אין רכבים. יש לשייך רכב במסך רכבים ומורים." with "מעבר לרכבים ומורים" sits under it, "שמירה" stays disabled even with every other field filled. Click the link: the dialog closes and `/teachers` opens. Go back to "תלמידים" and open the dialog again.
- [ ] 3e (Review Focus 1): national ID = `NOA_ID`, name "בדיקה כפולה", phone "050-0000000", Teacher "יעל כרמי", Car "פיקנטו אדומה", Save: `POST /api/students` → `409`; the error message at the top reads "הפעולה לא בוצעה" and "כבר קיים תלמיד עם תעודת הזהות הזו: נועה מזרחי."; the national ID field turns red with the same text under it; "שמירה" is disabled until the ID is edited. Now try `DANA_ID` (an Inactive Student): the same refusal naming "דנה ששון", never a 500 or a generic message.
- [ ] 3f: national ID `123456789`, Save: "תעודת הזהות לא תקינה. יש לבדוק שיש 9 ספרות ושספרת הביקורת נכונה." at the top and under the field. Then `1234567890` (ten digits): the same message (`nationalIdMustBeAtMostNineDigits` maps to it too).
- [ ] 3g: national ID = `SHAKED_ID`, name "שקד נבון", phone "050-3318842", Teacher "יעל כרמי", Car "פיקנטו אדומה". Before saving, unassign Picanto from Yael from the Bash tool:

```bash
cd "<scratchpad>/verify-93" && API=http://localhost:5080 && . ./ids.txt
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
curl -s -o /dev/null -w "unassign %{http_code}\n" -X DELETE "$API/api/cars/$PICANTO/teachers/$YAEL" -H "Authorization: Bearer $ADMIN"
```

Save: `409`, the top message reads "הרכב הזה לא משויך למורה הזה. יש לרענן ולבחור שוב." with a "רענון" link, and the Car field turns red. Click "רענון": `GET /api/cars/find` → `200`, the message goes away, Yael's Car list is now only "i20 כסופה" and, as her only Car, it is preselected with the "only Car" hint. Re-assign Picanto (`curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/$PICANTO/teachers/$YAEL" -H "Authorization: Bearer $ADMIN"` → `204`), then cancel the dialog.
- [ ] 3h (Review Focus 3 and 5): national ID = `SHAKED_ID`, name "שקד נבון", phone "050-3318842", Teacher "יעל כרמי", Car "פיקנטו אדומה", address "   " (spaces only), start date 1 September 2026 from the calendar, license type "B". Save: the request body (`read_network_requests`) has `"address": null`, `"startDate": "2026-09-01"`, `"licenseType": "B"`; `201`; the dialog closes; the toast reads "התלמיד נוסף" / "שקד נבון נוסף/ה לרשימה של יעל כרמי."; Shaked is the first row, tinted, with the "חדש" tag; the footer reads "7 תלמידים". `GET /api/students/{id}` for the new id (from the `201` body) returns `startDate` "2026-09-01".
- [ ] Reload the page: Shaked loses the "חדש" tag and takes its place under Yael (README decision 12).

- [ ] **Step 8: English (LTR) (frames 2m, 3i)**

Switch the language to English:
- [ ] `dir` "ltr"; title "Students" and the subtitle "Students are the people who submit weekly requests. This list includes Students from the Roster and Students added by hand."; buttons "Import Roster" and "Add Student"; filters "Teacher" ("All Teachers"), "Status" ("Active" / "Inactive" / "All"), "Search" ("Name or national ID"); headers "Name", "National ID", "Phone", "Teacher", "Car", "Status"; tags "Automatic" / "Manual", "Active" / "Inactive"; footer "{n} Students"; the search icon now at the field's left edge.
- [ ] Open "Add Student" (3i): every label, placeholder and hint in English ("Choose a Teacher first", "Only Yael's Cars are listed." with the Hebrew name, "Optional", "dd/mm/yyyy", "e.g. B"); Teacher "יעל כרמי" with Car "פיקנטו אדומה" shows "Automatic". Cancel.
- [ ] The Roster page reads "Back to Students", "Import Roster" and the English subtitle; the back chevron points left.
- [ ] No raw translation key on any of the three pages.

Switch back to Hebrew.

- [ ] **Step 9: 768px (frame 2n)**

`resize_window` with width 768 and height 1024, reload `/students`, choose "הכול":

```javascript
await document.fonts.ready;
({
  headers: [...document.querySelectorAll('.p-datatable-thead th')].filter(th => getComputedStyle(th).display !== 'none').map(th => th.innerText.trim()),
  firstRow: document.querySelector('.p-datatable-tbody > tr')?.innerText.replace(/\s+/g, ' ').trim(),
  filterColumns: getComputedStyle(document.querySelector('.students__filters')).gridTemplateColumns.split(' ').length,
  horizontalScroll: document.documentElement.scrollWidth > innerWidth,
})
```

Expected: `headers` "תלמיד", "מורה ורכב", "סטטוס"; the first row shows the name with its national ID and phone below it, then the Teacher with the Car and its tag below it, then the status; `filterColumns` 2 (Teacher and status side by side, search on its own row); no horizontal scroll. Reset with preset `desktop`.

- [ ] **Step 10: Compare against the design**

If the `claude_design` MCP is connected, read `students/app.jsx`, `students/kit.jsx`, `students/dialogs.jsx` and `students/more.jsx` from project `6a0ab892-caa4-49f7-baff-bba7ca38c862` with `read_file`, render frames 1a, 2a, 2b, 2g to 2n and 3a to 3i with `render_preview`, and compare them with steps 3 to 9. Check the copy word for word against `students/data.jsx` (`ST_GROUPS`), the transmission tag tones, the inactive and new row tints, and the 768px columns. Write down every visible difference for the PR. Expected differences (README decisions 15, 20 and 21):
- No row-actions column (kebab menu) and no Car-not-of-Teacher marker: #94 / #95.
- English buttons in Title Case ("Add Student"), not the copy deck's capitals; "Loading Students..." with three dots.
- The new-row tint is the theme's `--p-sky-50` (#EAF1FF), a shade darker than the design's #F2F7FF.
- The footer uses "תלמיד אחד" / "1 Student" for one row.

Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 11: Push and open the PR**

```bash
git push -u origin 93-students-screen
```

Write `pr-body.md` in the scratchpad:

```markdown
Closes #93. Part of #82, slice (5) "Students screen". Unblocks #94 and #95.

## What changed

- **Backend:** `GET api/students/{id}` (`GetStudentInteractor`, 404 `studentNotFound`); `POST api/students` (`CreateStudentInteractor`, 201 with the new id). A national ID already used by any Student, active or Inactive, is refused with `StudentNationalIdAlreadyInUseException` (409, `params.name` = the existing Student), checked through the new `IStudentRepository.GetByNationalIdAsync`. A Car not of the Teacher is refused by the `Student` aggregate (#92 invariant). Find-students now carries the Car's transmission. All three Student endpoints are Administrator-only and pinned in `ControllerAuthorizationTest`.
- **Client:** new lazy `students` feature (domain · data · state · ui) with a signals-only store. The "תלמידים" nav item opens the Students screen: Teacher / status / search filters (client-side), Inactive Students muted and last, transmission tags, loading / error / empty / no-match states, a 768px layout. "הוספת תלמיד" opens a dialog whose Car list is the chosen Teacher's Cars only, resets on every Teacher change, preselects an only Car and explains a Teacher with no Cars; 409s show inline (national ID in use with the name, invalid national ID, stale Car with Refresh). The Roster upload moved to `/students/import` behind "ייבוא רשימת תלמידים", with a back link; `/roster` redirects.
- **Translations:** `students.*`, `roster.back`, new Roster title and subtitle, `errors.studentNationalIdAlreadyInUse` and the missing `errors.studentCarMustBeAssignedToTeacher`, Hebrew first.

## Not in this PR (by design)

- Row actions, Edit details, Deactivate / Reactivate and the student-form Inactive notice: #94.
- Change Teacher, Change Car and the Car-not-of-Teacher flag in the list: #95.

## Verification

- Backend and client suites pass; no pending model changes.
- API smoke (task 2): <fill in>.
- Browser, Hebrew (RTL), English and 768px: <fill in from steps 3-9>.
- Design differences: <fill in from step 10>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the `<fill in ...>` lines with what the smoke and steps 3 to 10 actually showed, then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 93-students-screen --title "Students screen: list, filter and add a Student by hand (#93)" --body-file "<scratchpad>/pr-body.md"
```

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails.

- [ ] **Step 12: Drop the throwaway database**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us93_verify"
```

Expected: `DROP DATABASE`. Leave the compose Postgres running and `dl-postgres` stopped unless the user asks otherwise.
