# Task 7 of 7: Verify in the app and open the PR

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires tasks 1 to 6 committed. Work on branch `94-edit-deactivate-reactivate-students`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 6 (`PUT api/students/{id}/details`, `POST api/students/{id}/deactivate|reactivate`, the identify 409 `submissionStudentMustBeActive`, `StudentsStore` commands, the row menu, `DeactivateStudentDialog`, `EditStudentDialog`, `IdentifyStatus.inactive`, the new `students.*`, `studentForm.identify.inactive*` and `errors.studentAlready*` texts).
- Produces: the PR that closes #94.

**Why:** the AC "all strings are translation keys, Hebrew first, RTL-correct" and the design comparison need the running app. This task walks design frames 2e, 2f, 4a to 4c, 5a to 5e and 8b in Hebrew (RTL) first, then English, then at 768px (the Students screen) and 375px (the student form), and covers Review Focus 1 to 5 end to end.

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

- [ ] **Step 2: Start the app on a throwaway database and seed it**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us94_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us94_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`); it migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us94_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed the design's fictional data. Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value lives in `seed.js` and payloads go by relative `@file` paths; the script computes each national ID's check digit:

```bash
mkdir -p "<scratchpad>/verify-94" && cd "<scratchpad>/verify-94"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
auth() { echo "Authorization: Bearer $ADMIN"; }

cat > fleet.js <<'EOF'
const fs = require('fs');
const write = (file, body) => fs.writeFileSync(file, JSON.stringify(body));
write('teacher-ronit.json', { name: 'רונית אברהם', contactEmail: 'ronit@school.example' });
write('teacher-yael.json', { name: 'יעל כרמי', contactEmail: 'yael@school.example' });
write('car-corolla.json', { name: 'קורולה לבנה', type: 'קורולה', transmission: 'automatic' });
write('car-i20.json', { name: 'i20 כסופה', type: 'i20', transmission: 'manual' });
EOF
node fleet.js

create() { curl -s -X POST "$API/api/$1" -H "$(auth)" -H "Content-Type: application/json" -d @"$2" | json id; }
RONIT=$(create teachers teacher-ronit.json); YAEL=$(create teachers teacher-yael.json)
COROLLA=$(create cars car-corolla.json); I20=$(create cars car-i20.json)
for PAIR in "$COROLLA:$RONIT" "$I20:$RONIT" "$I20:$YAEL"; do
  curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/${PAIR%%:*}/teachers/${PAIR##*:}" -H "$(auth)"
done

cat > students.js <<'EOF'
const fs = require('fs');
const [ronit, yael, corolla, i20] = process.argv.slice(2);
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
  ['noa', 'נועה מזרחי', '20537418', '050-1234567', ronit, corolla, 'הרימון 12, מודיעין', '2026-09-01', 'B'],
  ['omer', 'עומר שלו', '31245678', '052-7654321', ronit, i20, null, null, null],
  ['itai', 'איתי פרץ', '20781543', '050-6612034', yael, i20, null, null, null],
  ['dana', 'דנה ששון', '31107854', '052-9038816', ronit, i20, null, null, null],
];
const lines = [];
for (const [key, name, body, phone, teacherId, carId, address, startDate, licenseType] of students) {
  fs.writeFileSync(`student-${key}.json`, JSON.stringify({ nationalId: id(body), name, phone, teacherId, carId, address, startDate, licenseType }));
  lines.push(`${key.toUpperCase()}_ID=${id(body)}`);
}
fs.writeFileSync('national-ids.txt', lines.join('\n') + '\n');
EOF
node students.js "$RONIT" "$YAEL" "$COROLLA" "$I20"
for KEY in noa omer itai dana; do
  ID=$(create students student-$KEY.json); echo "${KEY^^}=$ID" >> student-ids.txt; echo "$KEY $ID"
