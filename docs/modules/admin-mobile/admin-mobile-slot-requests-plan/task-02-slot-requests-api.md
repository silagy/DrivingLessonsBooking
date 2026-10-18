# Task 2: Who requested a slot - the API

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 2 to 5 first.

**Files:**
- Create: `src\DrivingLessons.Application\Queries\FindSlotRequests\ItemForFindSlotRequestsResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\FindSlotRequests\FindSlotRequestsInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (next to `GetPublicationDashboardInteractor`, line ~100)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\FindSlotRequestsInteractorTest.cs`

**Interfaces:**
- Consumes: `ICurrentUser`, `CurrentUserExtension.MayReach(TeacherId)`, `PublicationNotFoundException(PublicationId)`, and the private `SubmissionQueries.TeacherSubmissions(Guid publicationId, Guid teacherId)`.
- Produces:
  - `public class ItemForFindSlotRequestsResponse { Guid StudentId; string StudentName; int Rank; int PickCount; }` (`{ get; init; }`, `StudentName = string.Empty`, as `ItemForFindPublicationHistoryResponse`).
  - `ISubmissionQueries.FindSlotRequestsAsync(Guid publicationId, Guid teacherId, Guid slotId): Task<IReadOnlyList<ItemForFindSlotRequestsResponse>>`.
  - `FindSlotRequestsInteractor.ExecuteAsync(Guid publicationId, Guid teacherId, Guid slotId): Task<IReadOnlyList<ItemForFindSlotRequestsResponse>>`.
  - `GET api/publications/{id:guid}/slots/{slotId:guid}/requests?teacherId={guid}`. It answers 200 with a JSON array of `{ studentId, studentName, rank, pickCount }`, 401 when signed out, and 404 for another Teacher's slot.

- [ ] **Step 1: Write the failing tests**

`FindSlotRequestsInteractorTest`, set up like `GetPublicationDashboardInteractorTest`:
- fakes for `ISubmissionQueries` and `ICurrentUser`;
- a `SignedInAs(Role role, TeacherId? linkedTeacherId)` helper;
- `publicationId`, `slotId = Guid.NewGuid()`, and `ownTeacherId` / `otherTeacherId = TeacherId.New()`;
- `FindSlotRequestsAsync(publicationId, A<Guid>._, slotId)` returning two items: `{ StudentName = "Noa", Rank = 1, PickCount = 3 }`, `{ StudentName = "Omer", Rank = 2, PickCount = 2 }`.

```csharp
[TestMethod]
public async Task Teacher_Sees_Who_Requested_Own_Slot()
// SignedInAs(Role.Teacher, ownTeacherId); result = ExecuteAsync(publicationId, ownTeacherId.Value, slotId)
// result.Select(x => x.StudentName).ShouldBe(["Noa", "Omer"]); result[0].Rank.ShouldBe(1); result[0].PickCount.ShouldBe(3)

[TestMethod]
public async Task Other_Teachers_Slot_Is_Not_Found()
// SignedInAs(Role.Teacher, ownTeacherId); act = ExecuteAsync(publicationId, otherTeacherId.Value, slotId)
// await Should.ThrowAsync<PublicationNotFoundException>(act);
// A.CallTo(() => submissionQueries.FindSlotRequestsAsync(A<Guid>._, A<Guid>._, A<Guid>._)).MustNotHaveHappened();

[TestMethod]
public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Slot()
// SignedInAs(Role.Teacher, null) → PublicationNotFoundException, no query call

[TestMethod]
public async Task Administrator_Sees_Who_Requested_Any_Teachers_Slot()
// SignedInAs(Role.Administrator, null); ExecuteAsync(publicationId, otherTeacherId.Value, slotId) returns the 2 items;
// A.CallTo(() => submissionQueries.FindSlotRequestsAsync(publicationId, otherTeacherId.Value, slotId)).MustHaveHappenedOnceExactly();
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~FindSlotRequestsInteractorTest`
Expected: the build fails, because `FindSlotRequestsInteractor` and `FindSlotRequestsAsync` don't exist.

- [ ] **Step 3: Implement the query, the interactor, DI and the endpoint**

- **Response:** `ItemForFindSlotRequestsResponse` in namespace `DrivingLessons.Application.Queries.FindSlotRequests`. `ISubmissionQueries` imports it.
- **`SubmissionQueries.FindSlotRequestsAsync`:**
  - Start from `TeacherSubmissions(publicationId, teacherId)` and join `dbContext.Students` on `StudentId`.
  - Use `from request in submission.SlotRequests where request.SlotId == SlotId.Of(slotId)`.
  - `PickCount = submission.SlotRequests.Count()`.
  - Order by `submission.SubmittedAtUtc`, then the Student's name, then the Student id.
  - Project to the response, unwrapping value objects the way `GetSlotRequestDetailsAsync` does.
  - Don't filter `IsActive` or the slot state (decision 3).
- **`FindSlotRequestsInteractor`:**
  - Constructor takes `ISubmissionQueries` and `ICurrentUser`.
  - If `!currentUser.MayReach(TeacherId.Of(teacherId))`, throw `new PublicationNotFoundException(PublicationId.Of(publicationId))`.
  - Otherwise return the query's list.
- **DI:** `services.AddScoped<FindSlotRequestsInteractor>();`
- **Controller action** on `PublicationQueryController`:
  - `[HttpGet("{id:guid}/slots/{slotId:guid}/requests")]`
  - `[EndpointSummary("Lists the students who requested a slot of the teacher's week, in submission order")]`
  - the three `ProducesResponseType`s (200 with `IReadOnlyList<ItemForFindSlotRequestsResponse>`, 401, 404)
  - signature `FindSlotRequests([FromServices] FindSlotRequestsInteractor interactor, [FromRoute] Guid id, [FromRoute] Guid slotId, [FromQuery] Guid teacherId)`, delegating to the interactor.

- [ ] **Step 4: Run the tests**

Run: `dotnet test --filter FullyQualifiedName~FindSlotRequestsInteractorTest`, then `dotnet test`.
Expected: all PASS.

- [ ] **Step 5: Smoke the endpoint against the dev database**

There are no database-level query tests, so this step is the check that the EF query translates and matches the count.

1. Start the API (`preview_start {name: "api"}`) and sign in as the dev Administrator: `POST api/auth/login` with the body in a file, then pass `Authorization: Bearer <token>`.
2. Write request bodies to files (memory: Hebrew in curl argv breaks).
3. Take an Open or Closed week's `GET api/publications/{id}/dashboard?teacherId=` and pick a slot with `requestCount` ≥ 2.
4. Call `GET api/publications/{id}/slots/{slotId}/requests?teacherId=`.

Expected:
- 200;
- the array length equals that `requestCount`;
- every `rank` is between 1 and its `pickCount`;
- the order matches the Submissions' `submitted_at_utc` (check with `psql` on the compose Postgres: `select s.submitted_at_utc, st.name from submissions s join slot_requests r on r.submission_id = s.id join students st on st.id = s.student_id where r.slot_id = '<slotId>' order by 1`).
- A random `slotId` returns `[]`.

If the dev data has no requests, submit two picks through the student form link first.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat(publications): list the students who requested a slot, in submission order"
```
