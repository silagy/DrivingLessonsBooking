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
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
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