done
. ./student-ids.txt; . ./national-ids.txt
curl -s -o /dev/null -w "deactivate dana %{http_code}\n" -X POST "$API/api/students/$DANA/deactivate" -H "$(auth)"
```

For the student form (step 7) open a week for Ronit and publish it with a window open now. The publication is created with the first week schedule (`2026-10-11` is a Sunday after today):

```bash
WEEK=2026-10-11
printf '{"teacherId":"%s","weekStart":"%s"}' "$RONIT" "$WEEK" > week.json
curl -s -o /dev/null -w "week %{http_code}\n" -X POST $API/api/week-schedules -H "$(auth)" -H "Content-Type: application/json" -d @week.json
curl -s "$API/api/publications/by-week?week=$WEEK" -H "$(auth)" > publication.json
PUB_ID=$(json id < publication.json); LINK=$(json linkToken < publication.json)
printf '{"startUtc":"%s","endUtc":"%s"}' "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)" > window.json
curl -s -o /dev/null -w "publish %{http_code}\n" -X POST "$API/api/publications/$PUB_ID/publish" -H "$(auth)" -H "Content-Type: application/json" -d @window.json
sleep 5
echo "state $(curl -s $API/api/submissions/by-link/$LINK | json state)"
printf 'LINK=%s\n' "$LINK" >> student-ids.txt
```

Expected: three `assign 204` (or `201`), four Student ids, `deactivate dana 204`, `week 201`, `publish 204`, `state open` (wait a few more seconds if it still says `published`). If a route differs, read the controller and adapt the script.

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200`, sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew) and open "תלמידים".

Screenshots of this app can hang or come back half-rendered (memory note). Before each screenshot run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s; if one still hangs, verify through `read_page`, `get_page_text`, `read_network_requests` and `getComputedStyle`. No check below depends on a screenshot alone.

- [ ] **Step 3: Row actions in Hebrew (frames 2e, 2f)**

- [ ] Every row ends with a round kebab button at the inline end (left in RTL); the actions header cell is empty to the eye and reads "פעולות" to a screen reader. `read_page` shows each kebab's accessible name as "פעולות - {name}".
- [ ] Open Noa's menu (2e): "עריכת פרטים" (pencil), a separator, "סימון כלא פעיל" (pause icon) in red. No "החלפת מורה" / "החלפת רכב" (#95).
- [ ] Choose "הכול", open Dana's menu (2f): "עריכת פרטים" and "סימון כפעיל" (replay icon), no separator, nothing red.

```javascript
await document.fonts.ready;
const items = [...document.querySelectorAll('.p-menu .p-menu-item')].map(item => ({
  text: item.innerText.trim(),
  color: getComputedStyle(item.querySelector('.p-menu-item-label')).color,
}));
({ items, separators: document.querySelectorAll('.p-menu .p-menu-separator').length, rawKeys: document.body.innerText.match(/\b(students|errors)\.[A-Za-z.]+/g) })
```

Expected for Noa's menu: two items, the second in `--p-red-600` (rgb(220, 38, 38) or the theme's red), `separators` 1, `rawKeys` null.

- [ ] **Step 4: Deactivate (frames 5a, 5b)**

Back to "פעילים". Open Itai's menu, "סימון כלא פעיל".
- [ ] 5a: header "לסמן את איתי פרץ כלא פעיל/ה?"; the who card: avatar initials, "איתי פרץ", his national ID LTR below; at the inline end "מורה: יעל כרמי" and "רכב: i20 כסופה" with the "ידני" tag; three lines with icons: "עד לסימון מחדש כפעיל/ה, לא תהיה אפשרות להגיש בטופס התלמידים.", "ההגשות הקודמות נשמרות.", "קובץ רשימת תלמידים שעדיין כולל אותם יסמן אותם שוב כפעילים." (the replay icon mirrored in RTL); footer "ביטול" (text) and "סימון כלא פעיל" as a red **outlined** button.
- [ ] Confirm: `POST /api/students/{id}/deactivate` → `204`; the dialog closes; toast "התלמיד סומן כלא פעיל" / "איתי פרץ לא יוכל/תוכל להגיש עד לסימון מחדש כפעיל/ה."; Itai leaves the Active list (5b), the footer drops by one.
- [ ] "הכול": Itai is now muted, last, "לא פעיל".

- [ ] **Step 5: Edit details (frames 4a to 4c; Review Focus 1, 2, 3)**

