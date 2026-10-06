# Task 7 of 7: Verify in the app and open the PR

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires tasks 1 to 6 committed. Work on branch `95-change-teacher-and-car`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 6 (`POST api/students/{id}/change-teacher|change-car`, `isCarOfTeacher` on the list and details, `StudentsStore.changeTeacher` / `changeCar`, `ChangeTeacherDialog`, `ChangeCarDialog`, `CarFlagComponent`, the row menu, the new `students.*`, `general.close` and `errors.*` texts).
- Produces: the PR that closes #95.

**Why:** the AC "identifying on the student form resolves the new Teacher's Publication", "all strings are translation keys, Hebrew first, RTL-correct" and the design comparison need the running app on Postgres. This task covers Review Focus 2 and 4 end to end, walks frames 2c, 2d, 2e, 6a to 6h and 7a to 7f in Hebrew (RTL) first, then English, then at 768px.

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
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us95_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us95_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`); it migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us95_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

- [ ] **Step 3: Seed the design's fictional school**

Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value lives in a `.js` file and payloads go by relative `@file` paths; the script computes each national ID's check digit (the design's sample IDs fail it, memory note). Data follows `students/data.jsx`: Ronit (Corolla, i20), Yael (i20, Picanto), Oren (Mazda), Michal (no Cars).

```bash
mkdir -p "<scratchpad>/verify-95" && cd "<scratchpad>/verify-95"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
auth() { echo "Authorization: Bearer $ADMIN"; }
create() { curl -s -X POST "$API/api/$1" -H "$(auth)" -H "Content-Type: application/json" -d @"$2" | json id; }
assign() { curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/$1/teachers/$2" -H "$(auth)"; }

cat > fleet.js <<'EOF'
const fs = require('fs');
const write = (file, body) => fs.writeFileSync(file, JSON.stringify(body));
write('teacher-ronit.json', { name: 'רונית אברהם', contactEmail: 'ronit@school.example' });
write('teacher-yael.json', { name: 'יעל כרמי', contactEmail: 'yael@school.example' });
write('teacher-oren.json', { name: 'אורן לוי', contactEmail: 'oren@school.example' });
write('teacher-michal.json', { name: 'מיכל בן דוד', contactEmail: 'michal@school.example' });
write('car-corolla.json', { name: 'קורולה לבנה', type: 'קורולה', transmission: 'automatic' });
write('car-i20.json', { name: 'i20 כסופה', type: 'i20', transmission: 'manual' });
write('car-picanto.json', { name: 'פיקנטו אדומה', type: 'פיקנטו', transmission: 'automatic' });
write('car-mazda.json', { name: 'מאזדה 3 אפורה', type: 'מאזדה 3', transmission: 'manual' });
EOF
node fleet.js

RONIT=$(create teachers teacher-ronit.json); YAEL=$(create teachers teacher-yael.json)
OREN=$(create teachers teacher-oren.json); MICHAL=$(create teachers teacher-michal.json)
COROLLA=$(create cars car-corolla.json); I20=$(create cars car-i20.json)
PICANTO=$(create cars car-picanto.json); MAZDA=$(create cars car-mazda.json)
assign $COROLLA $RONIT; assign $I20 $RONIT; assign $I20 $YAEL; assign $PICANTO $YAEL; assign $MAZDA $OREN
assign $COROLLA $OREN

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
  ['lia', 'ליה חדד', '31654029', '053-4401277', oren, mazda],
  ['roi', 'רועי אלמוג', '20933176', '058-2290415', oren, corolla],
];
const lines = [];
for (const [key, name, body, phone, teacherId, carId] of students) {
  fs.writeFileSync(`student-${key}.json`, JSON.stringify({ nationalId: id(body), name, phone, teacherId, carId, address: null, startDate: null, licenseType: null }));
  lines.push(`${key.toUpperCase()}_ID=${id(body)}`);
}
fs.writeFileSync('national-ids.txt', lines.join('\n') + '\n');
EOF
node students.js "$RONIT" "$YAEL" "$OREN" "$COROLLA" "$I20" "$PICANTO" "$MAZDA"
for KEY in noa omer tamar lia roi; do
  ID=$(create students student-$KEY.json); echo "${KEY^^}=$ID" >> student-ids.txt; echo "$KEY $ID"
