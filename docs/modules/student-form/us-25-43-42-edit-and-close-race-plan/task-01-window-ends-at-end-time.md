# Task 1 of 7: The window ends at its end time; refused revisions keep the previous version; revise any number of times

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Work on branch `26-us-25-43-42-edit-and-close-race`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Domain\Values\SubmissionWindow.cs` (+ `HasEndedBy`)
- Modify: `src\DrivingLessons.Domain\Entities\Publication.cs` (+ `IsOpenAt`)
- Modify: `src\DrivingLessons.Domain\Entities\Submission.cs` (window guard takes the instant)
- Test: `tests\DrivingLessons.Domain.Test\Values\SubmissionWindowTest.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\PublicationTest.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\SubmissionTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ReviseSubmissionInteractorTest.cs`

**Interfaces:**
- Consumes (on `main`): `SubmissionWindow { StartUtc, EndUtc }`, `Publication.IsOpen` / `Window`, `Submission.Create(publication, student, weekSchedule, targetCount, picks, submittedAtUtc)`, `Submission.Revise(…, revisedAtUtc)`, `SubmissionWindowMustBeOpenException()`; test helpers `PublicationFakeBuilder.BuildOpen/StateBuilders`, `SubmissionFakeBuilder.BuildScenario/Build/PicksOf`, `Faker.FakeUtcInstant()` (always in the past).
- Produces:
  - `SubmissionWindow.HasEndedBy(DateTimeOffset instant) : bool`: `instant >= EndUtc`.
  - `Publication.IsOpenAt(DateTimeOffset instant) : bool`: `IsOpen && !Window.HasEndedBy(instant)`.
  - `Submission.Create/Revise` throw `SubmissionWindowMustBeOpenException` when `!publication.IsOpenAt(instant)` (README decision 6). Same exception, same message, so task 3 keys the problem type on it.

The fake windows start "now" and end three days later, and `Faker.FakeUtcInstant()` is always in the past, so every existing domain test instant is before the end. The interactor tests use `Now` against `Now + 2 days`. No existing test changes behaviour.

- [ ] **Step 1: Failing tests for `HasEndedBy` and `IsOpenAt`**

In `tests\DrivingLessons.Domain.Test\Values\SubmissionWindowTest.cs`, add after `Of__End_Must_Be_After_Start_When_Earlier` (inside the class):

```csharp
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    public void Has_Ended_By(int secondsAfterEnd, bool hasEnded)
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddDays(3);
        var window = SubmissionWindow.Of(startUtc, endUtc);
        var instant = endUtc.AddSeconds(secondsAfterEnd);

        //when
        var result = window.HasEndedBy(instant);

        //then
        result.ShouldBe(hasEnded);
    }
```

In `tests\DrivingLessons.Domain.Test\Entities\PublicationTest.cs`, add after the last `Reopen__Must_Be_Closed` test (inside the class):

```csharp
    [TestMethod]
    public void Is_Open_At__Before_The_Window_Ends()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var instant = publication.Window!.EndUtc.AddSeconds(-1);

        //when
        var result = publication.IsOpenAt(instant);

        //then
        result.ShouldBeTrue();
    }

    [TestMethod]
    public void Is_Open_At__Not_Once_The_Window_Has_Ended()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var instant = publication.Window!.EndUtc;

        //when
        var result = publication.IsOpenAt(instant);

        //then
        result.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Is_Open_At__Only_While_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var instant = DateTimeOffset.UtcNow;

        //when
        var result = publication.IsOpenAt(instant);

        //then
        result.ShouldBeFalse();
    }
```

The draft row matters: a draft has no `Window`, so `IsOpenAt` must short-circuit on `IsOpen` before it touches `Window`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test --filter "FullyQualifiedName~SubmissionWindowTest|FullyQualifiedName~PublicationTest"`
Expected: build FAILS with `CS1061: 'SubmissionWindow' does not contain a definition for 'HasEndedBy'` and the same for `'Publication' … 'IsOpenAt'`.

- [ ] **Step 3: Implement `HasEndedBy` and `IsOpenAt`**

In `src\DrivingLessons.Domain\Values\SubmissionWindow.cs`, add after the `Of` method (inside the record):

```csharp
    public bool HasEndedBy(DateTimeOffset instant)
    {
        return instant >= EndUtc;
    }
```

In `src\DrivingLessons.Domain\Entities\Publication.cs`, add after the `Reopen` method (before the `private void MustBeDraft()` guard):

