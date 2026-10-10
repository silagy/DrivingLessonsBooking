using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationByLink;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class IdentifyStudentInteractorTest
{
    private const string RosterNationalId = "000000018";

    private IPublicationQueries publicationQueries = null!;
    private IStudentQueries studentQueries = null!;
    private IdentifyStudentInteractor interactor = null!;
    private string linkToken = null!;
    private DateOnly weekStart;

    [TestInitialize]
    public void Init()
    {
        publicationQueries = A.Fake<IPublicationQueries>();
        studentQueries = A.Fake<IStudentQueries>();
        interactor = new IdentifyStudentInteractor(publicationQueries, studentQueries);
        linkToken = ShareableLinkToken.New().Value;
        weekStart = new DateOnly(2026, 10, 4);

        var publication = new GetPublicationByLinkResponse
        {
            WeekStart = weekStart,
            WeekNumber = 41,
            State = PublicationState.Open,
            WindowStartUtc = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero),
            WindowEndUtc = new DateTimeOffset(2026, 10, 2, 11, 0, 0, TimeSpan.Zero)
        };

        A.CallTo(() => publicationQueries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns(publication);
    }

    [TestMethod]
    public async Task Returns_The_Roster_Student_For_The_Publication_Week()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var student = new IdentifyStudentResponse
        {
            StudentName = "Test Student",
            TeacherName = "Teacher Cohen",
            CarName = "Corolla White",
            Transmission = Transmission.Automatic
        };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(student);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var response = await interactor.ExecuteAsync(linkToken, request);

        //then
        response.ShouldBeSameAs(student);
    }

    [TestMethod]
    public async Task Identifies_With_The_Teacher_The_Student_Has_Now()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var afterChangeTeacher = new IdentifyStudentResponse
        {
            StudentName = "Test Student",
            TeacherName = "Teacher Carmi",
            CarName = "i20 Silver",
            Transmission = Transmission.Manual
        };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(afterChangeTeacher);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var response = await interactor.ExecuteAsync(linkToken, request);

        //then
        response.TeacherName.ShouldBe("Teacher Carmi");
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Publication_Must_Exist_For_The_Link()
    {
        //given
        var unknownLinkToken = ShareableLinkToken.New().Value;

        A.CallTo(() => publicationQueries.GetByLinkTokenExcludingDraftsAsync(unknownLinkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(unknownLinkToken, request);

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(A<NationalId>._, A<DateOnly>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_Must_Be_Active_On_The_Roster()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }

    [TestMethod]
    public async Task Inactive_Student_Is_Told_To_Contact_The_School()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);
        A.CallTo(() => studentQueries.IsInactiveAsync(nationalId)).Returns(true);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<SubmissionStudentMustBeActiveException>(act);
    }

    [TestMethod]
    public async Task Unknown_National_Id_Is_Still_Not_Found()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);
        A.CallTo(() => studentQueries.IsInactiveAsync(nationalId)).Returns(false);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }

    [TestMethod]
    public async Task Active_Student_Is_Found_Without_The_Inactive_Lookup()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var student = new IdentifyStudentResponse { StudentName = "Test Student" };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(student);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        await interactor.ExecuteAsync(linkToken, request);

        //then
        A.CallTo(() => studentQueries.IsInactiveAsync(A<NationalId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Not_Found_Message_Does_Not_Reveal_The_National_Id()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        var exception = await Should.ThrowAsync<StudentNotFoundException>(act);
        exception.Message.ShouldNotContain(RosterNationalId);
    }

    [TestMethod]
    [DataRow("000000019")]
    [DataRow("0000000181")]
    [DataRow("000 000 018")]
    public async Task National_Id_Must_Be_Well_Formed(string malformedNationalId)
    {
        //given
        var request = new IdentifyStudentRequest(malformedNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<DomainException>(act);
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(A<NationalId>._, A<DateOnly>._))
            .MustNotHaveHappened();
    }
}