done
. ./student-ids.txt; . ./national-ids.txt
curl -s -o /dev/null -w "unassign corolla from oren %{http_code}\n" -X DELETE "$API/api/cars/$COROLLA/teachers/$OREN" -H "$(auth)"
curl -s "$API/api/students/find" -H "$(auth)" | json "filter(x => !x.isCarOfTeacher).map(x => x.id === '$ROI').join(' ')"
```

Expected: six `assign` 2xx codes, five Student ids, `unassign corolla from oren 204`, and the last line `true` (Roi, on the Corolla that Oren no longer teaches on, is the only flagged Student). If a route differs, read the controller and adapt the script.

- [ ] **Step 4: Identification after Change Teacher, and fixing a flagged Student, on Postgres (Review Focus 2 and 4)**

Open a week for Ronit **and** Yael and publish it with a window open now (`2026-10-11` is a Sunday after today; both week schedules join the week's one Publication):

```bash
WEEK=2026-10-11
for T in $RONIT $YAEL; do
  printf '{"teacherId":"%s","weekStart":"%s"}' "$T" "$WEEK" > week.json
  curl -s -o /dev/null -w "week %{http_code}\n" -X POST $API/api/week-schedules -H "$(auth)" -H "Content-Type: application/json" -d @week.json
done
curl -s "$API/api/publications/by-week?week=$WEEK" -H "$(auth)" > publication.json
PUB_ID=$(json id < publication.json); LINK=$(json linkToken < publication.json)
printf '{"startUtc":"%s","endUtc":"%s"}' "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)" > window.json
curl -s -o /dev/null -w "publish %{http_code}\n" -X POST "$API/api/publications/$PUB_ID/publish" -H "$(auth)" -H "Content-Type: application/json" -d @window.json
sleep 5
echo "state $(curl -s $API/api/submissions/by-link/$LINK | json state)"
printf 'LINK=%s\n' "$LINK" >> student-ids.txt

identify() { printf '{"nationalId":"%s"}' "$1" > identify.json; curl -s -X POST "$API/api/submissions/by-link/$LINK/identify" -H "Content-Type: application/json" -d @identify.json > identified.json; }

identify $NOA_ID
node -e "const r = require('./identified.json'); const slot = r.slots.find(s => s.state === 'open'); require('fs').writeFileSync('submit.json', JSON.stringify({ nationalId: process.argv[1], targetCount: 1, slotRequests: [{ slotId: slot.id, sessionType: 'single', constraint: null }] })); console.log('teacher before', r.teacherName === 'רונית אברהם')" $NOA_ID
echo "submit $(curl -s -o body.json -w "%{http_code}" -X POST "$API/api/submissions/by-link/$LINK" -H "Content-Type: application/json" -d @submit.json)"

printf '{"teacherId":"%s","carId":"%s"}' "$YAEL" "$I20" > to-yael.json
echo "change teacher $(curl -s -o body.json -w "%{http_code}" -X POST "$API/api/students/$NOA/change-teacher" -H "$(auth)" -H "Content-Type: application/json" -d @to-yael.json)"

identify $NOA_ID
node -e "const r = require('./identified.json'); const old = r.submission.slotRequests[0].slotId; const yaelSlot = r.slots.find(s => s.state === 'open'); console.log('teacher after', r.teacherName === 'יעל כרמי', 'old pick not in grid', !r.slots.some(s => s.id === old)); require('fs').writeFileSync('revise.json', JSON.stringify({ nationalId: process.argv[1], targetCount: 1, slotRequests: [{ slotId: yaelSlot.id, sessionType: 'single', constraint: null }] }))" $NOA_ID
echo "revise $(curl -s -o body.json -w "%{http_code}" -X PUT "$API/api/submissions/by-link/$LINK" -H "Content-Type: application/json" -d @revise.json)"
echo "submit again $(curl -s -o body.json -w "%{http_code}" -X POST "$API/api/submissions/by-link/$LINK" -H "Content-Type: application/json" -d @revise.json) $(json code < body.json)"

