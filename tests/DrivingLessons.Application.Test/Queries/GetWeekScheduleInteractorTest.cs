using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetWeekScheduleInteractorTest
{
    private static readonly DateOnly WeekStartDate = new(2026, 10, 4);

    private IWeekScheduleQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private GetWeekScheduleInteractor interactor = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;
    private GetWeekScheduleResponse ownWeekSchedule = null!;
    private GetWeekScheduleResponse otherWeekSchedule = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IWeekScheduleQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetWeekScheduleInteractor(queries, currentUser);

        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        ownWeekSchedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = ownTeacherId.Value,
            WeekStart = WeekStartDate
        };
        otherWeekSchedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = otherTeacherId.Value,
            WeekStart = WeekStartDate
        };

        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._))
         .Returns((GetWeekScheduleResponse?)null);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(ownTeacherId.Value, WeekStartDate))
         .Returns(ownWeekSchedule);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(otherTeacherId.Value, WeekStartDate))
         .Returns(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Teacher_Reaches_Own_Week_Schedule()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(ownTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(ownWeekSchedule);
    }

    [TestMethod]
    public async Task Other_Teachers_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);
        var notFound = new WeekScheduleNotFoundException(otherTeacherId.Value, WeekStartDate);

        //when
        var act = () => interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        var exception = await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        exception.Message.ShouldBe(notFound.Message);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Week_Schedule()
    {
        //given
        SignInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(ownTeacherId.Value, WeekStartDate);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Reaches_Any_Teachers_Week_Schedule()
    {
        //given
        SignInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Linked_Administrator_Reaches_Another_Teachers_Week_Schedule()
    {
        //given
        SignInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Missing_Own_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);
        var nextWeek = WeekStartDate.AddDays(7);

        //when
        var act = () => interactor.ExecuteAsync(ownTeacherId.Value, nextWeek);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(ownTeacherId.Value, nextWeek)).MustHaveHappenedOnceExactly();
    }

    private void SignInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
