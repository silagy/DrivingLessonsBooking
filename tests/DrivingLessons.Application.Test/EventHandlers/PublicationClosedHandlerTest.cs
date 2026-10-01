using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace DrivingLessons.Application.Test.EventHandlers;

[TestClass]
public class PublicationClosedHandlerTest
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private IPublicationRepository publicationRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IExcelGenerator excelGenerator = null!;
    private IEmailSender emailSender = null!;
    private ILogger<PublicationClosedHandler> logger = null!;
    private PublicationClosedHandler handler = null!;
    private List<EmailMessage> sent = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        excelGenerator = A.Fake<IExcelGenerator>();
        emailSender = A.Fake<IEmailSender>();
        logger = A.Fake<ILogger<PublicationClosedHandler>>();
        handler = new PublicationClosedHandler(publicationRepository, teacherRepository, excelGenerator, emailSender, logger);
        sent = [];

        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._))
            .ReturnsLazily((PublicationId _, TeacherId teacherId) => ExcelFor(teacherId));
        A.CallTo(() => emailSender.SendAsync(A<EmailMessage>._))
            .Invokes((EmailMessage message) => sent.Add(message));
    }

    [TestMethod]
    public async Task Sends_The_Teachers_File_To_Their_Contact_Email()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var message = sent.ShouldHaveSingleItem();
        message.ToEmail.ShouldBe("moshe.cohen@example.com");
        message.Attachment.Content.ShouldBe(ExcelFor(teacher.Id).Content);
        message.Attachment.ContentType.ShouldBe(ContentType);
    }

    [TestMethod]
    public async Task Subject_Names_The_Week_The_Teacher_And_The_Version_In_Hebrew()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().Subject.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 1");
    }

    [TestMethod]
    public async Task Subject_Carries_The_Next_Version_After_A_Reopen_And_Close()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);
        publication.Reopen(DateTimeOffset.UtcNow.AddDays(5));
        publication.Close([teacher.Id]);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var message = sent.ShouldHaveSingleItem();
        message.Subject.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 2");
        message.Attachment.FileName.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 2.xlsx");
    }

    [TestMethod]
    public async Task Body_Greets_The_Teacher_And_Names_The_Week_And_The_Version()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().Body.Split('\n').ShouldBe(
            [
                "שלום משה כהן,",
                "מצורף קובץ הבקשות לשבוע 41, גרסה 1.",
                "קובץ עם מספר גרסה גבוה יותר מחליף את כל הקבצים הקודמים של אותו שבוע."
            ]);
    }

    [TestMethod]
    public async Task Names_The_Attachment_After_The_Subject()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().Attachment.FileName.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 1.xlsx");
    }

    [TestMethod]
    [DataRow("כהן/לוי", "בקשות לשבוע 41 - כהן-לוי - גרסה 1.xlsx")]
    [DataRow("Cohen\\Levi", "בקשות לשבוע 41 - Cohen-Levi - גרסה 1.xlsx")]
    [DataRow("\"Moshe\" Cohen?", "בקשות לשבוע 41 - -Moshe- Cohen- - גרסה 1.xlsx")]
    [DataRow("Cohen: <Levi>*|", "בקשות לשבוע 41 - Cohen- -Levi--- - גרסה 1.xlsx")]
    public async Task Replaces_Characters_A_File_Name_Cannot_Hold(string teacherName, string expectedFileName)
    {
        //given
        var teacher = TeacherNamed(teacherName, "teacher@example.com");
        var publication = Closed(teacher);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var message = sent.ShouldHaveSingleItem();
        message.Attachment.FileName.ShouldBe(expectedFileName);
        message.Subject.ShouldBe($"בקשות לשבוע 41 - {teacherName} - גרסה 1");
    }

    [TestMethod]
    public async Task Sends_Each_Teacher_Their_Own_File()
    {
        //given
        var cohen = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(cohen, levi);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.Select(x => x.ToEmail).ShouldBe(
            [
                "moshe.cohen@example.com",
                "dana.levi@example.com"
            ]);
        sent[0].Attachment.Content.ShouldBe(ExcelFor(cohen.Id).Content);
        sent[1].Attachment.Content.ShouldBe(ExcelFor(levi.Id).Content);
    }

    [TestMethod]
    public async Task Skips_A_Teacher_Deleted_Since_The_Close()
    {
        //given
        var deleted = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(deleted, levi);

        A.CallTo(() => teacherRepository.GetAsync(deleted.Id))
            .Returns((Teacher?)null);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        ErrorsLogged().ShouldBe(0);
    }

    [TestMethod]
    public async Task A_Failed_Send_Is_Logged_And_The_Next_Teacher_Still_Gets_Their_File()
    {
        //given
        var cohen = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(cohen, levi);

        A.CallTo(() => emailSender.SendAsync(A<EmailMessage>.That.Matches(x => x.ToEmail == "moshe.cohen@example.com")))
            .ThrowsAsync(new IOException("SMTP server unreachable"));

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        ErrorsLogged().ShouldBe(1);
    }

    [TestMethod]
    public async Task A_Failed_Excel_Is_Logged_And_The_Next_Teacher_Still_Gets_Their_File()
    {
        //given
        var cohen = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(cohen, levi);

        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, cohen.Id))
            .ThrowsAsync(new InvalidOperationException("Excel failed"));

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        ErrorsLogged().ShouldBe(1);
    }

    [TestMethod]
    public async Task Sends_Nothing_When_The_Publication_Is_Gone()
    {
        //given
        var publicationId = PublicationId.New();

        A.CallTo(() => publicationRepository.GetAsync(publicationId))
            .Returns((Publication?)null);

        //when
        await handler.HandleAsync(new PublicationClosed(publicationId));

        //then
        sent.ShouldBeEmpty();
    }

    private Teacher TeacherNamed(string name, string contactEmail)
    {
        var teacher = Teacher.Create(TeacherName.Of(name), Email.Of(contactEmail));

        A.CallTo(() => teacherRepository.GetAsync(teacher.Id))
            .Returns(teacher);

        return teacher;
    }

    private Publication Closed(params Teacher[] teachers)
    {
        var publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        var startUtc = DateTimeOffset.UtcNow.AddDays(-1);
        publication.Publish(SubmissionWindow.Of(startUtc, startUtc.AddHours(1)));
        publication.Open();
        publication.Close(teachers.Select(x => x.Id));

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);

        return publication;
    }

    private int ErrorsLogged()
    {
        return Fake.GetCalls(logger)
                   .Count(x => x.Method.Name == nameof(ILogger.Log) && x.GetArgument<LogLevel>(0) == LogLevel.Error);
    }

    private static ExcelFile ExcelFor(TeacherId teacherId)
    {
        var content = teacherId.Value.ToByteArray();

        return new ExcelFile($"week-2026-10-04-{teacherId.Value}.xlsx", content, ContentType);
    }
}