Choose "הכול" if not already. Open Noa's menu, "עריכת פרטים": `GET /api/students/{id}` → `200`, then the dialog.
- [ ] 4a: header "עריכת פרטים", "נועה מזרחי" under it; fields: תעודת זהות (LTR, her ID) with the info-icon note "השינוי כאן לא משנה את קובץ רשימת התלמידים. ..."; שם מלא; טלפון (LTR "050-1234567"); כתובת "הרימון 12, מודיעין"; תאריך התחלה **01/09/2026** and סוג רישיון "B" side by side (Review Focus 3: the saved day, not 31/08); the locked "מורה ורכב" field: lock icon, "רונית אברהם", "· קורולה לבנה" with the "אוטומט" tag. "שמירה" enabled.
- [ ] Review Focus 1: Save without changes → `PUT /api/students/{id}/details` → `204`, toast "הפרטים נשמרו", dialog closed. Reopen; change the phone to "050-1234568"; Save → `204`; the row's phone updates after the reload.
- [ ] 4b: reopen Noa; national ID = `OMER_ID` (from `national-ids.txt`); Save → `409`; the error Message first in the dialog: "הפעולה לא בוצעה" / "תעודת הזהות הזו שייכת לתלמיד אחר: עומר שלו."; the field red with the same line under it; "שמירה" disabled. Type in the national ID: the Message and the red line go away, Save comes back.
- [ ] 4c: national ID `205374185`; Save → `409`: "תעודת הזהות לא תקינה. יש לבדוק שיש 9 ספרות ושספרת הביקורת נכונה." at the top and under the field. Restore `NOA_ID`.
- [ ] Review Focus 2: set the address to "   " (spaces only), clear the start date and the license type; Save: the request body (`read_network_requests`) has `"address": null`, `"startDate": null`, `"licenseType": null`; `204`. Reopen: all three are blank. Set the start date back to 1 September 2026 from the calendar and save: the body has `"startDate": "2026-09-01"`; reopen: 01/09/2026.
- [ ] Open Dana's (Inactive) "עריכת פרטים", change the phone, save: `204`, she stays Inactive.

- [ ] **Step 6: Reactivate and stale toggles (frames 5c to 5e; Review Focus 5)**

- [ ] 5c: Dana's menu → "סימון כפעיל": `POST /api/students/{id}/reactivate` → `204`; toast "התלמיד סומן כפעיל" / "דנה ששון יכול/ה להגיש שוב בטופס התלמידים."; her row turns "פעיל", unmuted, and sorts with the active rows.
- [ ] 5d: with Omer listed as active, deactivate him from the Bash tool (another Administrator got there first):

```bash
cd "<scratchpad>/verify-94" && API=http://localhost:5080 && . ./student-ids.txt
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
curl -s -o /dev/null -w "deactivate omer %{http_code}\n" -X POST "$API/api/students/$OMER/deactivate" -H "Authorization: Bearer $ADMIN"
```

  In the page (not reloaded), Omer's menu still offers "סימון כלא פעיל". Confirm in the dialog: `409`; the dialog **closes**; error toast "התלמיד הזה כבר לא פעיל. הרשימה רועננה."; `GET /api/students/find` runs again and Omer shows as "לא פעיל" under "הכול".
- [ ] 5e: reactivate Omer from the Bash tool (`curl ... -X POST "$API/api/students/$OMER/reactivate"` → `204`). In the page, Omer's row still shows "לא פעיל"; his menu → "סימון כפעיל": `409`, error toast "התלמיד הזה כבר פעיל. הרשימה רועננה.", the list reloads and Omer is "פעיל".
- [ ] While any command runs, every kebab is disabled (`store.isMutating`).

- [ ] **Step 7: Student form, Inactive Student (frame 8b; Review Focus 4)**

API check first (same shell as step 6):

```bash
. ./national-ids.txt
curl -s -o /dev/null -w "deactivate dana %{http_code}\n" -X POST "$API/api/students/$DANA/deactivate" -H "Authorization: Bearer $ADMIN"
identify() { printf '{"nationalId":"%s"}' "$1" > identify.json; curl -s -o identify-body.json -w "%{http_code}" -X POST "$API/api/submissions/by-link/$LINK/identify" -H "Content-Type: application/json" -d @identify.json; }
echo "noa $(identify $NOA_ID)"
echo "dana $(identify $DANA_ID) $(json code < identify-body.json)"
echo "unknown $(identify 000000059) $(json code < identify-body.json)"
```

