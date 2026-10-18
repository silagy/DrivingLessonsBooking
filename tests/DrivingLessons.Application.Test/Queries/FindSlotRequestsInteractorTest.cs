using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindSlotRequests;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class FindSlotRequestsInteractorTest
{
    private const string FirstStudentName = "Noa Mizrahi";
    private const string SecondStudentName = "Omer Biton";

    private ISubmissionQueries submissionQueries = null!;
    private ICurrentUser currentUser = null!;
    private FindSlotRequestsInteractor interactor = null!;
    private Guid publicationId;
    private Guid slotId;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        submissionQueries = A.Fake<ISubmissionQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new FindSlotRequestsInteractor(submissionQueries, currentUser);

        publicationId = Guid.NewGuid();
        slotId = Guid.NewGuid();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        IReadOnlyList<ItemForFindSlotRequestsResponse> requests =
        [
            new ItemForFindSlotRequestsResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = FirstStudentName,
                Rank = 1,
                PickCount = 3
            },
            new ItemForFindSlotRequestsResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = SecondStudentName,
                Rank = 2,
                PickCount = 2
            }
        ];

        A.CallTo(() => submissionQueries.FindSlotRequestsAsync(publicationId, A<Guid>._, slotId)).Returns(requests);
    }

    [TestMethod]
    public async Task Teacher_Sees_Who_Requested_Own_Slot()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, ownTeacherId.Value, slotId);

        //then
        result.Select(x => x.StudentName).ShouldBe([FirstStudentName, SecondStudentName]);
        result[0].Rank.ShouldBe(1);
        result[0].PickCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task Other_Teachers_Slot_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, otherTeacherId.Value, slotId);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => submissionQueries.FindSlotRequestsAsync(A<Guid>._, A<Guid>._, A<Guid>._))
         .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Slot()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, ownTeacherId.Value, slotId);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => submissionQueries.FindSlotRequestsAsync(A<Guid>._, A<Guid>._, A<Guid>._))
         .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Who_Requested_Any_Teachers_Slot()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value, slotId);

        //then
        result.Select(x => x.StudentName).ShouldBe([FirstStudentName, SecondStudentName]);
        A.CallTo(() => submissionQueries.FindSlotRequestsAsync(publicationId, otherTeacherId.Value, slotId))
         .MustHaveHappenedOnceExactly();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