```csharp
    public bool IsOpenAt(DateTimeOffset instant)
    {
        return IsOpen && !Window!.HasEndedBy(instant);
    }
```

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test --filter "FullyQualifiedName~SubmissionWindowTest|FullyQualifiedName~PublicationTest"`
Expected: PASS (3 new `Has_Ended_By` rows, 5 new `Is_Open_At…` cases, every existing test).

- [ ] **Step 5: Tests for the submission rule, the untouched previous version and repeated revisions**

In `tests\DrivingLessons.Domain.Test\Entities\SubmissionTest.cs`, add after `Create__Window_Must_Be_Open` (inside the class):

```csharp
    [TestMethod]
    public void Create__Window_Must_Not_Have_Ended()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var submittedAtUtc = scenario.Publication.Window!.EndUtc;

        //when
        var act = () => Submission.Create(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            submittedAtUtc);

        //then
        Should.Throw<SubmissionWindowMustBeOpenException>(act);
    }
```

and add after `Revise__Window_Must_Be_Open` (inside the class):

```csharp
    [TestMethod]
    public void Revise__Window_Must_Not_Have_Ended()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 3);
        var revisedAtUtc = scenario.Publication.Window!.EndUtc.AddSeconds(1);

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(3),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionWindowMustBeOpenException>(act);
    }

    [TestMethod]
    public void Revise__Rejected_Revision_Keeps_The_Previous_Version()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var previousTarget = submission.TargetCount;
        var previousSlotIds = submission.SlotRequests.Select(x => x.SlotId).ToList();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 4);
        var revisedAtUtc = scenario.Publication.Window!.EndUtc;

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(4),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionWindowMustBeOpenException>(act);
        submission.TargetCount.ShouldBe(previousTarget);
        submission.SlotRequests.Select(x => x.SlotId).ShouldBe(previousSlotIds);
        submission.RevisedAtUtc.ShouldBeNull();
        submission.UncommittedEvents.OfType<SubmissionRevised>().ShouldBeEmpty();
    }

    [TestMethod]
    public void Revise__Replaces_Every_Earlier_Version()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var slots = scenario.WeekSchedule.Slots.ToList();
        var secondPicks = new[] { slots[3], slots[8] }
                              .Select(slot => SlotPick.Of(slot, SessionType.Single, null))
                              .ToList();
        var constraint = SlotConstraint.Of(Faker.FakeString());
        var thirdPicks = new List<SlotPick> { SlotPick.Of(slots[11], SessionType.Double, constraint) };
        var secondRevisedAtUtc = Faker.FakeUtcInstant();
        var thirdRevisedAtUtc = secondRevisedAtUtc.AddMinutes(5);
        submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(2),
            secondPicks,
            secondRevisedAtUtc);

        //when
        submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            thirdPicks,
            thirdRevisedAtUtc);

        //then
        submission.TargetCount.ShouldBe(TargetSessionCount.Of(1));
        var slotRequest = submission.SlotRequests.ShouldHaveSingleItem();
        slotRequest.SlotId.ShouldBe(slots[11].Id);
        slotRequest.SessionType.ShouldBe(SessionType.Double);
        slotRequest.Constraint.ShouldBe(constraint);
        slotRequest.Rank.ShouldBe(Rank.Of(1));
        submission.RevisedAtUtc.ShouldBe(thirdRevisedAtUtc);
        submission.UncommittedEvents.OfType<SubmissionRevised>().Count().ShouldBe(2);
    }
