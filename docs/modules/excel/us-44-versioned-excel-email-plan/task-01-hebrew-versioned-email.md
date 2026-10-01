# Task 1 of 3: The Hebrew versioned email — subject, body, attachment name, one teacher never blocks another

> Part of [US-44: Versioned Excel Email](README.md). Work on branch `44-us-44-versioned-excel-email`, commands from the repo root in bash.

**Files:**
- Modify: `src\DrivingLessons.Application\DrivingLessons.Application.csproj` (+ `Microsoft.Extensions.Logging.Abstractions`)
- Modify: `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs`
- Test: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs` (new)
- Modify: `docs\requirements.md` (§6.4 example, decision #22)
- Never staged: `.claude\launch.json`, `docs\modules\excel\us-45-excel-summary-sheet-plan\task-0{1,2}-*.md` (unrelated local edits)

**Interfaces:**
- Consumes (on `main`): `IDomainEventHandler<PublicationClosed>`; `IPublicationRepository.GetAsync(PublicationId) : Task<Publication?>`; `ITeacherRepository.GetAsync(TeacherId) : Task<Teacher?>` (returns `null` for a soft-deleted teacher — `Teacher` has a query filter); `IExcelGenerator.GenerateAsync(PublicationId, TeacherId) : Task<ExcelFile>`; `record ExcelFile(string FileName, byte[] Content, string ContentType)`; `IEmailSender.SendAsync(EmailMessage) : Task`; `record EmailMessage(string ToEmail, string Subject, string Body, ExcelFile Attachment)`; `Publication.TeacherVersions : IReadOnlyCollection<TeacherExcelVersion>` (`TeacherId`, `Version`, in close order); `WeekStart.WeekNumber` (ISO week of the Monday).
- Produces:
  - `PublicationClosedHandler(IPublicationRepository, ITeacherRepository, IExcelGenerator, IEmailSender, ILogger<PublicationClosedHandler>)` — DI resolves the new logger with no registration change.
  - Every `EmailMessage` it sends: `Subject` = `בקשות לשבוע {week} - {teacher name} - גרסה {version}`; `Body` = three lines joined by `'\n'`; `Attachment` = the generated file with `FileName` = subject (unsafe characters → `-`) + `.xlsx`, same `Content` and `ContentType`. Task 2's `EmailMimeMessage` splits `Body` on `'\n'` into HTML paragraphs.
  - The contract task 2 relies on: an exception thrown by `IEmailSender.SendAsync` (or `IExcelGenerator`) is caught and logged here — the sender may throw freely.

`IEmailSender` stays `LoggingEmailSender` in this task, so the running app still only logs the send (now with the Hebrew subject).

- [ ] **Step 1: Write the failing handler tests**

Create `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs`:

```csharp
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
        sent.ShouldHaveSingleItem().Subject.ShouldBe("בקשות לשבוע 41 - משה כהן - גרסה 2");
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
```

How the fakes are wired:
- `Init` makes the generator return a distinct file per teacher (`ExcelFor(teacherId)` — the teacher's GUID bytes), so a test can prove each teacher got **their own** file, not a neighbor's.
- `emailSender.SendAsync` records every message in `sent`. A test that makes one send throw configures a narrower rule (`A<EmailMessage>.That.Matches(…)`) — FakeItEasy uses the latest matching rule, so that call throws and is never recorded.
- `Closed(…)` walks a real `Publication` through `Publish → Open → Close`, so `TeacherVersions` holds real versions in the order the teachers were passed. 4 October 2026 is a Sunday; its Monday falls in ISO week **41**.
- `ErrorsLogged()` counts `ILogger.Log` calls at `LogLevel.Error` on the fake logger — `LogError(…)` is an extension method over `Log`.

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~PublicationClosedHandlerTest"`
Expected: FAIL — the build breaks with `CS1729: 'PublicationClosedHandler' does not contain a constructor that takes 5 arguments`. (The test project already sees `Microsoft.Extensions.Logging` through Infrastructure; Application does not yet.)

- [ ] **Step 3: Give Application the logging abstractions**

```bash
dotnet add src/DrivingLessons.Application package Microsoft.Extensions.Logging.Abstractions --version 10.0.9
```

`src\DrivingLessons.Application\DrivingLessons.Application.csproj` now holds, beside the DI abstractions:

```xml
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
```

(10.0.9 matches every other `Microsoft.Extensions.*` package in the solution.)

- [ ] **Step 4: Rewrite the handler**