printf '{"carId":"%s"}' "$MAZDA" > to-mazda.json
echo "fix roi $(curl -s -o body.json -w "%{http_code}" -X POST "$API/api/students/$ROI/change-car" -H "$(auth)" -H "Content-Type: application/json" -d @to-mazda.json)"
curl -s "$API/api/students/find" -H "$(auth)" | json "filter(x => !x.isCarOfTeacher).length"
```

Expected:
- `week 201` twice, `publish 204`, `state open` (wait a few more seconds if it still says `published`).
- `teacher before true`, `submit 201` (or `204`).
- `change teacher 204`.
- `teacher after true old pick not in grid true` (identification resolves Yael's grid; the saved Submission is still loaded, its old pick is not in the new grid, so the form drops it - README decision 9).
- `revise 204` (the one Submission moves onto Yael's Week Schedule), `submit again 409 submissionAlreadyExists` (still one Submission per Student and Publication).
- `fix roi 204`, then `0` (no flagged Student left; Review Focus 2).

If the identify / submit JSON uses other field or enum names (for example `sessionType` as a number), read `IdentifyStudentResponse.cs` and `SlotRequestForSubmissionRequest.cs` and adapt the script, not the code. If `revise` fails, that is a defect against README decision 9: stop and report it with the response body before going on.

Reset Roi for the browser walk (unassign happens again, so he is flagged once more) and Noa back to Ronit on the Corolla:

```bash
printf '{"carId":"%s"}' "$COROLLA" > to-corolla.json
curl -s -o /dev/null -w "assign corolla to oren %{http_code}\n" -X POST "$API/api/cars/$COROLLA/teachers/$OREN" -H "$(auth)"
curl -s -o /dev/null -w "roi back %{http_code}\n" -X POST "$API/api/students/$ROI/change-car" -H "$(auth)" -H "Content-Type: application/json" -d @to-corolla.json
curl -s -o /dev/null -w "unassign %{http_code}\n" -X DELETE "$API/api/cars/$COROLLA/teachers/$OREN" -H "$(auth)"
printf '{"teacherId":"%s","carId":"%s"}' "$RONIT" "$COROLLA" > to-ronit.json
curl -s -o /dev/null -w "noa back %{http_code}\n" -X POST "$API/api/students/$NOA/change-teacher" -H "$(auth)" -H "Content-Type: application/json" -d @to-ronit.json
```

Expected: four 2xx codes.

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200`, sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew) and open "תלמידים".

Screenshots of this app can hang or come back half-rendered (memory note). Before each screenshot run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s; if one still hangs, verify through `read_page`, `get_page_text`, `read_network_requests` and `getComputedStyle`. No check below depends on a screenshot alone.

- [ ] **Step 5: The list marker and row menus in Hebrew (frames 2c, 2d, 2e)**

- [ ] Roi's Car cell shows "קורולה לבנה" + "אוטומט" tag and, under it, the marker "לא רכב של המורה" with the alert icon; `getComputedStyle` of `.car-flag` gives the `--p-red-600` colour and a dotted bottom border. No other row has a marker.
- [ ] Tab to the marker: focus is visible and the tooltip opens under it with "הרכב הזה אינו אחד מהרכבים של אורן לוי. כנראה נקבע לפני שהכלל נכנס לתוקף. אפשר לתקן ב"החלפת רכב"."; hovering opens it too; the name reads correctly inside the Hebrew sentence.
- [ ] Omer's kebab: "עריכת פרטים" · "החלפת מורה" · "החלפת רכב" · separator · "סימון כלא פעיל" (frame 2e).
- [ ] Roi's kebab: "החלפת רכב" has the alert icon in plum, a muted background and a plum outlined pill "תיקון" at the inline end (the left, in RTL) (frame 2d).
- [ ] Filter "הכול" and deactivate Tamar from her menu; her row shows only "עריכת פרטים" · "סימון כפעיל" (no Change items). Reactivate her.

- [ ] **Step 6: Change Teacher in Hebrew (frames 6a to 6g; Review Focus 1, 3, 5)**

