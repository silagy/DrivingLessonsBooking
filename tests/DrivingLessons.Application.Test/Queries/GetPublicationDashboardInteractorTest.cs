using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetPublicationDashboardInteractorTest
{
    private const int StudentsSubmitted = 3;
    private const int TotalPicks = 7;

    private IPublicationQueries publicationQueries = null!;
    private ISubmissionQueries submissionQueries = null!;
    private ICurrentUser currentUser = null!;
    private GetPublicationDashboardInteractor interactor = null!;
    private Guid publicationId;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        publicationQueries = A.Fake<IPublicationQueries>();
        submissionQueries = A.Fake<ISubmissionQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetPublicationDashboardInteractor(publicationQueries, submissionQueries, currentUser);

        publicationId = Guid.NewGuid();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        var dashboard = new GetPublicationDashboardResponse
        {
            State = PublicationState.Open,
            LinkToken = "link-token",
            LatestExcelVersion = 2,
            SlotCounts = []
        };
        var noCounts = new Dictionary<Guid, int>();
        var stats = new SubmissionStats(StudentsSubmitted, TotalPicks, null);

        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._))
         .Returns((GetPublicationDashboardResponse?)null);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, ownTeacherId.Value)).Returns(dashboard);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value)).Returns(dashboard);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._)).Returns(noCounts);
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).Returns(stats);
    }

    [TestMethod]
    public async Task Teacher_Sees_Own_Dashboard()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        result.TotalPicks.ShouldBe(TotalPicks);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, ownTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_Dashboard_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Dashboard()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Any_Teachers_Dashboard()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Sees_Another_Teachers_Dashboard()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Publication_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Administrator, null);
        var missingPublicationId = Guid.NewGuid();

        //when
        var act = () => interactor.ExecuteAsync(missingPublicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
