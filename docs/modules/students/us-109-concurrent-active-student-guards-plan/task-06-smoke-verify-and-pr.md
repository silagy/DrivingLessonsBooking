# Task 6 of 6: Full suites, a deterministic race smoke on Postgres, and the PR

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires tasks 1 to 5 committed.

**Files:**
- No planned changes. A step that finds a defect fixes it in the task's file, with a failing test first where one can be written, as its own commit `fix(students): <what> (#109)`, before the PR.

**Interfaces:**
- Consumes: everything from tasks 1 to 5. The smoke relies on the routes `POST api/teachers`, `POST api/cars`, `POST api/cars/{id}/teachers/{teacherId}`, `DELETE api/cars/{id}/teachers/{teacherId}`, `DELETE api/teachers/{id}`, `POST api/students`, `POST api/students/{id}/change-car` with `{carId}`, and `POST api/students/{id}/change-teacher` with `{teacherId, carId}`. It also relies on the columns `students.is_active`, `teacher_id`, `car_id`, `teachers.is_deleted`, `cars.is_deleted` and `car_teachers(car_id, teacher_id)`.
- Produces: the PR into `82-users-and-roles` that closes #109.

**Why:** #109 AC 2: "two concurrent requests (delete vs. add, unassign vs. change) never leave an active Student on a deleted Teacher or Car, or on a Car that isn't their Teacher's. The losing request gets a 409 with a translated message." The interactor tests prove the order of calls, and this step proves that real requests on Postgres take turns.

**How the race is made deterministic:**
1. A `psql` session takes the row lock first and holds it for 4 seconds: `BEGIN; SELECT ... FOR UPDATE; SELECT pg_sleep(4); COMMIT`.
2. Request 1 is sent at about 1 s and blocks on the lock. Request 2 is sent at about 2 s and queues behind it.
3. When `psql` commits, Postgres hands the lock to request 1, then to request 2.

`curl`'s `time_total` shows that a request waited (≥ 2 s), which proves it took the lock.

- [ ] **Step 1: Full suites and build**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: 0 warnings, every suite green, `No changes have been made to the model since the last migration.`

- [ ] **Step 2: A throwaway database and the API on it**

```bash
docker stop dl-postgres 2>/dev/null; docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us109_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us109_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us109_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait until its output shows `Now listening on: http://localhost:5080`.

- [ ] **Step 3: Seed Teachers, Cars and two Students**

