# #95: Change Teacher and Change Car for a Student - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95), a slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #95.

**Goal:** From the Students screen, Administrators record a Student switching Teacher (choosing one of the new Teacher's Cars in the same step, the current Car preselected when the new Teacher also teaches on it) or switching Car among the current Teacher's Cars; the list flags Students whose Car is not one of their Teacher's Cars so an Administrator can fix them with Change Car.

**Architecture:**
- Backend: `Student.ChangeTeacher(Teacher, Car)` and `Student.ChangeCar(Car)` take resolved aggregates, refuse the current Teacher / current Car with the new `StudentAlreadyWithTeacherException` / `StudentAlreadyOnCarException` (409 `studentAlreadyWithTeacher` / `studentAlreadyOnCar`), refuse a Car that is not the (new / current) Teacher's with the existing `StudentCarMustBeAssignedToTeacherException` (409 `studentCarMustBeAssignedToTeacher`), and emit the new `StudentTeacherChanged` / `StudentCarChanged`. `Car.IsAssignedTo(TeacherId)` is added so `ChangeCar` can check the current Teacher without loading it. `ChangeStudentTeacherInteractor` (`POST api/students/{id}/change-teacher`) and `ChangeStudentCarInteractor` (`POST api/students/{id}/change-car`) load Student (404 `studentNotFound`), Teacher (404 `teacherNotFound`), Car (404 `carNotFound`), call the domain and commit. `FindStudents` and `GetStudent` gain `IsCarOfTeacher`. Submissions are not touched; identification already resolves the Student's current Teacher (`StudentQueries.GetActiveByNationalIdAsync` joins on `student.TeacherId`).
- Client: `car-pick.ts` gains `pickCarForNewTeacher` (current Car preselected when shared) and `car-change.ts` the Change Car choices; the page-scoped `StudentsStore` gains `changeTeacher`, `changeCar`; `StudentRefusalKind` gains `sameTeacher`, `sameCar`. The table shows a focusable "Not this Teacher's Car" marker with a tooltip; the row menu of an active Student gains **Change Teacher** and **Change Car** (the latter with a "Fix" badge on a flagged row). Both are DynamicDialogs fed with store signals (the #93 / #94 pattern).

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration (`IsCarOfTeacher` is a projection, not a column).

**Spec:** issue [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95) (acceptance criteria) · predecessors [#93](https://github.com/silagy/DrivingLessonsBooking/issues/93) ([plan](../us-93-students-screen-plan/README.md)), [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94) ([plan](../us-94-edit-deactivate-reactivate-plan/README.md)) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · design brief [claude-design-prompt.md](../claude-design-prompt.md) · [docs/requirements.md](../../../requirements.md) §5.5, decision #23 ("a student's car is always one of their teacher's cars") · [CONTEXT.md](../../../../CONTEXT.md) glossary ("Change Teacher", "Change Car") · `.claude\rules\*.md`

**Design:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, file `Students.html`, built from `students/app.jsx` (frame list), `students/data.jsx` (copy deck `ST_GROUPS`, sample data), `students/kit.jsx` (`StFlag`, `StRowMenu`, `StWho`, `StSelect`, `StTx`), `students/dialogs.jsx` (`StTeacherDlg`, `StCarDlg`, `StCarCard`, `StTeacherField`, `StCarField`, `StNoCars`, `StErr`), `students/more.jsx` (`StInventory` decisions and tokens), on top of `auth/kit.jsx`. Frames built by this story:

| Frame | What it shows |
|-------|---------------|
| 2c | Car-not-of-Teacher marker under the Car + transmission tag: plum (`--p-red-600`) alert icon + "לא רכב של המורה", 12.5px semibold, 1px dotted plum underline, focusable; tooltip (dark, 270px, under the marker) "הרכב הזה אינו אחד מהרכבים של {t}. כנראה נקבע לפני שהכלל נכנס לתוקף. אפשר לתקן ב"החלפת רכב"." |
| 2d | Row menu of a flagged active Student: "עריכת פרטים" (pencil) · "החלפת מורה" (swap) · "החלפת רכב" on a muted background with a plum alert icon and a plum pill badge "תיקון" at the inline end · separator · "סימון כלא פעיל" (danger). |
| 2e | Row menu of an active Student: "עריכת פרטים" · "החלפת מורה" · "החלפת רכב" (car icon) · separator · "סימון כלא פעיל". (2f, Inactive: unchanged from #94, no Change items.) |
| 6a | Change Teacher dialog, 560px: header "החלפת מורה"; who card; "מורה חדש" Select; "רכב" Select with the current Car preselected, label "(הרכב הנוכחי)" after the transmission tag, hint "גם {t} מלמד/ת על הרכב הנוכחי, ולכן הוא נבחר."; info line "הגשות קיימות נשארות בשבוע שבו נעשו. מההזדהות הבאה, התלמיד יראה את השבוע של המורה החדש."; footer text Cancel + primary "החלפת מורה". |
| 6b | Car not shared: Car Select empty (open: only the new Teacher's Cars with transmission tags), hint "מוצגים רק הרכבים של {t}.", Save disabled. |
| 6c | Teacher list: the current Teacher disabled with "(נוכחי)", a Teacher without Cars noted "אין רכבים". |
| 6d | New Teacher has no Cars: Car Select disabled with the info message "למורה הזה עדיין אין רכבים..." + link "מעבר לרכבים ומורים"; Save disabled. |
| 6e / 6f | Refused (stale): error Message first in the dialog, "{t} כבר המורה של התלמיד הזה. יש לרענן את המסך." / "הרכב הזה לא משויך ל{t}. יש לרענן ולבחור שוב.", a "רענון" link inside it; the related field invalid with the same line under it. |
| 6g | Success toast "המורה הוחלף" / "המורה של {name}: {t}, על {c}." |
| 7a | Change Car dialog, 560px: header "החלפת רכב"; who card; label "רכב חדש"; radio cards (48px min, 10px radius, car icon, name, transmission tag), the current Car disabled with "(נוכחי)", the selected card border `--p-sky-500` on `#F2F7FF`; hint "מוצגים רק הרכבים של {t}."; footer text Cancel + primary "החלפת רכב" (disabled until a choice). |
| 7b | Teacher has no other Cars: info Message "ל{t} אין רכבים אחרים." + "אפשר לשייך עוד רכב למורה במסך רכבים ומורים." + link; footer only "סגירה". |
| 7c | Flagged Student: error Message "הרכב הנוכחי אינו אחד מהרכבים של {t}" / "{c} נקבע לפני שהכלל נכנס לתוקף. יש לבחור אחד מהרכבים של {t}." above the cards; every card enabled. |
| 7d / 7e | Refused (stale): "זה כבר הרכב של התלמיד הזה. יש לרענן את המסך." / "הרכב הזה כבר לא משויך ל{t}. יש לרענן ולבחור שוב." with the "רענון" link. |
| 7f | Success toast "הרכב הוחלף" / "{name} ילמד/תלמד על {c}." |

Every frame's layout, sizes, colors and copy are spelled out in the task that builds it, so the tasks can be built without the `claude_design` MCP. Task 7 compares against the frames when the MCP is connected.

**Branch:** `95-change-teacher-and-car`, from `82-users-and-roles` at `86d1a4d` (contains #93 and #94). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Student aggregate | `Create`, `UpdateFromRoster` (both guard `MustBeCarOfTeacher(car, teacher)` → `StudentCarMustBeAssignedToTeacherException(CarId, TeacherId)`), `ChangeDetails`, `Deactivate`, `Reactivate`. No Change Teacher / Change Car. | `src\DrivingLessons.Domain\Entities\Student.cs` |
| Car | `IsAssignedTo(Teacher)` over `TeacherAssignments` (each has `TeacherId`). No `TeacherId` overload. | `src\DrivingLessons.Domain\Entities\Car.cs` |
| Repositories | `IStudentRepository.GetAsync(StudentId)` (#94), `ITeacherRepository.GetAsync(TeacherId)`, `ICarRepository.GetAsync(CarId)` | `Domain\Repositories\` |
| Student endpoints | `GET find`, `GET {id}`, `POST`, `PUT {id}/details`, `POST {id}/deactivate|reactivate`; Administrator-only by fallback; `Students_Stay_Administrator_Only` pins them | `Presentation.Web\Controllers\Student\`, `tests\...\Auth\ControllerAuthorizationTest.cs` |
| List / details queries | `FindAsync` and `GetAsync` join Student → Teacher → Car; no Car-of-Teacher information | `Infrastructure\EntityFramework\Queries\StudentQueries.cs`, `Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`, `Queries\GetStudent\GetStudentResponse.cs` |
| Identify | `GetActiveByNationalIdAsync` picks the grid by `student.TeacherId` and loads this week's Submission by Student + week; `loadedSubmissionOf` (client) drops saved picks that aren't open Slots of the grid shown (`droppedPickCount`); `ReviseSubmission` re-points the Submission to the Student's current Week Schedule | `StudentQueries.cs`, `client\...\student-form\domain\loaded-submission.ts`, `Domain\Entities\Submission.cs` (`Revise`) |
| Unassigning a Car | `DELETE api/cars/{id}/teachers/{teacherId}` is allowed while Students learn on it: this is how a Student ends up on a Car that is not their Teacher's | `Application\Commands\UnassignCarFromTeacher\` |
| Client Students | List, filters, search, Add, Edit details, Deactivate, Reactivate. `pickCarFor` (Add's Teacher → Car pick), `StudentRefusalKind { nationalIdInUse, nationalIdInvalid, staleCar, other }` (`studentCarMustBeAssignedToTeacher` and `carNotFound` are `staleCar`), `StudentWhoCardComponent`, row menu built in `students.page.ts` (`activeRowActions`, `inactiveRowActions`) | `client\src\app\features\students\` |
| Edit details | Locked "מורה ורכב" field; the copy deck's `edit.tcHint` was left out until this story (#94 decision 16) | `ui\dialogs\edit-student\edit-student.dialog.html` |
| Translations | `general.refresh`, `general.cancel` exist; **no `general.close`**; `errors.studentCarMustBeAssignedToTeacher`, `errors.carNotFound`, `errors.teacherNotFound` exist | `client\public\i18n\he.json`, `en.json` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Endpoints are business actions**: `POST api/students/{id}/change-teacher` with `ChangeStudentTeacherRequest(Guid TeacherId, Guid CarId)` and `POST api/students/{id}/change-car` with `ChangeStudentCarRequest(Guid CarId)`, both → 204 (`api-guidelines.md`: business actions are POST sub-resources; they are not idempotent). Administrator-only by fallback; two new `[DataRow]`s in `Students_Stay_Administrator_Only`. |
| 2 | **Guard order**: `ChangeTeacher` checks "not the current Teacher" first, then "Car is the new Teacher's"; `ChangeCar` checks "not the current Car" first, then "Car is the current Teacher's". The design words "same" as a stale screen, so it wins over the Car rule. A rejected change leaves the Student untouched and adds no event. |
| 3 | **New exceptions** `StudentAlreadyWithTeacherException(StudentId, TeacherId)` → `studentAlreadyWithTeacher` and `StudentAlreadyOnCarException(StudentId, CarId)` → `studentAlreadyOnCar` (codes derive from the type name). The Car rule reuses `StudentCarMustBeAssignedToTeacherException` (same rule as Create and the Roster, same code). |
| 4 | **New events** `StudentTeacherChanged(StudentId, TeacherId, CarId)` and `StudentCarChanged(StudentId, CarId)` (critical rule 9). |
| 5 | **`Car.IsAssignedTo(TeacherId)`** is added and `IsAssignedTo(Teacher)` delegates to it. `ChangeCar(Car)` (the AC's signature) then checks the current Teacher without the interactor loading it. |
| 6 | **No Active guard**: an Inactive Student may change Teacher or Car in the domain (the Roster can already move them). The UI hides both actions on Inactive rows (design decision "Inactive rows"). |
| 7 | **Interactor order**: Student (404) → Teacher (404, Change Teacher only) → Car (404) → domain → one commit, like `CreateStudentInteractor`. No Submission repository is involved: Submissions are not modified (AC), pinned by a domain test that a Submission keeps its Publication and Week Schedule after Change Teacher. |
| 8 | **Identification after Change Teacher needs no code change**: the active lookup joins on the Student's current `TeacherId`. `IdentifyStudentInteractorTest` gains a test that the interactor answers with what the lookup resolves for the Student now, and task 7's smoke proves it end to end on Postgres (identify after Change Teacher returns the new Teacher's name and Slots). |
| 9 | **A Submission made this week before Change Teacher stays with the Publication** (AC "Submissions are not modified"). On the next identification the Student sees the new Teacher's grid; the saved picks belong to the old grid, so `loadedSubmissionOf` drops them (the existing "some picks were dropped" notice), and saving revises the same Submission onto the new Teacher's Week Schedule (`Submission.Revise` sets `WeekScheduleId`). This is the existing behavior for a Roster that moves a Student; task 7 step 4 checks it. **Challenge on review** if the old Teacher's Excel should drop that Submission immediately rather than on revision. |
| 10 | **`IsCarOfTeacher`** is computed by the queries (`car.TeacherAssignments.Any(a => a.TeacherId == teacher.Id)`) on both `ItemForFindStudentsResponse` and `GetStudentResponse` (client `StudentDetails extends Student`). The rule lives in the backend; the client only renders it (critical rule 12). |
| 11 | **The marker shows on every row**, active and Inactive (AC: "flags Students"), muted on an Inactive row. Inactive rows offer no Change Car (design), so their tooltip ends differently: "...Mark the Student as active to fix it with Change Car." (new copy `students.flag.tipInactive`, **spec owner to confirm**). |
| 12 | **Change Teacher preselection** (`pickCarForNewTeacher`, client spec): the current Car when the new Teacher teaches on it (hint `carKept`), otherwise the new Teacher's only Car (hint `carOnly`), otherwise none (hint `carScoped`, Save disabled). No Teacher chosen → Car Select disabled with "יש לבחור מורה קודם". |
| 13 | **Change Teacher's Teacher list** is every Teacher, sorted by name; the current Teacher is disabled with "(נוכחי)"; a Teacher without Cars stays selectable with the note "אין רכבים" (design 6c, 6d). |
| 14 | **Change Car choices** (`carChangeFor`, client spec): the current Teacher's Cars sorted by name; the current Car is disabled with "(נוכחי)". For a flagged Student the current Car is not among them, so every card is enabled. "No other Cars" (7b: info Message + Close only) whenever no card is selectable, also for a flagged Student whose Teacher has no Cars (the flag Message shows above it). |
| 15 | **Refusals** gain `StudentRefusalKind.sameTeacher` (`studentAlreadyWithTeacher`) and `sameCar` (`studentAlreadyOnCar`). The dialogs word them with the chosen Teacher's name (`students.changeTeacher.sameTeacher`, `.carNotOfTeacher`, `students.changeCar.sameCar`, `.carGone`); other refusals show `message`. `errors.studentAlreadyWithTeacher` / `errors.studentAlreadyOnCar` exist for the generic path. |
| 16 | **Refresh in a stale refusal closes the dialog and reloads Students, Teachers and Cars** (`store.reload()`): the dialog was opened on a row snapshot that is now wrong, so the Administrator reopens it on fresh data. Changing the Teacher or Car selection clears the refusal (as typing the national ID does in Add / Edit). |
| 17 | **Success** closes the dialog, reloads the list and toasts `students.teacherChanged` + `students.teacherChangedDetail` ({name}, {teacher}, {car}) or `students.carChanged` + `students.carChangedDetail` ({name}, {car}); names direction-isolated. |
| 18 | **Row menu** (design 2d / 2e): active → Edit details · Change Teacher (`pi pi-arrow-right-arrow-left`) · Change Car (`pi pi-car`; flagged: `pi pi-exclamation-circle`, class `students-menu__item--fix`, PrimeNG `badge` "תיקון" with `badgeStyleClass` `students-menu__fix-badge`) · separator · Deactivate. Inactive → unchanged. |
| 19 | **Who card** shows a plum alert icon after the Car tag when `!isCarOfTeacher` (title and aria-label `students.flag.car`), as `StWho` does. |
| 20 | **Edit details gains the copy deck's `edit.tcHint`** under the locked "מורה ורכב" field (deferred by #94 decision 16): "לשינוי: "החלפת מורה" או "החלפת רכב" בתפריט השורה." |
| 21 | **Dialog widths** `35rem` (560px) for both. **`general.close`** is added ("סגירה" / "Close"). |
| 22 | **Smoke and browser checks run on throwaway databases** (`drivinglessons_us95_smoke`, `drivinglessons_us95_verify`) on the compose Postgres, like #93 / #94. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller/response boundaries and in query interfaces.
- Operations are not idempotent; every refusal is a domain-specific exception mapped by `ApiExceptionFilter`.
- Prefer new domain events over modifying existing ones.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]`, `EndpointSummary` and `ProducesResponseType` on every action, zero logic.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only. National IDs and phones are LTR-isolated; names in translated sentences go through `isolateDirection`.
  - `core\` and `shared\` never import from `features\`; no feature imports another.
  - Business rules stay in the backend: the client shows the server's refusals and the server's `isCarOfTeacher`; it never decides which Car is allowed beyond scoping the pickers to the server's Car-Teacher assignments (as Add already does).
- Terminology: Student, Teacher, Car, Change Teacher, Change Car, Submission. Never "assign", "transfer", "reassign" or "move" a Student in user-visible copy (CONTEXT.md).

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~StudentTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/features/students/domain/car-pick.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

National IDs in backend tests and smokes must pass the check digit: use `205374184`, `312456783`, `000000018` (memory: the design's sample IDs fail it).

## Review Focus

1. **Change Teacher to a Teacher who also teaches on the Student's current Car keeps that Car without a click, and switching to a Teacher who doesn't clears the Car instead of keeping one the server would refuse.** Covered in task 4 (`pickCarForNewTeacher` spec: `keeps the current Car when the new Teacher teaches on it`, `starts empty when the new Teacher doesn't teach on the current Car`) and task 5 (dialog spec `clears the Car when the Teacher changes to one without the current Car`).
2. **A flagged Student (Car not of their Teacher) can be fixed with Change Car: every one of the Teacher's Cars is selectable, and the flag disappears after saving.** Covered in task 4 (`carChangeFor` spec: `offers every Car of the Teacher to a flagged Student`), task 6 (dialog spec `enables every card for a flagged Student`) and task 7 step 4 (fix a flagged Student; the marker is gone).
3. **Two Administrators changing the same Student: the second gets a clear stale refusal with Refresh, never a silent overwrite or a stuck dialog.** Covered in task 1 (`Change_Teacher__Must_Not_Be_Current_Teacher`, `Change_Car__Must_Not_Be_Current_Car`), task 4 (`keeps the same-Teacher refusal for the dialog`), task 5 (`refresh closes the dialog and reloads`) and task 7 step 5 (second tab).
4. **A Student who submitted this week, then changes Teacher, identifies on the student form and sees the new Teacher's grid; saving revises their one Submission instead of failing as "already submitted".** Covered in task 2 (`Identifies_With_The_Teacher_The_Student_Has_Now`) and task 7 step 4 (submit, Change Teacher, identify, revise on Postgres).
5. **A Teacher with no Cars, or no other Cars, never leaves the Administrator with an enabled Save that can only fail.** Covered in task 5 (`keeps Save disabled when the new Teacher has no Cars`) and task 6 (`shows Close only when the Teacher has no other Cars`).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\Student.cs` | `ChangeTeacher`, `ChangeCar`, guards | 1 |
| `src\DrivingLessons.Domain\Entities\Car.cs` | `IsAssignedTo(TeacherId)` | 1 |
| `src\DrivingLessons.Domain\Exceptions\StudentAlreadyWithTeacherException.cs`, `StudentAlreadyOnCarException.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Events\StudentTeacherChanged.cs`, `StudentCarChanged.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs` | `Change_Teacher*`, `Change_Car*` | 1 |
| `src\DrivingLessons.Application\Commands\ChangeStudentTeacher\*`, `Commands\ChangeStudentCar\*` | **New** interactors + requests | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register both | 2 |
| `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs` | `ChangeTeacherAsync`, `ChangeCarAsync` | 2 |
| `tests\DrivingLessons.Application.Test\Commands\ChangeStudentTeacherInteractorTest.cs`, `ChangeStudentCarInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, `Filters\ApiExceptionFilterTest.cs`, `Queries\IdentifyStudentInteractorTest.cs` | Rows / tests | 2 |
| `client\public\i18n\he.json`, `en.json` | `errors.studentAlreadyWithTeacher`, `errors.studentAlreadyOnCar` (2); `students.flag.*` (3); `students.teacherChanged*`, `students.carChanged*` (4); `students.actions.changeTeacher`, `students.changeTeacher.*`, `general.close` (5); `students.actions.changeCar`, `.fix`, `students.changeCar.*`, `students.edit.teacherAndCarHint` (6) | 2-6 |
| `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`, `Queries\GetStudent\GetStudentResponse.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` | `IsCarOfTeacher` | 3 |
| `client\src\app\features\students\domain\student.model.ts`, `data\item-for-find-students.response.ts`, `data\get-student.response.ts` (if it redeclares fields), spec fixtures | `isCarOfTeacher` | 3 |
| `client\src\app\features\students\ui\pages\students\students.page.*` | Marker + tooltip (3); Change Teacher menu item (5); Change Car menu item + Fix badge (6) | 3, 5, 6 |
| `client\src\app\features\students\ui\components\student-who-card\*` | Flag icon | 3 |
| `client\src\app\features\students\domain\car-pick.ts`, `car-pick.spec.ts` | `pickCarForNewTeacher`, exported `carsOfTeacher` | 4 |
| `client\src\app\features\students\domain\car-change.ts`, `car-change.spec.ts` | **New** | 4 |
| `client\src\app\features\students\domain\student-refusal.ts` | `sameTeacher`, `sameCar` | 4 |
| `client\src\app\features\students\data\change-student-teacher.request.ts`, `change-student-car.request.ts`, `students-api.service.ts` | **New** DTOs; two API methods | 4 |
| `client\src\app\features\students\state\students.store.ts`, `.spec.ts` | `changeTeacher`, `changeCar` | 4 |
| `client\src\app\features\students\ui\dialogs\change-teacher\*` | **New**, with a spec | 5 |
| `client\src\app\features\students\ui\dialogs\change-car\*` | **New**, with a spec | 6 |
| `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.html` | `teacherAndCarHint` | 6 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-student-change-teacher-and-car.md](task-01-student-change-teacher-and-car.md) | `Student.ChangeTeacher` / `ChangeCar`, guards, events, `Car.IsAssignedTo(TeacherId)`, Submission untouched | ✅ own commit |
| 2 | [task-02-change-teacher-and-car-endpoints.md](task-02-change-teacher-and-car-endpoints.md) | Interactors, `POST change-teacher|change-car`, filter + authorization + identify tests, `errors.*`, API smoke | ✅ own commit |
| 3 | [task-03-car-not-of-teacher-flag.md](task-03-car-not-of-teacher-flag.md) | `IsCarOfTeacher` in the queries; table marker + tooltip; who card icon | ✅ own commit |
| 4 | [task-04-client-store-and-car-picks.md](task-04-client-store-and-car-picks.md) | `pickCarForNewTeacher`, `carChangeFor`, refusal kinds, API methods, store commands | ✅ own commit |
| 5 | [task-05-client-change-teacher-dialog.md](task-05-client-change-teacher-dialog.md) | Change Teacher dialog with spec, menu item | ✅ own commit |
| 6 | [task-06-client-change-car-dialog.md](task-06-client-change-car-dialog.md) | Change Car dialog with spec, menu item + Fix badge, Edit details hint | ✅ own commit |
| 7 | [task-07-verify-and-pr.md](task-07-verify-and-pr.md) | Full suites, Postgres smoke of Review Focus 2 and 4, browser verification (Hebrew RTL, English, 768px), design comparison, PR | ✅ PR |

The PR targets `82-users-and-roles`, says `Closes #95` (it won't auto-close on a non-default base; close it when the epic merges), and references the parent spec #82.