- [ ] Omer → "החלפת מורה": 560px dialog, header "החלפת מורה", who card (Omer, ID LTR, "מורה: רונית אברהם", "רכב: i20 כסופה" + "ידני"). Car Select disabled with "יש לבחור מורה קודם"; "החלפת מורה" button disabled; the info line under the fields.
- [ ] Open the Teacher list (6c): "רונית אברהם" disabled with "(נוכחי)", "מיכל בן דוד" with "אין רכבים".
- [ ] Choose "יעל כרמי" (6a): the Car shows "i20 כסופה" + "ידני" + "(הרכב הנוכחי)", hint "גם יעל כרמי מלמד/ת על הרכב הנוכחי, ולכן הוא נבחר.", Save enabled.
- [ ] Choose "אורן לוי" (he doesn't teach on the i20 and has one Car): "מאזדה 3 אפורה" is preselected with "זה הרכב היחיד של אורן לוי, ולכן הוא נבחר." Choose "מיכל בן דוד" (6d): Car disabled, the info message with the "מעבר לרכבים ומורים" link, Save disabled.
- [ ] Choose "יעל כרמי" again and save (6g): toast "המורה הוחלף" / "המורה של עומר שלו: יעל כרמי, על i20 כסופה."; Omer's row now shows Yael.
- [ ] Noa (Ronit, Corolla) → "החלפת מורה" → "יעל כרמי" (6b): Car empty, the list holds only "i20 כסופה" and "פיקנטו אדומה", hint "מוצגים רק הרכבים של יעל כרמי.", Save disabled until a Car is chosen. Cancel.
- [ ] Stale (6e, Review Focus 3): open Noa's Change Teacher in this tab and choose Yael + i20; in a second tab change Noa to Yael on the i20 (or run the `to-yael.json` curl); save in the first tab: the error Message "יעל כרמי כבר המורה של התלמיד הזה. יש לרענן את המסך." with "רענון", the Teacher field invalid with the same line. Click "רענון": the dialog closes and the list shows Noa with Yael. Put Noa back on Ronit + Corolla.

- [ ] **Step 7: Change Car in Hebrew (frames 7a to 7f; Review Focus 2, 5)**

- [ ] Noa → "החלפת רכב" (7a): who card; label "רכב חדש"; two cards "קורולה לבנה" (disabled, muted, "(נוכחי)") and "i20 כסופה"; hint "מוצגים רק הרכבים של רונית אברהם."; "החלפת רכב" disabled until the i20 card is chosen; the chosen card has the sky border and `#F2F7FF` background; keyboard: Tab reaches the radio group, arrow keys move between enabled cards. Save (7f): toast "הרכב הוחלף" / "נועה מזרחי ילמד/תלמד על i20 כסופה."
- [ ] Lia → "החלפת רכב" (7b): the info Message "לאורן לוי אין רכבים אחרים." + hint + link; the footer has only "סגירה".
- [ ] Roi → "החלפת רכב" from the "תיקון" item (7c): the error Message "הרכב הנוכחי אינו אחד מהרכבים של אורן לוי" / "קורולה לבנה נקבע לפני שהכלל נכנס לתוקף. יש לבחור אחד מהרכבים של אורן לוי."; the "מאזדה 3 אפורה" card enabled; save: Roi's marker disappears, the who card's alert icon is gone next time.
- [ ] Stale (7d): open Noa's Change Car, choose a Car, change it in a second tab first, save: "זה כבר הרכב של התלמיד הזה. יש לרענן את המסך." with "רענון" (or 7e's wording if the second tab unassigned the Car from Ronit instead).
- [ ] Edit details on any active Student: under the locked "מורה ורכב" field, the hint "לשינוי: "החלפת מורה" או "החלפת רכב" בתפריט השורה."

- [ ] **Step 8: English (LTR)**

Switch the language to English and repeat the main path: marker text "Not this Teacher's Car" and its tooltip; menu "Change Teacher" / "Change Car" / "Fix"; Change Teacher (header, "New Teacher", "(current)", "(current Car)", hint "Yael Carmi also teaches on the current Car, so it's selected.", button "Change Teacher"); Change Car (cards, "(current)", "Close" in the no-other-Cars case). The badge sits at the right end, icons are not mirrored, nothing overlaps.

Also check the Inactive tooltip: deactivate Roi, filter "All", tab to his marker: "...To fix it with Change Car, mark the Student as active first."; his menu has no Change items. Reactivate him.

- [ ] **Step 9: 768px**

`resize_window` width 768, height 1024; reload `/students`, choose "הכול":
- [ ] The compact "מורה ורכב" column shows Roi's marker under his Car; the tooltip and the row menu open fully on screen; no horizontal scroll (`document.documentElement.scrollWidth > innerWidth` is false).
- [ ] Both dialogs fit the width; the Change Car cards don't wrap their transmission tag under the name.

