# Task 4 of 6: The teacher's Excel email becomes the first consumer

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`. Requires tasks 1 to 3 committed.

**Files:**
- Create: `src\DrivingLessons.Application\IntegrationEvents\Publications\PublicationClosedIntegrationEvent.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\Publications\ClosedTeacherVersion.cs`
- Create: `src\DrivingLessons.Application\Abstractions\ITeacherExcelEmailLog.cs`
- Create: `src\DrivingLessons.Application\Consumers\TeacherExcelEmail\SendTeacherExcelEmailsConsumer.cs`
- Create: `src\DrivingLessons.Application\Consumers\TeacherExcelEmail\TeacherExcelEmailFailedException.cs`
- Modify: `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs` (queues the integration event; sends nothing)
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (event + consumer)
- Create: `src\DrivingLessons.Infrastructure\Email\SentTeacherExcelEmail.cs`
- Create: `src\DrivingLessons.Infrastructure\Email\TeacherExcelEmailLog.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\SentTeacherExcelEmailConfiguration.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (`DbSet`)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (`ITeacherExcelEmailLog`)
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_AddSentTeacherExcelEmails.cs` (+ designer, snapshot), generated
- Move + modify: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs` → `tests\DrivingLessons.Application.Test\Consumers\SendTeacherExcelEmailsConsumerTest.cs`
- Create: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs` (new content)

**Interfaces:**
- Consumes (on `main`): `IDomainEventHandler<PublicationClosed>`; `IPublicationRepository.GetAsync(PublicationId)`; `ITeacherRepository.GetAsync(TeacherId)` (`null` for a soft-deleted Teacher); `IExcelGenerator.GenerateAsync(PublicationId, TeacherId) : Task<ExcelFile>`; `IEmailSender.SendAsync(EmailMessage)`; `IUnitOfWork.CommitAsync()`; `Publication.TeacherVersions` (`TeacherId`, `Version`); `WeekStart.WeekNumber`; `PublicationId.Of(Guid)`, `TeacherId.Of(Guid)`. From tasks 1–3: `IOutbox`, `IIntegrationEvent`, `IIntegrationEventConsumer<T>`, `AddIntegrationEvent`, `AddIntegrationEventConsumer`.
- Produces:
  - `record PublicationClosedIntegrationEvent(Guid PublicationId, IReadOnlyList<ClosedTeacherVersion> Teachers) : IIntegrationEvent` with `const string Name = "publication-closed.v1"`; `record ClosedTeacherVersion(Guid TeacherId, int Version)`.
  - `PublicationClosedHandler(IPublicationRepository, IOutbox)`: one `PublicationClosedIntegrationEvent` per close, inside the close's transaction.
  - `SendTeacherExcelEmailsConsumer`: the US-44 email, once per (Publication, Teacher, version); throws `TeacherExcelEmailFailedException` after trying every Teacher when any failed.
  - `ITeacherExcelEmailLog.WasSentAsync(PublicationId, TeacherId, int) : Task<bool>`, `.RecordSent(PublicationId, TeacherId, int, DateTimeOffset)`; table `sent_teacher_excel_emails(publication_id, teacher_id, version, sent_at_utc)`, primary key `(publication_id, teacher_id, version)`.

After this task a close writes one outbox message and one delivery, and the outbox job sends the emails a few seconds later.

- [ ] **Step 1: Move the US-44 tests to the consumer**

```bash
mkdir -p tests/DrivingLessons.Application.Test/Consumers
git mv tests/DrivingLessons.Application.Test/EventHandlers/PublicationClosedHandlerTest.cs tests/DrivingLessons.Application.Test/Consumers/SendTeacherExcelEmailsConsumerTest.cs
```

In the moved file:

1. Usings: add `using DrivingLessons.Application.Common;`, `using DrivingLessons.Application.Consumers.TeacherExcelEmail;`, `using DrivingLessons.Application.IntegrationEvents.Publications;`. Remove `using DrivingLessons.Application.EventHandlers;` and `using DrivingLessons.Domain.Events;`.
2. Namespace `DrivingLessons.Application.Test.Consumers`; class `SendTeacherExcelEmailsConsumerTest`.
3. Replace the fields and `Init()` with:

```csharp
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
```

`WasSentAsync` is configured to `false` explicitly: an unconfigured `Task<bool>` returns `false` anyway, but stating it keeps the guard visible.

4. In every test, replace `await handler.HandleAsync(new PublicationClosed(publication.Id));` with `await consumer.ConsumeAsync(EventFor(publication));`.
5. In `A_Failed_Send_Is_Logged_And_The_Next_Teacher_Still_Gets_Their_File` and `A_Failed_Excel_Is_Logged_And_The_Next_Teacher_Still_Gets_Their_File`, the consumer now throws after trying everyone (README decision 10). Replace the `//when` line with:

