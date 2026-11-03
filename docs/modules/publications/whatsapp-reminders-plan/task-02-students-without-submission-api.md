# Task 2: Students without a Submission - the API

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README's Current State and Decisions 7 to 9 first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** Task 1 committed.
**Scope guard:**
- Create: `src\DrivingLessons.Application\Queries\FindStudentsWithoutSubmission\ItemForFindStudentsWithoutSubmissionResponse.cs`, `FindStudentsWithoutSubmissionInteractor.cs`, `FindSchoolStudentsWithoutSubmissionInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IStudentQueries.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`, `src\DrivingLessons.Application\DependencyInjection.cs` (next to `FindStudentsInteractor`)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs`
- Rename and modify: `PublicationSchoolExcelQueryController.cs` → `PublicationSchoolQueryController.cs` (same folder; `git mv`)
- Test: `tests\DrivingLessons.Application.Test\Queries\FindStudentsWithoutSubmissionInteractorTest.cs`, `FindSchoolStudentsWithoutSubmissionInteractorTest.cs` (new), `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`

**Interfaces:**
- Consumes: `PhoneNumber.ToWhatsAppNumber(): WhatsAppNumber?` (Task 1), `ICurrentUser`, `CurrentUserExtension.MayReach(TeacherId)`, `PublicationNotFoundException(PublicationId)`.
- Produces:
  - `ItemForFindStudentsWithoutSubmissionResponse` with `{ get; init; }` properties: `Guid StudentId`, `string StudentName = string.Empty`, `Guid TeacherId`, `string TeacherName = string.Empty`, `string Phone = string.Empty`, `string? WhatsAppNumber`.
  - `IStudentQueries.FindWithoutSubmissionAsync(Guid publicationId, Guid? teacherId): Task<IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>?>`. It returns `null` when the Publication doesn't exist.
  - `GET api/publications/{id:guid}/students-without-submission?teacherId={guid}`: 200, 401, or 404 (another Teacher's list, or no Publication).
  - `GET api/publications/{id:guid}/school-students-without-submission`: 200, 401, 403 for a Teacher-role User, or 404.
  - Both answer a JSON array of `{ studentId, studentName, teacherId, teacherName, phone, whatsAppNumber }`.

- [ ] **Step 2.1: Write the failing interactor tests**

Set them up like `FindSlotRequestsInteractorTest`: fakes for `IStudentQueries` and `ICurrentUser`, a `SignedInAs(Role role, TeacherId? linkedTeacherId)` helper, `publicationId = Guid.NewGuid()`, and `ownTeacherId` / `otherTeacherId = TeacherId.New()`. The fake returns two items, Noa and Omer.

`FindStudentsWithoutSubmissionInteractorTest`:

```csharp
[TestMethod]
public async Task Teacher_Sees_Own_Students_Without_Submission()
// SignedInAs(Role.Teacher, ownTeacherId); result = ExecuteAsync(publicationId, ownTeacherId.Value)
// result.Select(x => x.StudentName).ShouldBe(["Noa", "Omer"]);
// A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, ownTeacherId.Value)).MustHaveHappenedOnceExactly();

[TestMethod]
public async Task Other_Teachers_List_Is_Not_Found()
// SignedInAs(Role.Teacher, ownTeacherId); ExecuteAsync(publicationId, otherTeacherId.Value) → PublicationNotFoundException;
// A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(A<Guid>._, A<Guid?>._)).MustNotHaveHappened();

[TestMethod]
public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_List()
// SignedInAs(Role.Teacher, null) → PublicationNotFoundException, no query call

[TestMethod]
public async Task Administrator_Sees_Any_Teachers_List()
// SignedInAs(Role.Administrator, null); ExecuteAsync(publicationId, otherTeacherId.Value) returns the 2 items

[TestMethod]
public async Task Publication_Must_Exist()
// SignedInAs(Role.Administrator, null); query returns null → PublicationNotFoundException
```

`FindSchoolStudentsWithoutSubmissionInteractorTest`:

```csharp
[TestMethod]
public async Task Lists_Every_Teachers_Students_Without_Submission()
// ExecuteAsync(publicationId) returns the 2 items;
// A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, null)).MustHaveHappenedOnceExactly();

[TestMethod]
public async Task Publication_Must_Exist()
// query returns null → PublicationNotFoundException
```

- [ ] **Step 2.2: Run them to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test --filter "FullyQualifiedName~StudentsWithoutSubmission"`
Expected: the build fails, because the interactors and `FindWithoutSubmissionAsync` don't exist.

- [ ] **Step 2.3: Implement the query**