All names are ASCII, so no Hebrew passes through the Windows command line. Work in a scratchpad folder (`<scratchpad>` is this session's scratchpad directory):

```bash
mkdir -p "<scratchpad>/smoke-109" && cd "<scratchpad>/smoke-109"
API=http://localhost:5080
PG="docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us109_smoke -At"
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
code() { node -pe "try { JSON.parse(require('fs').readFileSync('$1','utf8')).code } catch (e) { '' }"; }
auth() { echo "Authorization: Bearer $ADMIN"; }
req() { curl -s -o "$3" -w "%{http_code} %{time_total}" -X "$1" "$API/api/$2" -H "$(auth)" -H "Content-Type: application/json" ${4:+-d @"$4"}; }
hold() { $PG -c "BEGIN; SELECT id FROM $1 WHERE id = '$2' FOR UPDATE; SELECT pg_sleep(4); COMMIT;" > /dev/null & }
teacher() { printf '{"name":"%s","contactEmail":"%s@school.example"}' "$1" "$2" > t.json; req POST teachers t-out.json t.json > /dev/null; json id < t-out.json; }
car() { printf '{"name":"%s","type":"%s","transmission":"%s"}' "$1" "$1" "$2" > c.json; req POST cars c-out.json c.json > /dev/null; json id < c-out.json; }
assign() { req POST "cars/$1/teachers/$2" a-out.json | cut -d' ' -f1; }
student() { printf '{"nationalId":"%s","name":"%s","phone":"050-1234567","teacherId":"%s","carId":"%s","address":null,"startDate":null,"licenseType":null}' "$1" "$2" "$3" "$4" > "$5"; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken)

RONIT=$(teacher "Ronit Avraham" ronit); YAEL=$(teacher "Yael Carmi" yael); TAL=$(teacher "Tal Ben Ami" tal)
COROLLA=$(car "Corolla White" automatic); I20=$(car "i20 Silver" manual); POLO=$(car "Polo Blue" manual); MAZDA=$(car "Mazda Grey" manual)
echo "assign $(assign $COROLLA $RONIT) $(assign $I20 $RONIT) $(assign $I20 $YAEL) $(assign $POLO $YAEL) $(assign $MAZDA $YAEL) $(assign $POLO $TAL)"

student 312456783 "Omer Shalev" $YAEL $I20 omer.json; echo "omer $(req POST students s-out.json omer.json)"; OMER=$(json id < s-out.json)
student 000000018 "Dana Levi" $RONIT $I20 dana.json; echo "dana $(req POST students s-out.json dana.json)"; DANA=$(json id < s-out.json)
```

Expected: `assign` prints six 2xx codes; `omer 201 ...`, `dana 201 ...`. If a route or payload field differs from what this step assumes, read the controller and request DTO and adapt the script; don't change the code to fit the script.

- [ ] **Step 4: Run the five races**

```bash
# A - Add Student, then Delete Teacher (same Teacher)
student 205374184 "Noa Mizrahi" $RONIT $COROLLA noa.json
hold teachers $RONIT; sleep 1
(echo "A add $(req POST students a1.json noa.json)" > a1.txt) & sleep 1
(echo "A delete $(req DELETE teachers/$RONIT a2.json) $(code a2.json)" > a2.txt) & wait
cat a1.txt a2.txt

# B - Delete Teacher, then Add Student (same Teacher)
student 123456782 "Lior Golan" $TAL $POLO lior.json
hold teachers $TAL; sleep 1
(echo "B delete $(req DELETE teachers/$TAL b1.json)" > b1.txt) & sleep 1
(echo "B add $(req POST students b2.json lior.json) $(code b2.json)" > b2.txt) & wait
cat b1.txt b2.txt

# C - Change Car, then Unassign that Car from the Teacher
printf '{"carId":"%s"}' $POLO > omer-polo.json
hold cars $POLO; sleep 1
(echo "C change $(req POST students/$OMER/change-car c1.json omer-polo.json)" > c1.txt) & sleep 1
(echo "C unassign $(req DELETE cars/$POLO/teachers/$YAEL c2.json) $(code c2.json)" > c2.txt) & wait
cat c1.txt c2.txt

# D - Unassign a Car from the Teacher, then Change Car to it
printf '{"carId":"%s"}' $MAZDA > omer-mazda.json
hold cars $MAZDA; sleep 1
(echo "D unassign $(req DELETE cars/$MAZDA/teachers/$YAEL d1.json)" > d1.txt) & sleep 1
(echo "D change $(req POST students/$OMER/change-car d2.json omer-mazda.json) $(code d2.json)" > d2.txt) & wait
cat d1.txt d2.txt

# E - the same Student: Change Car (blocked on Ronit) while Change Teacher (Yael + Polo) commits
printf '{"carId":"%s"}' $COROLLA > dana-corolla.json
printf '{"teacherId":"%s","carId":"%s"}' $YAEL $POLO > dana-yael.json
hold teachers $RONIT; sleep 1
(echo "E change car $(req POST students/$DANA/change-car e1.json dana-corolla.json) $(code e1.json)" > e1.txt) & sleep 1
(echo "E change teacher $(req POST students/$DANA/change-teacher e2.json dana-yael.json)" > e2.txt) & wait
cat e1.txt e2.txt
```

Expected (`time_total` is the second number):
- **A:** `A add 201 <≥2.0>`; `A delete 409 <≥2.0> teacherMustNotHaveActiveStudents`. The delete waited, then saw Noa.
- **B:** `B delete 204 <≥2.0>`; `B add 404 <≥2.0> teacherNotFound`. The add waited, then found Tal deleted (README decision 7).
- **C:** `C change 204 <≥2.0>`; `C unassign 409 <≥2.0> teacherAssignmentMustNotHaveActiveStudents`.
- **D:** `D unassign 204 <≥2.0>`; `D change 409 <≥2.0> studentCarMustBeAssignedToTeacher`.
- **E:** `E change teacher 204 <under 1.0>`. It locks Yael and the Polo, which nobody holds, so it doesn't wait. `E change car 409 <≥2.0> concurrentChange`. It read Dana before Change Teacher committed, and the `xmin` check refused its save.
- **Review Focus 2:** E's `hold teachers $RONIT` gets Ronit's lock straight away, although A's refused delete held that lock earlier. That shows a refused command releases its locks. Had it not, E's requests would hang past the hold and print `000`.

If a line shows 2xx for both requests of a race, or the second request never waits, a lock is missing or comes after a read: stop and fix it with a failing test first.

- [ ] **Step 5: Check the invariant and the end state**

```bash
$PG -c "SELECT count(*) FROM students s JOIN teachers t ON t.id = s.teacher_id JOIN cars c ON c.id = s.car_id WHERE s.is_active AND (t.is_deleted OR c.is_deleted OR NOT EXISTS (SELECT 1 FROM car_teachers ct WHERE ct.car_id = s.car_id AND ct.teacher_id = s.teacher_id))"
$PG -c "SELECT s.name, t.name, c.name FROM students s JOIN teachers t ON t.id = s.teacher_id JOIN cars c ON c.id = s.car_id ORDER BY s.name"
```

Expected:
- `0` from the first query.
- From the second: `Dana Levi|Yael Carmi|Polo Blue`, `Noa Mizrahi|Ronit Avraham|Corolla White`, `Omer Shalev|Yael Carmi|Polo Blue`. Lior isn't there.

Then stop the API (`TaskStop` on the background task) and drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us109_smoke"
```

- [ ] **Step 6: Push and open the PR**

```bash
git push -u origin 109-concurrent-active-student-guards
```

Open the PR with `gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 109-concurrent-active-student-guards`. Title: `Concurrent admin writes can't break the active-Student guards (#109)`. The body has:
- **Summary:** row locks on Teacher and Car before the active-Student checks, the `xmin` token on Student, the new 409 `concurrentChange`, and ADR 0007.
- **Acceptance criteria:** ADR 0007 recorded; the five races from step 4, each with its two result lines, and the invariant count `0`.
- **Decisions to look at:** README decisions 1, 2 and 7. Decision 7 is the 404 for a request whose Teacher was deleted in the race, where the AC says 409.
- **Not covered:** national-ID uniqueness races (decision 10); no automated database concurrency test (decision 9).
- `Closes #109` and `Part of #82`. Note that it won't auto-close on this base, so close #109 by hand after the merge.
- The PR-description attribution line from the session's instructions, at the end.

After it opens, follow the session's PR instructions: bind the PR and read its status.