```csharp
        var act = () => consumer.ConsumeAsync(EventFor(publication));

        //then
        await act.ShouldThrowAsync<TeacherExcelEmailFailedException>();
```

and keep the two existing `//then` assertions below it (drop the old `//then` marker line).
6. Rename `Sends_Nothing_When_The_Publication_Is_Gone` and replace its `//when` line with `await consumer.ConsumeAsync(new PublicationClosedIntegrationEvent(publicationId.Value, [new ClosedTeacherVersion(Guid.NewGuid(), 1)]));`. Keep its `GetAsync(publicationId)` → `null` arrangement and `sent.ShouldBeEmpty()`.
7. Add this helper next to `Closed(...)`:

```csharp
    private static PublicationClosedIntegrationEvent EventFor(Publication publication)
    {
        var teachers = publication.TeacherVersions
                                  .Select(x => new ClosedTeacherVersion(x.TeacherId.Value, x.Version))
                                  .ToList();

        return new PublicationClosedIntegrationEvent(publication.Id.Value, teachers);
    }
```

8. Add these tests (before the helpers):

```csharp
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
```

`ShouldThrowAsync<T>` returns the exception, so `exception.TeacherIds` can be checked.

- [ ] **Step 2: Write the new handler tests**

Create `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.EventHandlers;

[TestClass]
public class PublicationClosedHandlerTest
{
    private IPublicationRepository publicationRepository = null!;
    private IOutbox outbox = null!;
    private PublicationClosedHandler handler = null!;
    private List<IIntegrationEvent> queued = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        outbox = A.Fake<IOutbox>();
        handler = new PublicationClosedHandler(publicationRepository, outbox);
        queued = [];

        A.CallTo(() => outbox.Add(A<IIntegrationEvent>._))
            .Invokes((IIntegrationEvent integrationEvent) => queued.Add(integrationEvent));
    }

    [TestMethod]
    public async Task Queues_The_Close_With_Each_Teachers_Excel_Version()
    {
        //given
        var cohen = TeacherId.New();
        var levi = TeacherId.New();
        var publication = Closed(cohen, levi);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var closed = queued.ShouldHaveSingleItem().ShouldBeOfType<PublicationClosedIntegrationEvent>();
        closed.PublicationId.ShouldBe(publication.Id.Value);
        closed.Teachers.ShouldBe(
            [
                new ClosedTeacherVersion(cohen.Value, 1),
                new ClosedTeacherVersion(levi.Value, 1)
            ]);
    }

    [TestMethod]
    public async Task Queues_The_Next_Version_After_A_Reopen_And_Close()
    {
        //given
        var teacherId = TeacherId.New();
        var publication = Closed(teacherId);
        publication.Reopen(DateTimeOffset.UtcNow.AddDays(5));
        publication.Close([teacherId]);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var closed = queued.ShouldHaveSingleItem().ShouldBeOfType<PublicationClosedIntegrationEvent>();
        closed.Teachers.ShouldBe([new ClosedTeacherVersion(teacherId.Value, 2)]);
    }

    [TestMethod]
    public async Task Queues_Nothing_When_The_Publication_Is_Gone()
    {
        //given
        var publicationId = PublicationId.New();

        A.CallTo(() => publicationRepository.GetAsync(publicationId))
            .Returns((Publication?)null);

        //when
        await handler.HandleAsync(new PublicationClosed(publicationId));

        //then
        queued.ShouldBeEmpty();
    }

    private Publication Closed(params TeacherId[] teacherIds)
    {
        var publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        var startUtc = DateTimeOffset.UtcNow.AddDays(-1);
        publication.Publish(SubmissionWindow.Of(startUtc, startUtc.AddHours(1)));
        publication.Open();
        publication.Close(teacherIds);

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);

        return publication;
    }
}
```

