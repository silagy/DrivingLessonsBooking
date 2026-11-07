using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Consumers.TeacherExcelEmail;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace DrivingLessons.Application.Test.Consumers;

[TestClass]
public class SendTeacherExcelEmailsConsumerTest
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private IPublicationRepository publicationRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IExcelGenerator excelGenerator = null!;
    private IEmailSender emailSender = null!;
    private ITeacherExcelEmailLog emailLog = null!;
    private IUnitOfWork unitOfWork = null!;
    private ILogger<SendTeacherExcelEmailsConsumer> logger = null!;
    private SendTeacherExcelEmailsConsumer consumer = null!;
    private List<EmailMessage> sent = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        excelGenerator = A.Fake<IExcelGenerator>();
        emailSender = A.Fake<IEmailSender>();
        emailLog = A.Fake<ITeacherExcelEmailLog>();
        unitOfWork = A.Fake<IUnitOfWork>();
        logger = A.Fake<ILogger<SendTeacherExcelEmailsConsumer>>();
        var timeProvider = A.Fake<TimeProvider>();
        consumer = new SendTeacherExcelEmailsConsumer(
            publicationRepository,
            teacherRepository,
            excelGenerator,
            emailSender,
            emailLog,
            unitOfWork,
            timeProvider,
            logger);
        sent = [];

        A.CallTo(() => timeProvider.GetUtcNow()).Returns(NowUtc);
        A.CallTo(() => emailLog.WasSentAsync(A<PublicationId>._, A<TeacherId>._, A<int>._)).Returns(false);
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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        await consumer.ConsumeAsync(EventFor(publication));

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
        var act = () => consumer.ConsumeAsync(EventFor(publication));

        //then
        await act.ShouldThrowAsync<TeacherExcelEmailFailedException>();
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
        var act = () => consumer.ConsumeAsync(EventFor(publication));

        //then
        await act.ShouldThrowAsync<TeacherExcelEmailFailedException>();
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        ErrorsLogged().ShouldBe(1);
    }

    [TestMethod]
    public async Task Sends_Nothing_When_The_Publication_Is_Gone_Since_The_Close()
    {
        //given
        var publicationId = PublicationId.New();

        A.CallTo(() => publicationRepository.GetAsync(publicationId))
            .Returns((Publication?)null);

        //when
        await consumer.ConsumeAsync(new PublicationClosedIntegrationEvent(publicationId.Value, [new ClosedTeacherVersion(Guid.NewGuid(), 1)]));

        //then
        sent.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Skips_A_Teacher_Already_Sent_For_This_Version()
    {
        //given
        var cohen = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(cohen, levi);

        A.CallTo(() => emailLog.WasSentAsync(publication.Id, cohen.Id, 1)).Returns(true);

        //when
        await consumer.ConsumeAsync(EventFor(publication));

        //then
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, cohen.Id)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Is_Done_When_Every_Teacher_Was_Sent()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        var outcome = await consumer.ConsumeAsync(EventFor(publication));

        //then
        outcome.ShouldBe(DeliveryOutcome.Done);
    }

    [TestMethod]
    public async Task Records_And_Commits_Each_Sent_Email_Right_After_Sending_It()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        //when
        await consumer.ConsumeAsync(EventFor(publication));

        //then
        A.CallTo(() => emailSender.SendAsync(A<EmailMessage>._))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => emailLog.RecordSent(publication.Id, teacher.Id, 1, NowUtc)).MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly());
    }

    [TestMethod]
    public async Task Does_Not_Record_A_Failed_Send()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);

        A.CallTo(() => emailSender.SendAsync(A<EmailMessage>._))
            .ThrowsAsync(new IOException("SMTP server unreachable"));

        //when
        var act = () => consumer.ConsumeAsync(EventFor(publication));

        //then
        await act.ShouldThrowAsync<TeacherExcelEmailFailedException>();
        A.CallTo(() => emailLog.RecordSent(A<PublicationId>._, A<TeacherId>._, A<int>._, A<DateTimeOffset>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Throws_After_Trying_Every_Teacher_When_One_Fails()
    {
        //given
        var cohen = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var levi = TeacherNamed("דנה לוי", "dana.levi@example.com");
        var publication = Closed(cohen, levi);

        A.CallTo(() => emailSender.SendAsync(A<EmailMessage>.That.Matches(x => x.ToEmail == "moshe.cohen@example.com")))
            .ThrowsAsync(new IOException("SMTP server unreachable"));

        //when
        var act = () => consumer.ConsumeAsync(EventFor(publication));

        //then
        var exception = await act.ShouldThrowAsync<TeacherExcelEmailFailedException>();
        exception.TeacherIds.ShouldBe([cohen.Id]);
        sent.ShouldHaveSingleItem().ToEmail.ShouldBe("dana.levi@example.com");
        A.CallTo(() => emailLog.RecordSent(publication.Id, levi.Id, 1, NowUtc)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Uses_The_Version_From_The_Event()
    {
        //given
        var teacher = TeacherNamed("משה כהן", "moshe.cohen@example.com");
        var publication = Closed(teacher);
        var firstClose = EventFor(publication);
        publication.Reopen(DateTimeOffset.UtcNow.AddDays(5));
        publication.Close([teacher.Id]);

        //when
        await consumer.ConsumeAsync(firstClose);

        //then
        sent.ShouldHaveSingleItem().Subject.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 1");
        A.CallTo(() => emailLog.RecordSent(publication.Id, teacher.Id, 1, NowUtc)).MustHaveHappenedOnceExactly();
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

    private static PublicationClosedIntegrationEvent EventFor(Publication publication)
    {
        var teachers = publication.TeacherVersions
                                  .Select(x => new ClosedTeacherVersion(x.TeacherId.Value, x.Version))
                                  .ToList();

        return new PublicationClosedIntegrationEvent(publication.Id.Value, teachers);
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
