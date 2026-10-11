using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class FindPublicationHistoryInteractorTest
{
    private IPublicationQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private FindPublicationHistoryInteractor interactor = null!;
    private TeacherId ownTeacherId = null!;
    private List<ItemForFindPublicationHistoryResponse> ownRows = null!;
    private List<ItemForFindPublicationHistoryResponse> everyRow = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IPublicationQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new FindPublicationHistoryInteractor(queries, currentUser);

        ownTeacherId = TeacherId.New();
        var ownRow = HistoryRow(ownTeacherId.Value, "Yael Carmi");
        var otherRow = HistoryRow(Guid.NewGuid(), "Dana Levi");
        ownRows = [ownRow];
        everyRow = [ownRow, otherRow];

        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).Returns(ownRows);
        A.CallTo(() => queries.FindHistoryAsync(null)).Returns(everyRow);
    }

    [TestMethod]
    public async Task Teacher_Sees_Only_Own_History()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(ownRows);
        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).MustHaveHappenedOnceExactly();
        A.CallTo(() => queries.FindHistoryAsync(null)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Every_Teachers_History()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(everyRow);
        A.CallTo(() => queries.FindHistoryAsync(null)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Sees_Every_Teachers_History()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(everyRow);
        A.CallTo(() => queries.FindHistoryAsync(null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Sees_No_History()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBeEmpty();
        A.CallTo(() => queries.FindHistoryAsync(A<Guid?>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }

    private static ItemForFindPublicationHistoryResponse HistoryRow(Guid teacherId, string teacherName)
    {
        return new ItemForFindPublicationHistoryResponse
        {
            PublicationId = Guid.NewGuid(),
            WeekStart = new DateOnly(2026, 10, 4),
            TeacherId = teacherId,
            TeacherName = teacherName,
            State = PublicationState.Closed,
            LatestExcelVersion = 1
        };
    }
}