- [ ] **Step 3: Run the tests to confirm they fail to compile**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~PublicationClosedHandlerTest|FullyQualifiedName~SendTeacherExcelEmailsConsumerTest"
```

Expected: build errors for the missing integration event, consumer, log and exception types.

- [ ] **Step 4: Create the event, the log port and the exception**

`src\DrivingLessons.Application\IntegrationEvents\Publications\PublicationClosedIntegrationEvent.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents.Publications;

public record PublicationClosedIntegrationEvent(Guid PublicationId, IReadOnlyList<ClosedTeacherVersion> Teachers) : IIntegrationEvent
{
    public const string Name = "publication-closed.v1";
}
```

`src\DrivingLessons.Application\IntegrationEvents\Publications\ClosedTeacherVersion.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents.Publications;

public record ClosedTeacherVersion(Guid TeacherId, int Version);
```

Raw `Guid`s here are deliberate: an integration event is a serialized contract, like a response DTO (README decision 15, intent §5.1). Consumers turn them back into typed IDs with `.Of()` before reaching the domain (CLAUDE.md rule 2).

`src\DrivingLessons.Application\Abstractions\ITeacherExcelEmailLog.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Abstractions;

public interface ITeacherExcelEmailLog
{
    Task<bool> WasSentAsync(PublicationId publicationId, TeacherId teacherId, int version);

    void RecordSent(PublicationId publicationId, TeacherId teacherId, int version, DateTimeOffset sentAtUtc);
}
```

`src\DrivingLessons.Application\Consumers\TeacherExcelEmail\TeacherExcelEmailFailedException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Consumers.TeacherExcelEmail;

public class TeacherExcelEmailFailedException : Exception
{
    public PublicationId PublicationId { get; }

    public IReadOnlyList<TeacherId> TeacherIds { get; }

    public TeacherExcelEmailFailedException(PublicationId publicationId, IReadOnlyList<TeacherId> teacherIds)
        : base($"Excel email failed for [{teacherIds.Count}] teacher(s) of publication [{publicationId.Value}].")
    {
        PublicationId = publicationId;
        TeacherIds = teacherIds;
    }
}
```

It is not a `DomainException`: it never reaches HTTP, it only tells the outbox to retry. Its message carries a count and a GUID, no name or address, because the processor stores it in `last_error` (README decision 14).

- [ ] **Step 5: Create the consumer and slim the handler**

`src\DrivingLessons.Application\Consumers\TeacherExcelEmail\SendTeacherExcelEmailsConsumer.cs`. The subject, body and attachment-name code is moved **unchanged** from `PublicationClosedHandler` (US-44):

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.Extensions.Logging;

namespace DrivingLessons.Application.Consumers.TeacherExcelEmail;

public class SendTeacherExcelEmailsConsumer : IIntegrationEventConsumer<PublicationClosedIntegrationEvent>
{
    private const string AttachmentExtension = ".xlsx";
    private const char FileNameReplacement = '-';

    private static readonly char[] UnsafeFileNameCharacters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;
    private readonly ITeacherExcelEmailLog emailLog;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SendTeacherExcelEmailsConsumer> logger;

    public SendTeacherExcelEmailsConsumer(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender,
        ITeacherExcelEmailLog emailLog,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SendTeacherExcelEmailsConsumer> logger)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
        this.emailLog = emailLog;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task ConsumeAsync(PublicationClosedIntegrationEvent integrationEvent)
    {
        var publicationId = PublicationId.Of(integrationEvent.PublicationId);
        var publication = await repository.GetAsync(publicationId);

        if (publication is null)
        {
            return;
        }

        var failedTeacherIds = new List<TeacherId>();

        foreach (var closed in integrationEvent.Teachers)
        {
            var teacherId = TeacherId.Of(closed.TeacherId);

            try
            {
                await SendOnceAsync(publication, teacherId, closed.Version);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Excel email for teacher [{TeacherId}] of publication [{PublicationId}] failed.",
                    teacherId.Value,
                    publicationId.Value);

                failedTeacherIds.Add(teacherId);
            }
        }

        if (failedTeacherIds.Any())
        {
            throw new TeacherExcelEmailFailedException(publicationId, failedTeacherIds);
        }
    }

    private async Task SendOnceAsync(Publication publication, TeacherId teacherId, int version)
    {
        var alreadySent = await emailLog.WasSentAsync(publication.Id, teacherId, version);

        if (alreadySent)
        {
            return;
        }

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            return;
        }

        var week = publication.WeekStart.WeekNumber;
        var teacherName = teacher.Name.Value;

        var subject = Subject(week, teacherName, version);
        var body = Body(week, teacherName, version);

        var excel = await excelGenerator.GenerateAsync(publication.Id, teacherId);
        var attachment = excel with { FileName = AttachmentFileName(subject) };

        var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, attachment);

        await emailSender.SendAsync(message);

        var sentAtUtc = timeProvider.GetUtcNow();
        emailLog.RecordSent(publication.Id, teacherId, version, sentAtUtc);

        await unitOfWork.CommitAsync();
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
        var safeCharacters = subject.Select(SafeFileNameCharacter);
        var safeName = string.Concat(safeCharacters);

        return safeName + AttachmentExtension;
    }

    private static char SafeFileNameCharacter(char character)
    {
        return UnsafeFileNameCharacters.Contains(character)
            ? FileNameReplacement
            : character;
    }
}
```