Expected: `deactivate dana 204`, `noa 200`, `dana 409 submissionStudentMustBeActive`, `unknown 404 studentNotFound`.

In the browser: `resize_window` preset `mobile` (375px), open `http://localhost:4200/s/<LINK>` (the `LINK` from `student-ids.txt`), Hebrew.
- [ ] Type `DANA_ID`: after the lookup, the field is **not** red; under it the calm notice: pause icon, "ההרשמה שלכם לא פעילה." (title, own line) and "לכן אי אפשר להגיש בקשות השבוע. כדי לחזור לשיעורים, פנו לבית הספר."; "המשך" disabled with "אפשר להמשיך רק עם הרשמה פעילה" centred under it. No name is shown.

```javascript
await document.fonts.ready;
const notice = document.querySelector('.identify__inactive .p-message');
({
  notice: notice?.innerText.replace(/\s+/g, ' ').trim(),
  background: notice && getComputedStyle(notice).backgroundColor,
  border: notice && getComputedStyle(notice).borderColor,
  titleColor: getComputedStyle(document.querySelector('.identify__inactive-title')).color,
  locked: document.querySelector('.identify__locked')?.textContent.trim(),
  continueDisabled: document.querySelector('app-identify-step button[type="submit"]').disabled,
  fieldInvalid: document.querySelector('#national-id').classList.contains('ng-invalid') || document.querySelector('#national-id').classList.contains('p-invalid'),
  horizontalScroll: document.documentElement.scrollWidth > innerWidth,
})
```

  Expected: the notice text as above; `background` the `--app-bg-muted` colour, `border` the `--app-steel-light` colour, `titleColor` the `--app-whale` colour (compare with `getComputedStyle(document.documentElement).getPropertyValue(...)`; if the Message ignores the `--p-message-secondary-*` variables, fix the variable names in `identify-step.component.scss` with a fix commit); `continueDisabled` true; `fieldInvalid` false; no horizontal scroll.
- [ ] Edit the ID to `000000059`: the existing "אינכם מופיעים אצלנו." notice (8a), the field red, no "registration" notice.
- [ ] Edit the ID to `NOA_ID`: "מצאנו אתכם - נועה מזרחי." and Continue enabled.

Reset with preset `desktop`.

- [ ] **Step 8: English (LTR)**