`StudentQueries.FindWithoutSubmissionAsync`:
1. Read the Publication's `WeekStart` (`Where(x => x.Id == resolvedPublicationId).Select(x => x.WeekStart).FirstOrDefaultAsync()`). If there is no Publication, return `null`.
2. Query:
   - from active Students (`IsActive`), filtered to `teacherId` when it has a value;
   - joined to `dbContext.WeekSchedules` on `TeacherId` with the same `WeekStart`;
   - joined to `dbContext.Teachers` on `TeacherId` (the default soft-delete filter stays on: a deleted Teacher has no active Students);
   - where no `dbContext.Submissions` row has this `PublicationId` and this `StudentId`, whatever its Week Schedule (intent §4.1: a Student who submitted and then had Change Teacher applied isn't listed);
   - ordered by Teacher name, then Student name, then Student id;
   - selecting the Student id, name and `Phone`, and the Teacher id and name.
3. After `ToListAsync()`, map to the response in memory. `WhatsAppNumber = x.Phone.ToWhatsAppNumber()?.Value`, because the conversion doesn't translate to SQL. Unwrap value objects as `FindSlotRequestsAsync` does.

Extract filter values into locals before the LINQ expression (`ddd-architecture.md`).

- [ ] **Step 2.4: Implement the interactors, DI and endpoints**

- **`FindStudentsWithoutSubmissionInteractor(IStudentQueries, ICurrentUser)`:**
  - If `!currentUser.MayReach(TeacherId.Of(teacherId))`, throw `PublicationNotFoundException(PublicationId.Of(publicationId))`.
  - Otherwise return `await FindWithoutSubmissionAsync(publicationId, teacherId)`, or throw the same exception on `null`.
- **`FindSchoolStudentsWithoutSubmissionInteractor(IStudentQueries)`:** the query with `teacherId: null`, `?? throw` as above.
- **DI:** `services.AddScoped<...>()` for both.
- **`PublicationQueryController.FindStudentsWithoutSubmission`:**
  - `[HttpGet("{id:guid}/students-without-submission")]`
  - `[EndpointSummary("Lists the teacher's active students who have not submitted for a publication")]`
  - `ProducesResponseType` 200 (`IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>`), 401, 404
  - `[FromRoute] Guid id, [FromQuery] Guid teacherId`
- **`git mv` the school controller to `PublicationSchoolQueryController.cs`,** and rename the class to match. Add `FindSchoolStudentsWithoutSubmission`:
  - `[HttpGet("{id:guid}/school-students-without-submission")]`
  - `[EndpointSummary("Lists every teacher's active students who have not submitted for a publication")]`
  - 200, 401, 403, 404
- **`ControllerAuthorizationTest`:**
  - add `"PublicationQueryController.FindStudentsWithoutSubmission"` to `TeacherOrAdministratorEndpoints`;
  - rename the `PublicationSchoolExcelQueryController.DownloadSchoolExcel` data row to `PublicationSchoolQueryController.DownloadSchoolExcel`;
  - add `[DataRow("PublicationSchoolQueryController.FindSchoolStudentsWithoutSubmission")]` to `Week_Schedule_Creation_And_The_Publication_Lifecycle_Stay_Administrator_Only`.

- [ ] **Step 2.5: Run the tests**

Run: `dotnet test`
Expected: all pass. Application gains 7 interactor tests and 1 authorization data row; nothing else changes compared with Task 1.

- [ ] **Step 2.6: Smoke both endpoints against the dev database**

There are no database-level query tests, so this step proves the EF query translates and filters correctly.
1. Start the API (`preview_start {name: "api"}`), against the compose Postgres. Sign in as the dev Administrator with `POST api/auth/login`, its body in a file. Pass `Authorization: Bearer <token>`.
2. Pick an `Open` Publication. If none exists, publish the current dev week with a window that includes now, through the dashboard or `POST api/publications/{id}/publish`.
3. Call `GET api/publications/{id}/school-students-without-submission`. Compare it with `psql` on `drivinglessonsbooking-postgres-1`: active Students whose Teacher has a `week_schedules` row for the Publication's week, minus those with a `submissions` row for this Publication. Expected: the same Students, ordered by Teacher name then Student name. Every Israeli mobile has `whatsAppNumber` starting `9725`; other phones have `null`.
4. Call `GET api/publications/{id}/students-without-submission?teacherId=<one teacher>`. Expected: exactly that Teacher's rows from step 3.
5. Submit for one listed Student through the student form link (`/s/{linkToken}`, their national ID). Expected: they are gone from both lists.
6. Change Teacher for a Student who has a Submission in this Publication (Students screen, or `POST api/students/{id}/change-teacher` with the body in a file). Expected: listed under neither Teacher.
7. A random Publication id gives 404 on both endpoints.

Undo the dev data changes from steps 5 and 6 only if they get in the way of later tasks. They are dev data.

- [ ] **Step 2.7: Commit**

```bash
git add src/DrivingLessons.Application src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs src/DrivingLessons.Presentation.Web/Controllers/Publication tests/DrivingLessons.Application.Test
git commit -m "feat(publications): list the active students who haven't submitted, per teacher and school-wide"
```
