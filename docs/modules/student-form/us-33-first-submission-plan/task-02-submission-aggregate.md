# Task 2 of 11: `Submission` aggregate + `SlotRequest` child (TDD)

> Part of [US-33…US-41: First Submission](README.md). Requires task 1 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\Submission.cs`, `SlotRequest.cs`
- Create: `src\DrivingLessons.Domain\Events\SubmissionCreated.cs`, `SubmissionRevised.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\SubmissionWindowMustBeOpenException.cs`, `SubmissionStudentMustBeActiveException.cs`, `SubmissionWeekScheduleMustMatchStudentWeekException.cs`, `SubmissionSlotMustBeInWeekScheduleException.cs`, `SubmissionSlotMustBeOpenException.cs`, `SubmissionSlotMustBeRequestedOnceException.cs`, `SubmissionPicksMustCoverTargetException.cs`, `SubmissionMustBeForPublicationAndStudentException.cs`
- Create (test): `tests\DrivingLessons.Domain.Test\Entities\Fake\SubmissionScenario.cs`, `Fake\SubmissionFakeBuilder.cs`
- Modify (test): `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (`FakeUtcInstant`)
- Test: `tests\DrivingLessons.Domain.Test\Entities\SubmissionTest.cs`

**Interfaces:**
- Consumes (task 1): `SubmissionId`, `SlotRequestId`, `TargetSessionCount.IsCoveredBy`, `SessionType`, `SlotConstraint`, `Rank`, `SlotPick`. (On `main`): `Publication.IsOpen` / `WeekStart` / `Close(IEnumerable<TeacherId>)`, `Student.IsActive` / `TeacherId` / `Deactivate()`, `WeekSchedule.Create(Teacher, WeekStart)` / `Slots` / `MarkSlotUnavailable(Slot)`, `PublicationFakeBuilder.StateBuilders` / `BuildOpen()`, `StudentFakeBuilder.WithTeacher(...).Build()` / `BuildInactive()`, `TeacherFakeBuilder.Build()`, `WeekScheduleFakeBuilder.Build()`.
- Produces (tasks 3–6 rely on these exact names):
  - `Submission.Create(Publication publication, Student student, WeekSchedule weekSchedule, TargetSessionCount targetCount, IReadOnlyList<SlotPick> picks, DateTimeOffset submittedAtUtc) : Submission`
  - `submission.Revise(Publication publication, Student student, WeekSchedule weekSchedule, TargetSessionCount targetCount, IReadOnlyList<SlotPick> picks, DateTimeOffset revisedAtUtc) : void` — full replace (roadmap decision 3).
  - Properties: `PublicationId`, `StudentId`, `WeekScheduleId`, `TargetCount`, `SubmittedAtUtc`, `RevisedAtUtc?`, `IReadOnlyCollection<SlotRequest> SlotRequests` (in rank order); backing field `slotRequests`.
  - `SlotRequest { SlotRequestId Id; SlotId SlotId; SessionType SessionType; SlotConstraint? Constraint; Rank Rank; }`
  - Events `SubmissionCreated(SubmissionId, PublicationId, StudentId, WeekScheduleId, TargetSessionCount TargetCount, DateTimeOffset SubmittedAtUtc)`, `SubmissionRevised(SubmissionId, WeekScheduleId, TargetSessionCount TargetCount, DateTimeOffset RevisedAtUtc)`.
  - Eight payload-free `DomainException`s (409) listed under **Files** — none carries an id, a national ID or constraint text (they reach anonymous callers as ProblemDetails `detail`).

Precedents to open before coding: `Domain\Entities\WeekSchedule.cs` (aggregate with an owned collection, created event in the constructor, `MustOwnSlot`), `Domain\Entities\TeacherExcelVersion.cs` (child entity with `internal` factory), `Domain\Entities\Publication.cs` (instant parameters as `DateTimeOffset`), `tests\DrivingLessons.Domain.Test\Entities\WeekScheduleTest.cs` and `PublicationTest.cs` (test shape, `StateBuilders` DataRows).

