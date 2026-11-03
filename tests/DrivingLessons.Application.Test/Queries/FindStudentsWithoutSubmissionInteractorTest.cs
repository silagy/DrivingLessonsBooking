using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudentsWithoutSubmission;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class FindStudentsWithoutSubmissionInteractorTest
{
    private const string FirstStudentName = "Noa";
    private const string SecondStudentName = "Omer";

    private IStudentQueries studentQueries = null!;
    private ICurrentUser currentUser = null!;
    private FindStudentsWithoutSubmissionInteractor interactor = null!;
    private Guid publicationId;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        studentQueries = A.Fake<IStudentQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new FindStudentsWithoutSubmissionInteractor(studentQueries, currentUser);

        publicationId = Guid.NewGuid();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse> students =
        [
            new ItemForFindStudentsWithoutSubmissionResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = FirstStudentName,
                Phone = "050-1234567",
                WhatsAppNumber = "972501234567"
            },
            new ItemForFindStudentsWithoutSubmissionResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = SecondStudentName,
                Phone = "02-1234567",
                WhatsAppNumber = null
            }
        ];

        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, A<Guid?>._)).Returns(students);
    }

    [TestMethod]
    public async Task Teacher_Sees_Own_Students_Without_Submission()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        result.Select(x => x.StudentName).ShouldBe([FirstStudentName, SecondStudentName]);
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, ownTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_List_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(A<Guid>._, A<Guid?>._))
         .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_List()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(A<Guid>._, A<Guid?>._))
         .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Any_Teachers_List()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        result.Select(x => x.StudentName).ShouldBe([FirstStudentName, SecondStudentName]);
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, otherTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Publication_Must_Exist()
    {
        //given
        SignedInAs(Role.Administrator, null);

        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, A<Guid?>._))
         .Returns((IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>?)null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