```

`Revise__Rejected_Revision_Keeps_The_Previous_Version` refuses on the **end time** while the publication is still Open, which is the US-42 scenario ("no partial data is saved").

In `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`:

1. Promote the time provider to a field. Add next to the other fields (after `private Submission? addedSubmission;`):
   ```csharp
   private TimeProvider timeProvider = null!;
   ```
   and in `Init` change `var timeProvider = A.Fake<TimeProvider>();` to:
   ```csharp
   timeProvider = A.Fake<TimeProvider>();
   ```
2. Add after `Closed_Window_Is_Rejected` (inside the class):
   ```csharp
    [TestMethod]
    public async Task Window_That_Has_Ended_Is_Rejected_Before_The_Close_Job_Runs()
    {
        //given
        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(publication.Window!.EndUtc);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
        publication.IsOpen.ShouldBeTrue();
        A.CallTo(() => submissionRepository.Add(A<Submission>._))
            .MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }
   ```

In `tests\DrivingLessons.Application.Test\Commands\ReviseSubmissionInteractorTest.cs`:

1. Promote the time provider to a field. Add after `private Submission existing = null!;`:
   ```csharp
   private TimeProvider timeProvider = null!;
   ```
   and in `Init` change `var timeProvider = A.Fake<TimeProvider>();` to:
   ```csharp
   timeProvider = A.Fake<TimeProvider>();
   ```
2. Add after `Closed_Window_Is_Rejected` (inside the class):
   ```csharp
    [TestMethod]
    public async Task Window_That_Has_Ended_Is_Rejected_And_Keeps_The_Previous_Version()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var request = new ReviseSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[4].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[6].Id.Value, SessionType.Double, null)
            ]);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(publication.Window!.EndUtc.AddMinutes(1));

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
        existing.TargetCount.ShouldBe(TargetSessionCount.Of(1));
        existing.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[0].Id]);
        existing.RevisedAtUtc.ShouldBeNull();
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Revises_Any_Number_Of_Times()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var laterNow = Now.AddHours(3);
        var firstRevision = new ReviseSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[4].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[6].Id.Value, SessionType.Single, null)
            ]);
        var secondRevision = new ReviseSubmissionRequest(
            RosterNationalId,
            1,
            [new SlotRequestForSubmissionRequest(slots[10].Id.Value, SessionType.Double, "pick me up from work")]);

        await interactor.ExecuteAsync(publication.LinkToken.Value, firstRevision);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(laterNow);

        //when
        await interactor.ExecuteAsync(publication.LinkToken.Value, secondRevision);

        //then
        existing.TargetCount.ShouldBe(TargetSessionCount.Of(1));
        existing.SlotRequests.ShouldHaveSingleItem().SlotId.ShouldBe(slots[10].Id);
        existing.RevisedAtUtc.ShouldBe(laterNow);
        existing.SubmittedAtUtc.ShouldBe(SubmittedAtUtc);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedTwiceExactly();
    }
   ```

`existing` was created in `Init` with `weekSchedule.Slots.First()`, so `slots[0]` is its only slot request.

- [ ] **Step 6: Run them to see the window-end tests fail**

Run: `dotnet test`
Expected: FAIL on exactly five tests, all with "no exception was thrown": `Create__Window_Must_Not_Have_Ended`, `Revise__Window_Must_Not_Have_Ended`, `Revise__Rejected_Revision_Keeps_The_Previous_Version`, `Window_That_Has_Ended_Is_Rejected_Before_The_Close_Job_Runs` and `Window_That_Has_Ended_Is_Rejected_And_Keeps_The_Previous_Version`. `Revise__Replaces_Every_Earlier_Version` and `Revises_Any_Number_Of_Times` already PASS: they pin behaviour that exists (US-43). If they fail, stop and investigate before touching `Submission`.

- [ ] **Step 7: The submission guard takes the instant**

In `src\DrivingLessons.Domain\Entities\Submission.cs`:

1. In `Create`, replace
   ```csharp
        WindowMustBeOpen(publication);
        StudentMustBeActive(student);
   ```
   (the first occurrence, in `Create`) with
   ```csharp
        WindowMustBeOpen(publication, submittedAtUtc);
        StudentMustBeActive(student);
   ```
2. In `Revise`, replace
   ```csharp
        MustBeFor(publication, student);
        WindowMustBeOpen(publication);
   ```
   with
   ```csharp
        MustBeFor(publication, student);
        WindowMustBeOpen(publication, revisedAtUtc);
   ```
3. Replace the whole `WindowMustBeOpen` method with:
   ```csharp
    private static void WindowMustBeOpen(Publication publication, DateTimeOffset instant)
    {
        if (!publication.IsOpenAt(instant))
        {
            throw new SubmissionWindowMustBeOpenException();
        }
    }
   ```

The guard order is unchanged: every check still runs before `Revise` assigns anything, which is what `Revise__Rejected_Revision_Keeps_The_Previous_Version` pins.

- [ ] **Step 8: Run everything**

Run: `dotnet build` then `dotnet test`
Expected: build clean with no new warnings, every test PASS. That includes the existing `Create__Window_Must_Be_Open` rows (Draft, Published, Closed) and both `Closed_Window_Is_Rejected` interactor tests, which still throw the same exception.

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Domain/Values/SubmissionWindow.cs src/DrivingLessons.Domain/Entities/Publication.cs src/DrivingLessons.Domain/Entities/Submission.cs tests/DrivingLessons.Domain.Test/Values/SubmissionWindowTest.cs tests/DrivingLessons.Domain.Test/Entities/PublicationTest.cs tests/DrivingLessons.Domain.Test/Entities/SubmissionTest.cs tests/DrivingLessons.Application.Test/Commands/CreateSubmissionInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ReviseSubmissionInteractorTest.cs
git commit -m "feat(domain): the submission window ends at its end time

A submission or revision at or after the window's end is refused even
while the close job has not yet flipped the publication to Closed. A
refused revision leaves the earlier version untouched, and a submission
can be revised any number of times while the window is open."
```

---

**Next:** [task-02-identify-returns-submission.md](task-02-identify-returns-submission.md)