**The rules this aggregate owns** (requirements §5.6, §5.7, §7 validation; issues #33–#40):

| Rule | Guard | Exception |
|------|-------|-----------|
| Window must be open (carried over from slice 2's open item 1) | `WindowMustBeOpen` | `SubmissionWindowMustBeOpenException` |
| Only active roster students submit (ADR 0003, decision #19) — the commands only load active students (task 3), so this is the aggregate protecting itself | `StudentMustBeActive` | `SubmissionStudentMustBeActiveException` |
| The grid is the student's own teacher's, for the publication's week (US-51 routing, never another teacher's grid) | `WeekScheduleMustMatchStudentWeek` | `SubmissionWeekScheduleMustMatchStudentWeekException` |
| Every pick is a slot of that grid | `SlotsMustBeOpenInWeekSchedule` | `SubmissionSlotMustBeInWeekScheduleException` |
| Unavailable slots are unselectable (US-34) | `SlotsMustBeOpenInWeekSchedule` | `SubmissionSlotMustBeOpenException` |
| A slot at most once per submission (US-40) | `SlotsMustBeRequestedOnce` | `SubmissionSlotMustBeRequestedOnceException` |
| Picks ≥ target (US-40; extra picks are backups, US-39 — roadmap decision 8) | `PicksMustCoverTarget` | `SubmissionPicksMustCoverTargetException` |
| Revise acts on its own publication and student | `MustBeFor` | `SubmissionMustBeForPublicationAndStudentException` |

Target ≥ 1 is already guaranteed by the `TargetSessionCount` type (task 1). Ranks are positions in the pick list, assigned 1…n in selection order (US-37) — there is no primary/alternative type (decision 4). `Revise` is a full replace (roadmap decision 3): the add/remove helpers stay private, and re-sending the same list is allowed (revising is not a state transition, so "not idempotent" does not apply). There is **no "already exists" rule here**: whether a submission exists is the application's question (task 4), like `WeekScheduleAlreadyExistsException` in `CreateWeekScheduleInteractor`.

- [ ] **Step 1: Test helpers — `Faker.FakeUtcInstant`, scenario and fake builder**

In `tests\DrivingLessons.Domain.Test\Common\Faker.cs`, add after `FakeUtcDate()`:

```csharp

    public static DateTimeOffset FakeUtcInstant()
    {
        var minutesAgo = Random.Shared.Next(1, 100000);

        return DateTimeOffset.UtcNow.AddMinutes(-minutesAgo);
    }
```

`tests\DrivingLessons.Domain.Test\Entities\Fake\SubmissionScenario.cs`:

```csharp
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public record SubmissionScenario(Publication Publication, Teacher Teacher, Student Student, WeekSchedule WeekSchedule);
```

`tests\DrivingLessons.Domain.Test\Entities\Fake\SubmissionFakeBuilder.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class SubmissionFakeBuilder
{
    public static SubmissionScenario BuildScenario()
    {
        var publication = PublicationFakeBuilder.BuildOpen();

        return BuildScenarioFor(publication);
    }

    public static SubmissionScenario BuildScenarioFor(Publication publication)
    {
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var weekSchedule = WeekSchedule.Create(teacher, publication.WeekStart);

        return new SubmissionScenario(publication, teacher, student, weekSchedule);
    }

    public static IReadOnlyList<SlotPick> PicksOf(WeekSchedule weekSchedule, int count)
    {
        return weekSchedule
                   .Slots
                   .Take(count)
                   .Select(slot => SlotPick.Of(slot, SessionType.Single, null))
                   .ToList();
    }

    public static Submission Build(
        SubmissionScenario scenario,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks)
    {
        var submittedAtUtc = Faker.FakeUtcInstant();

        return Submission.Create(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            submittedAtUtc);
    }

    public static Submission Build(SubmissionScenario scenario)
    {
        var targetCount = TargetSessionCount.Of(1);
        var picks = PicksOf(scenario.WeekSchedule, 2);

        return Build(scenario, targetCount, picks);
    }
}
```

A "scenario" is the only valid starting point for a submission: an **open** publication, an **active** student, and **that student's teacher's** week schedule for **that publication's week**. Each guard test breaks exactly one of those facts (`scenario with { … }`). There is no state enum on `Submission`, so there is no `StateBuilders` dictionary.

- [ ] **Step 2: Write the failing aggregate tests**

`tests\DrivingLessons.Domain.Test\Entities\SubmissionTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class SubmissionTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var targetCount = TargetSessionCount.Of(2);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 2);
        var submittedAtUtc = Faker.FakeUtcInstant();

        //when
        var submission = Submission.Create(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            submittedAtUtc);

        //then
        submission.PublicationId.ShouldBe(scenario.Publication.Id);
        submission.StudentId.ShouldBe(scenario.Student.Id);
        submission.WeekScheduleId.ShouldBe(scenario.WeekSchedule.Id);
        submission.TargetCount.ShouldBe(targetCount);
        submission.SubmittedAtUtc.ShouldBe(submittedAtUtc);
        submission.RevisedAtUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Create__Ranks_Slot_Requests_In_Selection_Order()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var slots = scenario.WeekSchedule.Slots.ToList();
        var picks = new[] { slots[6], slots[0], slots[13] }
                        .Select(slot => SlotPick.Of(slot, SessionType.Single, null))
                        .ToList();

        //when
        var submission = SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(1), picks);

        //then
        submission.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[6].Id, slots[0].Id, slots[13].Id]);
        submission.SlotRequests.Select(x => x.Rank).ShouldBe([Rank.Of(1), Rank.Of(2), Rank.Of(3)]);
    }

    [TestMethod]
    public void Create__Keeps_Session_Type_And_Constraint_Per_Slot_Request()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var slots = scenario.WeekSchedule.Slots.ToList();
        var constraint = SlotConstraint.Of(Faker.FakeString());
        var picks = new List<SlotPick>
        {
            SlotPick.Of(slots[2], SessionType.Double, constraint),
            SlotPick.Of(slots[7], SessionType.Single, null)
        };

        //when
        var submission = SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(2), picks);

        //then
        var doubleRequest = submission.SlotRequests.First(x => x.SlotId == slots[2].Id);
        var singleRequest = submission.SlotRequests.First(x => x.SlotId == slots[7].Id);
        doubleRequest.SessionType.ShouldBe(SessionType.Double);
        doubleRequest.Constraint.ShouldBe(constraint);
        singleRequest.SessionType.ShouldBe(SessionType.Single);
        singleRequest.Constraint.ShouldBeNull();
    }

    [TestMethod]
    public void Create__Accepts_Extra_Picks_As_Backups()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var targetCount = TargetSessionCount.Of(2);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 5);

        //when
        var submission = SubmissionFakeBuilder.Build(scenario, targetCount, picks);

        //then
        submission.TargetCount.ShouldBe(targetCount);
        submission.SlotRequests.Select(x => x.Rank.Value).ShouldBe([1, 2, 3, 4, 5]);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var targetCount = TargetSessionCount.Of(1);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var submittedAtUtc = Faker.FakeUtcInstant();

        //when
        var submission = Submission.Create(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            submittedAtUtc);

        //then
        submission.UncommittedEvents
                  .OfType<SubmissionCreated>()
                  .Where(x => x.SubmissionId == submission.Id)
                  .Where(x => x.PublicationId == scenario.Publication.Id)
                  .Where(x => x.StudentId == scenario.Student.Id)
                  .Where(x => x.WeekScheduleId == scenario.WeekSchedule.Id)
                  .Where(x => x.TargetCount == targetCount)
                  .Where(x => x.SubmittedAtUtc == submittedAtUtc)
                  .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Create__Window_Must_Be_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var scenario = SubmissionFakeBuilder.BuildScenarioFor(publication);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);

        //when
        var act = () => SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionWindowMustBeOpenException>(act);
    }

    [TestMethod]
    public void Create__Student_Must_Be_Active()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var inactiveStudent = new StudentFakeBuilder().WithTeacher(scenario.Teacher).BuildInactive();
        var deactivated = scenario with { Student = inactiveStudent };
        var picks = SubmissionFakeBuilder.PicksOf(deactivated.WeekSchedule, 1);

        //when
        var act = () => SubmissionFakeBuilder.Build(deactivated, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionStudentMustBeActiveException>(act);
    }

    [TestMethod]
    public void Create__Week_Schedule_Must_Be_The_Students_Teachers()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var otherTeacher = TeacherFakeBuilder.Build();
        var otherTeachersWeek = WeekSchedule.Create(otherTeacher, scenario.Publication.WeekStart);
        var mismatched = scenario with { WeekSchedule = otherTeachersWeek };
        var picks = SubmissionFakeBuilder.PicksOf(otherTeachersWeek, 1);

        //when
        var act = () => SubmissionFakeBuilder.Build(mismatched, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionWeekScheduleMustMatchStudentWeekException>(act);
    }

    [TestMethod]
    public void Create__Week_Schedule_Must_Be_For_The_Publication_Week()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var nextSunday = scenario.Publication.WeekStart.Value.AddDays(7);
        var nextWeek = WeekSchedule.Create(scenario.Teacher, WeekStart.Of(nextSunday));
        var mismatched = scenario with { WeekSchedule = nextWeek };
        var picks = SubmissionFakeBuilder.PicksOf(nextWeek, 1);

        //when
        var act = () => SubmissionFakeBuilder.Build(mismatched, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionWeekScheduleMustMatchStudentWeekException>(act);
    }

    [TestMethod]
    public void Create__Slot_Must_Be_In_The_Week_Schedule()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var otherTeachersSlot = WeekScheduleFakeBuilder.Build().Slots.First();
        var picks = new List<SlotPick> { SlotPick.Of(otherTeachersSlot, SessionType.Single, null) };

        //when
        var act = () => SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionSlotMustBeInWeekScheduleException>(act);
    }

    [TestMethod]
    public void Create__Slot_Must_Be_Open()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var slot = scenario.WeekSchedule.Slots.First();
        scenario.WeekSchedule.MarkSlotUnavailable(slot);
        var picks = new List<SlotPick> { SlotPick.Of(slot, SessionType.Single, null) };

        //when
        var act = () => SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionSlotMustBeOpenException>(act);
    }

    [TestMethod]
    public void Create__Slot_Must_Be_Requested_Once()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var slot = scenario.WeekSchedule.Slots.First();
        var picks = new List<SlotPick>
        {
            SlotPick.Of(slot, SessionType.Single, null),
            SlotPick.Of(slot, SessionType.Double, null)
        };

        //when
        var act = () => SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(1), picks);

        //then
        Should.Throw<SubmissionSlotMustBeRequestedOnceException>(act);
    }

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(3, 2)]
    [DataRow(10, 3)]
    public void Create__Picks_Must_Cover_Target(int target, int pickCount)
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, pickCount);

        //when
        var act = () => SubmissionFakeBuilder.Build(scenario, TargetSessionCount.Of(target), picks);

        //then
        Should.Throw<SubmissionPicksMustCoverTargetException>(act);
    }

    [TestMethod]
    public void Revise()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var submittedAtUtc = submission.SubmittedAtUtc;
        var slots = scenario.WeekSchedule.Slots.ToList();
        var targetCount = TargetSessionCount.Of(3);
        var picks = new[] { slots[9], slots[4], slots[1] }
                        .Select(slot => SlotPick.Of(slot, SessionType.Double, null))
                        .ToList();
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            revisedAtUtc);

        //then
        submission.TargetCount.ShouldBe(targetCount);
        submission.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[9].Id, slots[4].Id, slots[1].Id]);
        submission.SlotRequests.Select(x => x.Rank.Value).ShouldBe([1, 2, 3]);
        submission.SlotRequests.ShouldAllBe(x => x.SessionType == SessionType.Double);
        submission.RevisedAtUtc.ShouldBe(revisedAtUtc);
        submission.SubmittedAtUtc.ShouldBe(submittedAtUtc);
    }

    [TestMethod]
    public void Revise__Add_Event()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var targetCount = TargetSessionCount.Of(1);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 3);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            revisedAtUtc);

        //then
        submission.UncommittedEvents
                  .OfType<SubmissionRevised>()
                  .Where(x => x.SubmissionId == submission.Id)
                  .Where(x => x.WeekScheduleId == scenario.WeekSchedule.Id)
                  .Where(x => x.TargetCount == targetCount)
                  .Where(x => x.RevisedAtUtc == revisedAtUtc)
                  .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Revise__Window_Must_Be_Open()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        scenario.Publication.Close([scenario.Teacher.Id]);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionWindowMustBeOpenException>(act);
    }

    [TestMethod]
    public void Revise__Must_Be_For_Its_Publication_And_Student()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var classmate = new StudentFakeBuilder().WithTeacher(scenario.Teacher).Build();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            classmate,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionMustBeForPublicationAndStudentException>(act);
    }

    [TestMethod]
    public void Revise__Student_Must_Be_Active()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        scenario.Student.Deactivate();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionStudentMustBeActiveException>(act);
    }

    [TestMethod]
    public void Revise__Picks_Must_Cover_Target()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 3);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(4),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionPicksMustCoverTargetException>(act);
    }

    [TestMethod]
    public void Revise__Slot_Must_Be_Requested_Once()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var slot = scenario.WeekSchedule.Slots.Last();
        var picks = new List<SlotPick>
        {
            SlotPick.Of(slot, SessionType.Single, null),
            SlotPick.Of(slot, SessionType.Single, null)
        };
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionSlotMustBeRequestedOnceException>(act);
    }

    [TestMethod]
    public void Revise__Moves_To_The_Students_Current_Teachers_Week_Schedule()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var student = scenario.Student;
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        student.UpdateFromRoster(
            student.Name,
            student.Phone,
            newTeacher,
            newCar,
            student.Address,
            student.StartDate,
            student.LicenseType);
        var newTeachersWeek = WeekSchedule.Create(newTeacher, scenario.Publication.WeekStart);
        var picks = SubmissionFakeBuilder.PicksOf(newTeachersWeek, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        submission.Revise(
            scenario.Publication,
            student,
            newTeachersWeek,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        submission.WeekScheduleId.ShouldBe(newTeachersWeek.Id);
    }

    [TestMethod]
    public void Revise__Must_Be_For_Its_Publication()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var otherPublication = PublicationFakeBuilder.BuildOpen();
        var picks = SubmissionFakeBuilder.PicksOf(scenario.WeekSchedule, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            otherPublication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionMustBeForPublicationAndStudentException>(act);
    }

    [TestMethod]
    public void Revise__Week_Schedule_Must_Be_The_Students_Teachers()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var otherTeacher = TeacherFakeBuilder.Build();
        var otherTeachersWeek = WeekSchedule.Create(otherTeacher, scenario.Publication.WeekStart);
        var picks = SubmissionFakeBuilder.PicksOf(otherTeachersWeek, 1);
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            otherTeachersWeek,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionWeekScheduleMustMatchStudentWeekException>(act);
    }

    [TestMethod]
    public void Revise__Slot_Must_Be_In_The_Week_Schedule()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var otherTeachersSlot = WeekScheduleFakeBuilder.Build().Slots.First();
        var picks = new List<SlotPick> { SlotPick.Of(otherTeachersSlot, SessionType.Single, null) };
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionSlotMustBeInWeekScheduleException>(act);
    }

    [TestMethod]
    public void Revise__Slot_Must_Be_Open()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var slot = scenario.WeekSchedule.Slots.Last();
        scenario.WeekSchedule.MarkSlotUnavailable(slot);
        var picks = new List<SlotPick> { SlotPick.Of(slot, SessionType.Single, null) };
        var revisedAtUtc = Faker.FakeUtcInstant();

        //when
        var act = () => submission.Revise(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            TargetSessionCount.Of(1),
            picks,
            revisedAtUtc);

        //then
        Should.Throw<SubmissionSlotMustBeOpenException>(act);
    }
}
```

What these pin (see README Review Focus):
- `Create__Slot_Must_Be_Requested_Once` / `Revise__Slot_Must_Be_Requested_Once` → **Review Focus 1** (duplicate slot).
- `Create__Slot_Must_Be_Open` / `Revise__Slot_Must_Be_Open` → **Review Focus 2** (an Unavailable slot id sent by a tampered client) and US-34.
- `Create__Slot_Must_Be_In_The_Week_Schedule` / `Create__Week_Schedule_Must_Be_The_Students_Teachers` and their `Revise__…` twins → **Review Focus 3** (another teacher's grid) on both the create and the edit path.
- `Create__Window_Must_Be_Open` / `Revise__Window_Must_Be_Open` → **Review Focus 4** (window closed mid-submit).
- `Create__Student_Must_Be_Active` / `Revise__Student_Must_Be_Active` → the aggregate half of **Review Focus 6** (deactivated between identify and submit; the HTTP answer is task 4/5's 404).
- `Create__Picks_Must_Cover_Target` row `(10, 3)` → **Review Focus 8** (target far above picks); rows `(3, 2)` → US-40's exact scenario; `(1, 0)` → an empty list is never a submission.
- `Create__Accepts_Extra_Picks_As_Backups` → US-39's exact scenario (target 2, five picks, ranks 1–5).
- `Revise__Moves_To_The_Students_Current_Teachers_Week_Schedule` → README open item 8 (a roster re-upload moved the student mid-week; the revised list lives on the new teacher's grid).
- `Revise__Must_Be_For_Its_Publication_And_Student` / `Revise__Must_Be_For_Its_Publication` → both halves of `MustBeFor`.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~SubmissionTest"`
Expected: build FAILS — `Submission`, `SlotRequest`, the two events and the eight exceptions do not exist.

- [ ] **Step 4: Events**

`src\DrivingLessons.Domain\Events\SubmissionCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SubmissionCreated(
    SubmissionId SubmissionId,
    PublicationId PublicationId,
    StudentId StudentId,
    WeekScheduleId WeekScheduleId,
    TargetSessionCount TargetCount,
    DateTimeOffset SubmittedAtUtc) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\SubmissionRevised.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SubmissionRevised(
    SubmissionId SubmissionId,
    WeekScheduleId WeekScheduleId,
    TargetSessionCount TargetCount,
    DateTimeOffset RevisedAtUtc) : IDomainEvent;
```

One event per operation (roadmap decision 3: one `SubmissionRevised`, no per-request add/remove events). No handler exists yet — `DomainEventDispatcher` simply finds none.

- [ ] **Step 5: Exceptions**

Every message is fixed text: these reach an anonymous caller as the ProblemDetails `detail` (the client never renders it — it maps the status), so no id, national ID or constraint text goes in.

`src\DrivingLessons.Domain\Exceptions\SubmissionWindowMustBeOpenException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWindowMustBeOpenException : DomainException
{
    public SubmissionWindowMustBeOpenException()
        : base("Submissions are accepted only while the week's submission window is open.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionStudentMustBeActiveException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionStudentMustBeActiveException : DomainException
{
    public SubmissionStudentMustBeActiveException()
        : base("Submissions are accepted only from students on the current roster.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionWeekScheduleMustMatchStudentWeekException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWeekScheduleMustMatchStudentWeekException : DomainException
{
    public SubmissionWeekScheduleMustMatchStudentWeekException()
        : base("A submission must use the week schedule of the student's teacher for the publication's week.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionSlotMustBeInWeekScheduleException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeInWeekScheduleException : DomainException
{
    public SubmissionSlotMustBeInWeekScheduleException()
        : base("Every requested slot must belong to the submission's week schedule.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionSlotMustBeOpenException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeOpenException : DomainException
{
    public SubmissionSlotMustBeOpenException()
        : base("Unavailable slots cannot be requested.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionSlotMustBeRequestedOnceException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeRequestedOnceException : DomainException
{
    public SubmissionSlotMustBeRequestedOnceException()
        : base("A slot can be requested at most once per submission.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionPicksMustCoverTargetException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionPicksMustCoverTargetException : DomainException
{
    public SubmissionPicksMustCoverTargetException(TargetSessionCount targetCount, int pickCount)
        : base($"A submission needs at least {targetCount.Value} slot requests to cover its target; "
               + $"it has {pickCount}.")
    {
    }
}
```

(Counts are not personal data, so this one carries them — useful in the API smoke output.)

`src\DrivingLessons.Domain\Exceptions\SubmissionMustBeForPublicationAndStudentException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionMustBeForPublicationAndStudentException : DomainException
{
    public SubmissionMustBeForPublicationAndStudentException()
        : base("A submission can only be revised for its own publication and student.")
    {
    }
}
```

- [ ] **Step 6: `SlotRequest` child entity**

`src\DrivingLessons.Domain\Entities\SlotRequest.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class SlotRequest : Entity<SlotRequestId>
{
    public SlotId SlotId { get; private set; }
    public SessionType SessionType { get; private set; }
    public SlotConstraint? Constraint { get; private set; }
    public Rank Rank { get; private set; }

    private SlotRequest()
    {
    }

    private SlotRequest(
        SlotRequestId id,
        SlotId slotId,
        SessionType sessionType,
        SlotConstraint? constraint,
        Rank rank)
        : base(id)
    {
        SlotId = slotId;
        SessionType = sessionType;
        Constraint = constraint;
        Rank = rank;
    }

    internal static SlotRequest Create(SlotPick pick, Rank rank)
    {
        var id = SlotRequestId.New();
        var slot = pick.Slot;

        return new SlotRequest(id, slot.Id, pick.SessionType, pick.Constraint, rank);
    }
}
```

The request references its slot **by id** (the `Slot` belongs to another aggregate, `WeekSchedule`). It has no `internal` mutators: a revision replaces the whole list (roadmap decision 3), and slice 4's reorder will add `internal void SetRank(Rank)` when it needs one.

- [ ] **Step 7: `Submission` aggregate root**

`src\DrivingLessons.Domain\Entities\Submission.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Submission : AggregateRoot<SubmissionId>
{
    private readonly List<SlotRequest> slotRequests = [];

    public PublicationId PublicationId { get; private set; }
    public StudentId StudentId { get; private set; }
    public WeekScheduleId WeekScheduleId { get; private set; }
    public TargetSessionCount TargetCount { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public DateTimeOffset? RevisedAtUtc { get; private set; }

    public IReadOnlyCollection<SlotRequest> SlotRequests => slotRequests.AsReadOnly();

    private Submission()
    {
    }

    private Submission(
        SubmissionId id,
        PublicationId publicationId,
        StudentId studentId,
        WeekScheduleId weekScheduleId,
        TargetSessionCount targetCount,
        DateTimeOffset submittedAtUtc)
        : base(id)
    {
        PublicationId = publicationId;
        StudentId = studentId;
        WeekScheduleId = weekScheduleId;
        TargetCount = targetCount;
        SubmittedAtUtc = submittedAtUtc;

        var createdEvent = new SubmissionCreated(
            id,
            publicationId,
            studentId,
            weekScheduleId,
            targetCount,
            submittedAtUtc);
        AddEvent(createdEvent);
    }

    public static Submission Create(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks,
        DateTimeOffset submittedAtUtc)
    {
        WindowMustBeOpen(publication);
        StudentMustBeActive(student);
        WeekScheduleMustMatchStudentWeek(publication, student, weekSchedule);
        SlotsMustBeOpenInWeekSchedule(weekSchedule, picks);
        SlotsMustBeRequestedOnce(picks);
        PicksMustCoverTarget(targetCount, picks);

        var id = SubmissionId.New();
        var submission = new Submission(
            id,
            publication.Id,
            student.Id,
            weekSchedule.Id,
            targetCount,
            submittedAtUtc);
        submission.ReplaceSlotRequests(picks);

        return submission;
    }

    public void Revise(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks,
        DateTimeOffset revisedAtUtc)
    {
        MustBeFor(publication, student);
        WindowMustBeOpen(publication);
        StudentMustBeActive(student);
        WeekScheduleMustMatchStudentWeek(publication, student, weekSchedule);
        SlotsMustBeOpenInWeekSchedule(weekSchedule, picks);
        SlotsMustBeRequestedOnce(picks);
        PicksMustCoverTarget(targetCount, picks);

        var weekScheduleId = weekSchedule.Id;
        WeekScheduleId = weekScheduleId;
        TargetCount = targetCount;
        RevisedAtUtc = revisedAtUtc;
        ReplaceSlotRequests(picks);

        AddEvent(new SubmissionRevised(Id, weekScheduleId, targetCount, revisedAtUtc));
    }

    private void ReplaceSlotRequests(IReadOnlyList<SlotPick> picks)
    {
        slotRequests.Clear();

        for (var index = 0; index < picks.Count; index++)
        {
            var pick = picks[index];
            var position = index + 1;
            var rank = Rank.Of(position);
            var slotRequest = SlotRequest.Create(pick, rank);
            slotRequests.Add(slotRequest);
        }
    }

    private void MustBeFor(Publication publication, Student student)
    {
        var isSamePublication = publication.Id == PublicationId;
        var isSameStudent = student.Id == StudentId;

        if (!isSamePublication || !isSameStudent)
        {
            throw new SubmissionMustBeForPublicationAndStudentException();
        }
    }

    private static void WindowMustBeOpen(Publication publication)
    {
        if (!publication.IsOpen)
        {
            throw new SubmissionWindowMustBeOpenException();
        }
    }

    private static void StudentMustBeActive(Student student)
    {
        if (!student.IsActive)
        {
            throw new SubmissionStudentMustBeActiveException();
        }
    }

    private static void WeekScheduleMustMatchStudentWeek(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule)
    {
        var isStudentsTeacher = weekSchedule.TeacherId == student.TeacherId;
        var isPublicationWeek = weekSchedule.WeekStart == publication.WeekStart;

        if (!isStudentsTeacher || !isPublicationWeek)
        {
            throw new SubmissionWeekScheduleMustMatchStudentWeekException();
        }
    }

    private static void SlotsMustBeOpenInWeekSchedule(WeekSchedule weekSchedule, IReadOnlyList<SlotPick> picks)
    {
        var scheduleSlots = weekSchedule.Slots;

        foreach (var pick in picks)
        {
            var scheduleSlot = scheduleSlots.FirstOrDefault(x => x == pick.Slot);

            if (scheduleSlot is null)
            {
                throw new SubmissionSlotMustBeInWeekScheduleException();
            }

            if (!scheduleSlot.IsOpen)
            {
                throw new SubmissionSlotMustBeOpenException();
            }
        }
    }

    private static void SlotsMustBeRequestedOnce(IReadOnlyList<SlotPick> picks)
    {
        var distinctSlotCount = picks
                                    .Select(x => x.Slot)
                                    .Distinct()
                                    .Count();

        if (distinctSlotCount != picks.Count)
        {
            throw new SubmissionSlotMustBeRequestedOnceException();
        }
    }

    private static void PicksMustCoverTarget(TargetSessionCount targetCount, IReadOnlyList<SlotPick> picks)
    {
        var pickCount = picks.Count;

        if (!targetCount.IsCoveredBy(pickCount))
        {
            throw new SubmissionPicksMustCoverTargetException(targetCount, pickCount);
        }
    }
}
```

Notes for the implementer:
- Method body order is guards → mutations → events. `Create` raises `SubmissionCreated` from the private constructor, like `WeekSchedule` (its slots are also added after the constructor).
- The window rule is **state-based** (`publication.IsOpen`): the Quartz close job owns "the window ended" (README decision 3). `Publication.IsOpen` is false for Draft, Published and Closed, which is exactly the three rejected `DataRow`s.
- `SlotsMustBeOpenInWeekSchedule` reads `IsOpen` from the grid's **own** `Slot` instance (`FirstOrDefault(x => x == pick.Slot)`), never from the caller's copy, so a stale instance with the same id cannot pass an Unavailable slot through.
- `Distinct()` over `Slot` uses entity identity equality (`Entity<TId>.Equals`), so two picks of the same slot collapse even if the interactor resolved them separately.
- `Revise` checks `MustBeFor` first so a wrong student is reported as a wrong student, not as a mismatched grid. It re-points `WeekScheduleId` to the grid it was validated against: if a roster re-upload moved the student to another teacher mid-week, the revised list lives on the new teacher's grid (README open item 8).
- `index + 1` is named `position` because a rank is the 1-based position (code-style: no bare magic numbers).

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~SubmissionTest"`
Expected: 29 tests PASS (17 `Create…` incl. data rows, 12 `Revise…`).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS.

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): submission aggregate with ranked slot requests

A student's weekly submission: a target count plus slot requests ranked
in selection order, each Single or Double with an optional constraint.
Only open windows, active students, their own teacher's grid, open
slots, one request per slot and picks covering the target are accepted.
Revise replaces the whole list."
```

---

**Next:** [task-03-submission-persistence.md](task-03-submission-persistence.md)