Switch the language to English (the admin shell's language switch; the student form has its own):
- [ ] Students screen: `dir` "ltr"; kebab at the right end; menus "Edit details" / "Deactivate", and "Edit details" / "Reactivate"; the Deactivate dialog "Deactivate Itai Peretz?" (or any active Student), "Teacher:" / "Car:", the three lines ("Until reactivated, they can't submit on the student form.", "Their past Submissions stay.", "A Roster file that still lists them will reactivate them."), buttons "Cancel" / "Deactivate". Cancel.
- [ ] Edit details: "Edit details", the note "Changing it here doesn't change the Roster file. ...", "Teacher and Car"; a refused ID reads "This national ID belongs to another Student: עומר שלו." (the Hebrew name isolated, the sentence LTR). Cancel.
- [ ] Toasts in English: "Student deactivated" / "{name} can't submit until reactivated.", "Student reactivated" / "{name} can submit on the student form again.", "Details saved".
- [ ] Student form (`/s/<LINK>`, English): Dana's ID shows "Your registration isn't active." / "So you can't submit requests this week. To get back to lessons, contact the school." and "You can continue only with an active registration".
- [ ] No raw translation key anywhere (`document.body.innerText.match(/\b(students|studentForm|errors)\.[A-Za-z.]+/g)` is null on each page).

Switch back to Hebrew.

- [ ] **Step 9: 768px**

`resize_window` width 768, height 1024; reload `/students`, choose "הכול":
- [ ] Columns "תלמיד", "מורה ורכב", "סטטוס" and the actions column with its kebab; the menu opens fully on screen; no horizontal scroll (`document.documentElement.scrollWidth > innerWidth` is false).
- [ ] The Edit and Deactivate dialogs fit the width (the start date / license type pair stays side by side).

Reset with preset `desktop`.

- [ ] **Step 10: Compare against the design**

If the `claude_design` MCP is connected, read `students/app.jsx`, `students/kit.jsx` (`StRowMenu`, `StKebab`, `StWho`), `students/dialogs.jsx` (`StEditDlg`, `StDeactDlg`) and `students/more.jsx` (`StForm`) from project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, render frames 2e, 2f, 4a to 4c, 5a to 5e and 8b with `render_preview`, and compare them with steps 3 to 9. Check the copy word for word against `students/data.jsx` (`ST_GROUPS`, groups "Students screen", "Edit details", "Deactivate & Reactivate", "Student form (mobile)"). Write down every visible difference for the PR. Expected differences (README decisions 9, 10, 16 and #93's 20):
- No "החלפת מורה" / "החלפת רכב" menu items, no "Fix" badge and no `edit.tcHint` line under the locked Teacher and Car: #95.
- The student-form Hebrew is in the form's plural voice ("ההרשמה שלכם", "פנו") instead of the copy deck's singular.
- English buttons in Title Case ("Deactivate"), not the copy deck's capitals.
- The design's toast for 5d / 5e is titled "That didn't go through"; the app's error toasts carry only the rule's sentence as their summary (existing `ToastService.apiError`).

Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 11: Push and open the PR**

Commit the plan first if it isn't committed yet (it should be, before task 1):

```bash
git status --short docs/modules/students/us-94-edit-deactivate-reactivate-plan
git push -u origin 94-edit-deactivate-reactivate-students
```

Write `pr-body.md` in the scratchpad:

```markdown
Closes #94. Part of #82 (Students). Follows #93; #95 (Change Teacher, Change Car, Car-not-of-Teacher flag) builds on this.

## What changed

- **Backend:** `Student.ChangeDetails(...)` with the new `StudentDetailsChanged` event; `PUT api/students/{id}/details` (`ChangeStudentDetailsInteractor`): a national ID that another Student already uses (active or Inactive) is refused with `studentNationalIdAlreadyInUse` (409, `params.name` = that Student), the Student's own ID is never a conflict. `POST api/students/{id}/deactivate` and `/reactivate` on the existing domain methods: 409 `studentAlreadyDeactivated` / `studentAlreadyActive`, 404 `studentNotFound`. New `IStudentRepository.GetAsync(StudentId)`. All Administrator-only, pinned in `ControllerAuthorizationTest`; `ApiExceptionFilterTest` pins the codes.
- **Student form:** an Inactive Student identifying now gets 409 `submissionStudentMustBeActive` (was 404 "not on the roster") and a distinct, calmer notice: "ההרשמה שלכם לא פעילה." with Continue locked. Unknown IDs are unchanged.
- **Client:** Students rows get an actions menu: Edit details + Deactivate (active), Edit details + Reactivate (Inactive). Edit details opens on fresh details, shows 409s inline (ID used by another Student with their name, invalid ID). Deactivate is a confirmation dialog; Reactivate is one click. A toggle someone else already made shows "already inactive / active - the list has been refreshed" and reloads.
- **Translations:** `students.actions.*`, `students.who.*`, `students.deactivate.*`, `students.edit.*`, the three new toasts, `studentForm.identify.inactive*`; `errors.studentAlreadyActive` / `studentAlreadyDeactivated` reworded. Hebrew first.

## For the spec owner

- Hebrew wording "סימון כלא פעיל" / "סימון כפעיל" (the design's choice over השבתה / הפעלה מחדש). Please confirm.
- The student-form Hebrew uses the form's plural voice, not the copy deck's singular.

## Verification

- Backend and client suites pass; no pending model changes.
- API smoke (task 2): <fill in>.
- Browser, Hebrew (RTL), English, 768px, student form at 375px: <fill in from steps 3-9>.
- Design differences: <fill in from step 10>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the `<fill in ...>` lines with what the smoke and steps 3 to 10 actually showed, then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 94-edit-deactivate-reactivate-students --title "Edit, deactivate and reactivate a Student (#94)" --body-file "<scratchpad>/pr-body.md"
```

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails. `Closes #94` won't auto-close the issue on the non-default base: close it when `82-users-and-roles` merges to `main`.

- [ ] **Step 12: Drop the throwaway database**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us94_verify"
```

Expected: `DROP DATABASE`. Leave the compose Postgres running and `dl-postgres` stopped unless the user asks otherwise.
