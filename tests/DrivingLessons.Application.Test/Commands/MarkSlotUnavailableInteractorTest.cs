using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.MarkSlotUnavailable;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class MarkSlotUnavailableInteractorTest
{
    private IWeekScheduleRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ICurrentUser currentUser = null!;
    private MarkSlotUnavailableInteractor interactor = null!;
    private Teacher teacher = null!;
    private Teacher otherTeacher = null!;
    private WeekSchedule weekSchedule = null!;
    private Slot slot = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IWeekScheduleRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new MarkSlotUnavailableInteractor(repository, unitOfWork, currentUser);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        otherTeacher = Teacher.Create(TeacherName.Of("Yael Carmi"), Email.Of("yael@school.example"));
        weekSchedule = WeekSchedule.Create(teacher, WeekStart.Of(new DateOnly(2026, 10, 4)));
        slot = weekSchedule.Slots.First();

        A.CallTo(() => repository.GetAsync(A<WeekScheduleId>._)).Returns((WeekSchedule?)null);
        A.CallTo(() => repository.GetAsync(weekSchedule.Id)).Returns(weekSchedule);
    }

    [TestMethod]
    public async Task Teacher_Marks_Own_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_Slot_Is_Not_Marked()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);
        var notFound = new WeekScheduleNotFoundException(weekSchedule.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        var exception = await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        exception.Message.ShouldBe(notFound.Message);
        slot.State.ShouldBe(SlotState.Open);
        weekSchedule.UncommittedEvents.OfType<SlotMarkedUnavailable>().ShouldBeEmpty();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Other_Teachers_Week_Schedule_Is_Not_Found_Before_The_Slot_Lookup()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, Guid.NewGuid());

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Marks_Any_Teachers_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Administrator, null);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Marks_Another_Teachers_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Administrator, otherTeacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), slot.Id.Value);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private void SignInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