Replace the contents of `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs` with:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private const string AttachmentExtension = ".xlsx";
    private const char FileNameReplacement = '-';

    private static readonly char[] UnsafeFileNameCharacters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;
    private readonly ILogger<PublicationClosedHandler> logger;

    public PublicationClosedHandler(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender,
        ILogger<PublicationClosedHandler> logger)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
        this.logger = logger;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        foreach (var teacherVersion in publication.TeacherVersions)
        {
            try
            {
                await SendExcelAsync(publication, teacherVersion);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Excel email for teacher [{TeacherId}] of publication [{PublicationId}] failed.",
                    teacherVersion.TeacherId.Value,
                    publication.Id.Value);
            }
        }
    }

    private async Task SendExcelAsync(Publication publication, TeacherExcelVersion teacherVersion)
    {
        var teacher = await teacherRepository.GetAsync(teacherVersion.TeacherId);

        if (teacher is null)
        {
            return;
        }

        var week = publication.WeekStart.WeekNumber;
        var teacherName = teacher.Name.Value;
        var version = teacherVersion.Version;

        var subject = Subject(week, teacherName, version);
        var body = Body(week, teacherName, version);

        var excel = await excelGenerator.GenerateAsync(publication.Id, teacherVersion.TeacherId);
        var attachment = excel with { FileName = AttachmentFileName(subject) };

        var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, attachment);

        await emailSender.SendAsync(message);
    }

    private static string Subject(int week, string teacherName, int version)
    {
        return $"בקשות לשבוע {week} - {teacherName} - גרסה {version}";
    }

    private static string Body(int week, string teacherName, int version)
    {
        return string.Join(
            '\n',
            $"שלום {teacherName},",
            $"מצורף קובץ הבקשות לשבוע {week}, גרסה {version}.",
            "קובץ עם מספר גרסה גבוה יותר מחליף את כל הקבצים הקודמים של אותו שבוע.");
    }

    private static string AttachmentFileName(string subject)
    {
        var safeName = string.Concat(subject.Select(x => UnsafeFileNameCharacters.Contains(x) ? FileNameReplacement : x));

        return safeName + AttachmentExtension;
    }
}
```

Why it is shaped this way:
- **The `try/catch` wraps one teacher's whole generate-and-send.** The close is already saved when this runs (`CommitAsync` saves, then dispatches), so catching cannot lose the state change — it only stops one teacher's failure from skipping everyone after them, and stops a mail outage during the startup reconciliation from stopping the app (README decision 4). `catch (Exception)` is deliberate: SMTP, socket, TLS and Excel failures share no base type.
- **The log line carries GUIDs only** — the teacher's address and name are personal data (code-style.md Logging). The exception is attached so the stack and the server's reply reach the logs.
- **The version is `teacherVersion.Version`**, the number the domain bumped on this close — never recomputed here (§6.4).
- **`excel with { FileName = … }`** renames only the attachment; `ExcelGenerator` and the admin download keep `week-{date}-{guid}.xlsx` (README decision 3). The unsafe set is the Windows-forbidden file-name characters — the strictest desktop a teacher is likely to save to.
- **The body is plain lines joined by `'\n'`**: task 2 turns it into a right-to-left HTML part, one `<p>` per line.

- [ ] **Step 5: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~PublicationClosedHandlerTest"`
Expected: PASS — 14 tests (11 methods; `Replaces_Characters_A_File_Name_Cannot_Hold` runs 4 rows).

Then `dotnet build` and `dotnet test` — build clean (no new warnings beyond the existing `NU1903` / `MSTEST0001` / `CS8618`), every test PASSES; the Application suite goes from 97 to 111.

- [ ] **Step 6: Record the Hebrew subject in the requirements**

In `docs\requirements.md` §6.4, replace

```markdown
- **Versioning rule:** every window close event fires the email. The email subject carries an incrementing version number (e.g., "Week 25 Requests - Teacher Cohen - v2") so the teacher always knows whether a previously received file is stale.
```

with

```markdown
- **Versioning rule:** every window close event fires the email. The email subject carries an incrementing version number (e.g., "בקשות לשבוע 25 - משה כהן - גרסה 2"; the email is Hebrew, see decision #22) so the teacher always knows whether a previously received file is stale.
```

In §11 Decisions Log, add after the row for decision 21:

```markdown
| 22 | **The teacher's Excel email is Hebrew**: subject "בקשות לשבוע N - {teacher} - גרסה K", a right-to-left body, and the attachment named after the subject | Teachers read Hebrew and the workbook is already Hebrew (Excel roadmap decision 1); the version in the attachment's name keeps a saved file identifiable after it leaves the inbox |
```

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/DrivingLessons.Application.csproj \
  src/DrivingLessons.Application/EventHandlers/PublicationClosedHandler.cs \
  tests/DrivingLessons.Application.Test/EventHandlers/PublicationClosedHandlerTest.cs \
  docs/requirements.md
git status --short
git commit -m "feat(excel): Hebrew versioned email per teacher, one failure never blocks the rest

The close email's subject and body are Hebrew and carry the week,
the teacher and the close version; the attachment is named after the
subject. Each teacher's generate-and-send is isolated: a failure is
logged with the teacher and publication ids and the next teacher is
still sent their file.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Before committing, `git status --short` must show the four paths staged (`M ` / `A `) and only ` M .claude/launch.json` plus the two US-45 plan files unstaged.

---

**Next:** [task-02-smtp-sender.md](task-02-smtp-sender.md)
