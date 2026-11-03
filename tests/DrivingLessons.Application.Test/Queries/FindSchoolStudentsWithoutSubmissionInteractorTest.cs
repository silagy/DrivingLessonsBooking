using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudentsWithoutSubmission;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class FindSchoolStudentsWithoutSubmissionInteractorTest
{
    private const string FirstStudentName = "Noa";
    private const string SecondStudentName = "Omer";

    private IStudentQueries studentQueries = null!;
    private FindSchoolStudentsWithoutSubmissionInteractor interactor = null!;
    private Guid publicationId;

    [TestInitialize]
    public void Init()
    {
        studentQueries = A.Fake<IStudentQueries>();
        interactor = new FindSchoolStudentsWithoutSubmissionInteractor(studentQueries);

        publicationId = Guid.NewGuid();
    }

    [TestMethod]
    public async Task Lists_Every_Teachers_Students_Without_Submission()
    {
        //given
        IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse> students =
        [
            new ItemForFindStudentsWithoutSubmissionResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = FirstStudentName,
                TeacherId = Guid.NewGuid(),
                TeacherName = "Dana"
            },
            new ItemForFindStudentsWithoutSubmissionResponse
            {
                StudentId = Guid.NewGuid(),
                StudentName = SecondStudentName,
                TeacherId = Guid.NewGuid(),
                TeacherName = "Yossi"
            }
        ];

        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, null)).Returns(students);

        //when
        var result = await interactor.ExecuteAsync(publicationId);

        //then
        result.Select(x => x.StudentName).ShouldBe([FirstStudentName, SecondStudentName]);
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, null))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Publication_Must_Exist()
    {
        //given
        A.CallTo(() => studentQueries.FindWithoutSubmissionAsync(publicationId, null))
         .Returns((IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>?)null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
    }
}