Before saving, diff `Subject`, `Body`, `AttachmentFileName` and `SafeFileNameCharacter` against the handler on `main`: they must be identical. The broad `catch (Exception)` is the US-44 per-Teacher isolation, kept as is.

Replace `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs` with:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private readonly IPublicationRepository repository;
    private readonly IOutbox outbox;

    public PublicationClosedHandler(IPublicationRepository repository, IOutbox outbox)
    {
        this.repository = repository;
        this.outbox = outbox;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        var teachers = publication.TeacherVersions
                                  .Select(x => new ClosedTeacherVersion(x.TeacherId.Value, x.Version))
                                  .ToList();
        var publicationId = publication.Id.Value;

        var closed = new PublicationClosedIntegrationEvent(publicationId, teachers);
        outbox.Add(closed);
    }
}
```

`repository.GetAsync` returns the Publication the close interactor already tracks, with the versions the close just incremented. `TeacherVersions` also lists a Teacher who had a version on an earlier close but no Week Schedule now. Today that Teacher gets their old version again on every close; with the guard (`WasSentAsync`) the old version is already recorded and skipped. That's a small improvement, not a behavior to preserve.

- [ ] **Step 6: Register the event and the consumer**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.Consumers.TeacherExcelEmail;` and `using DrivingLessons.Application.IntegrationEvents.Publications;`, and after the two `IDomainEventHandler` registrations:

```csharp
        services.AddIntegrationEvent<PublicationClosedIntegrationEvent>(PublicationClosedIntegrationEvent.Name);
        services.AddIntegrationEventConsumer<PublicationClosedIntegrationEvent, SendTeacherExcelEmailsConsumer>();
```

`TimeProvider` is already registered (`AddInfrastructure`), so the consumer resolves.

- [ ] **Step 7: Store the sent emails**

`src\DrivingLessons.Infrastructure\Email\SentTeacherExcelEmail.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Email;

public class SentTeacherExcelEmail
{
    public Guid PublicationId { get; private set; }

    public Guid TeacherId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset SentAtUtc { get; private set; }

    private SentTeacherExcelEmail()
    {
    }

    private SentTeacherExcelEmail(Guid publicationId, Guid teacherId, int version, DateTimeOffset sentAtUtc)
    {
        PublicationId = publicationId;
        TeacherId = teacherId;
        Version = version;
        SentAtUtc = sentAtUtc;
    }

    public static SentTeacherExcelEmail Create(Guid publicationId, Guid teacherId, int version, DateTimeOffset sentAtUtc)
    {
        return new SentTeacherExcelEmail(publicationId, teacherId, version, sentAtUtc);
    }
}
```