Reset with preset `desktop`.

- [ ] **Step 10: Compare against the design**

If the `claude_design` MCP is connected, read `students/app.jsx`, `students/kit.jsx` (`StFlag`, `StRowMenu`, `StWho`) and `students/dialogs.jsx` (`StTeacherDlg`, `StCarDlg`, `StCarCard`) from project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, render frames 2c, 2d, 2e, 6a to 6h and 7a to 7f with `render_preview`, and compare them with steps 5 to 9. Check the copy word for word against `students/data.jsx` (`ST_GROUPS`, groups "Students screen", "Edit details", "Change Teacher", "Change Car"). Write down every visible difference for the PR. Expected differences (README decisions 11, 14, 16):
- The Inactive-row tooltip (`students.flag.tipInactive`) is new copy, not in the deck.
- Refresh closes the dialog instead of refreshing it in place.
- A flagged Student whose Teacher has no Cars gets both the flag Message and the no-other-Cars Message.
- English buttons in Title Case ("Change Teacher"), not the copy deck's capitals.

Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 11: Push and open the PR**

The plan is committed before task 1; confirm, then push:

```bash
git status --short docs/modules/students/us-95-change-teacher-and-car-plan
git push -u origin 95-change-teacher-and-car
```

Write `pr-body.md` in the scratchpad:

```markdown
Closes #95. Part of #82 (Students). Follows #93 and #94.

## What changed

- **Domain:** `Student.ChangeTeacher(Teacher, Car)` and `Student.ChangeCar(Car)` take resolved aggregates. The current Teacher / Car is refused (`StudentAlreadyWithTeacherException`, `StudentAlreadyOnCarException`); a Car that is not the new / current Teacher's is refused with the existing `StudentCarMustBeAssignedToTeacherException`. New events `StudentTeacherChanged`, `StudentCarChanged`. `Car.IsAssignedTo(TeacherId)`. Submissions are untouched.
- **API:** `POST api/students/{id}/change-teacher` (`teacherId`, `carId`) and `POST api/students/{id}/change-car` (`carId`), Administrator-only: 204; 404 `studentNotFound` / `teacherNotFound` / `carNotFound`; 409 `studentAlreadyWithTeacher` / `studentAlreadyOnCar` / `studentCarMustBeAssignedToTeacher`. `isCarOfTeacher` on the Students list and details.
- **Client:** the Students list marks a Car that is not the Teacher's (focusable marker + tooltip); active rows gain Change Teacher and Change Car (with a "Fix" badge on a flagged row). Change Teacher picks the Teacher then one of their Cars, keeping the current Car when the new Teacher teaches on it. Change Car offers the Teacher's Cars as radio cards, the current one disabled. Stale refusals offer Refresh. Edit details now points to both actions.
- **Translations:** `students.flag.*`, `students.actions.changeTeacher|changeCar|fix`, `students.changeTeacher.*`, `students.changeCar.*`, the two toasts, `students.edit.teacherAndCarHint`, `general.close`, `errors.studentAlreadyWithTeacher|studentAlreadyOnCar`. Hebrew first.

## For the spec owner

- The tooltip on an Inactive Student's marker is new copy ("...כדי לתקן ב"החלפת רכב", יש לסמן את התלמיד כפעיל."), since Inactive rows have no Change Car. Please confirm.
- A Student who already submitted this week and then changes Teacher keeps that one Submission; on the next identification they see the new Teacher's grid with their old picks dropped, and saving moves the Submission to the new Teacher. Until they save, the old Teacher's Excel still counts it. Please confirm that's acceptable.

## Verification

- Backend and client suites pass; no pending model changes.
- API smoke (task 2): <fill in>.
- Postgres end to end (step 4): <fill in>.
- Browser, Hebrew (RTL), English, 768px: <fill in from steps 5-9>.
- Design differences: <fill in from step 10>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the `<fill in ...>` lines with what actually happened, then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 95-change-teacher-and-car --title "Change Teacher and Change Car for a Student (#95)" --body-file "<scratchpad>/pr-body.md"
```

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails. `Closes #95` won't auto-close the issue on the non-default base: close it when `82-users-and-roles` merges to `main`.

- [ ] **Step 12: Drop the throwaway database**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us95_verify"
```