`src\DrivingLessons.Infrastructure\Email\TeacherExcelEmailLog.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Email;

public class TeacherExcelEmailLog : ITeacherExcelEmailLog
{
    private readonly DrivingLessonsDbContext context;

    public TeacherExcelEmailLog(DrivingLessonsDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> WasSentAsync(PublicationId publicationId, TeacherId teacherId, int version)
    {
        var publicationGuid = publicationId.Value;
        var teacherGuid = teacherId.Value;

        return await context.SentTeacherExcelEmails.AnyAsync(
            x => x.PublicationId == publicationGuid && x.TeacherId == teacherGuid && x.Version == version);
    }

    public void RecordSent(PublicationId publicationId, TeacherId teacherId, int version, DateTimeOffset sentAtUtc)
    {
        var sent = SentTeacherExcelEmail.Create(publicationId.Value, teacherId.Value, version, sentAtUtc);

        context.SentTeacherExcelEmails.Add(sent);
    }
}
```

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\SentTeacherExcelEmailConfiguration.cs`:

```csharp
using DrivingLessons.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class SentTeacherExcelEmailConfiguration : IEntityTypeConfiguration<SentTeacherExcelEmail>
{
    public void Configure(EntityTypeBuilder<SentTeacherExcelEmail> builder)
    {
        builder.ToTable("sent_teacher_excel_emails");

        builder
            .Property(x => x.PublicationId)
            .HasColumnName("publication_id");

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id");

        builder
            .Property(x => x.Version)
            .HasColumnName("version");

        builder
            .Property(x => x.SentAtUtc)
            .HasColumnName("sent_at_utc")
            .HasColumnType("timestamptz");

        builder.HasKey(x => new { x.PublicationId, x.TeacherId, x.Version });
    }
}
```

No foreign keys to `publications` or `teachers`: this is a delivery record, kept even if those rows change, and it never blocks a Teacher's soft delete.

In `DrivingLessonsDbContext.cs`, add `using DrivingLessons.Infrastructure.Email;` and after the outbox `DbSet`s:

```csharp
    public DbSet<SentTeacherExcelEmail> SentTeacherExcelEmails => Set<SentTeacherExcelEmail>();
```

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, after `services.AddScoped<IEmailSender, SmtpEmailSender>();`:

```csharp
        services.AddScoped<ITeacherExcelEmailLog, TeacherExcelEmailLog>();
```

- [ ] **Step 8: Generate and read the migration**

```bash
dotnet ef migrations add AddSentTeacherExcelEmails --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

The migration must create only `sent_teacher_excel_emails` with the composite primary key. No backfill: emails sent before this slice have no row, and no outbox event exists for them, so nothing will try to send them again.

- [ ] **Step 9: Run the tests**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~PublicationClosedHandlerTest|FullyQualifiedName~SendTeacherExcelEmailsConsumerTest"
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: handler 3 passed; consumer: the 13 moved test runs plus the 5 new ones pass; full suites pass.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/IntegrationEvents/Publications src/DrivingLessons.Application/Abstractions/ITeacherExcelEmailLog.cs src/DrivingLessons.Application/Consumers src/DrivingLessons.Application/EventHandlers/PublicationClosedHandler.cs src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Infrastructure/Email/SentTeacherExcelEmail.cs src/DrivingLessons.Infrastructure/Email/TeacherExcelEmailLog.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/SentTeacherExcelEmailConfiguration.cs src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs src/DrivingLessons.Infrastructure/EntityFramework/Migrations src/DrivingLessons.Infrastructure/DependencyInjection.cs tests/DrivingLessons.Application.Test/Consumers tests/DrivingLessons.Application.Test/EventHandlers/PublicationClosedHandlerTest.cs
git commit -m "feat(notifications): the teacher's Excel email is sent from the outbox, once per version"
```
