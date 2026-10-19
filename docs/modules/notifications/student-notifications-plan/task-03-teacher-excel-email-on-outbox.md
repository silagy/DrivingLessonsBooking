# Task 3 of 18: The teacher's Excel email moves onto the outbox

> Part of [Student Notifications (stage 1)](README.md), milestone A. Work on branch `notifications-outbox`, commands from the repo root in bash. Requires tasks 1–2 committed.

**Files:**
- Modify: `src\DrivingLessons.Domain\Entities\TeacherExcelVersion.cs` (`EmailedAtUtc`, `IsEmailed`, `RecordEmailed`, cleared by `Increment`)
- Modify: `src\DrivingLessons.Domain\Entities\Publication.cs` (`RecordExcelEmailed`)
- Create: `src\DrivingLessons.Domain\Events\PublicationExcelEmailed.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PublicationTeacherExcelVersionMustExistException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PublicationExcelAlreadyEmailedException.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\PublicationClosedIntegrationEvent.cs`
- Modify: `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs` (maps only)
- Create: `src\DrivingLessons.Application\Notifications\TeacherExcelEmail.cs` (subject, body and file name, moved verbatim)
- Create: `src\DrivingLessons.Application\Consumers\TeacherExcelEmailConsumer.cs`
- Create: `src\DrivingLessons.Application\Consumers\TeacherExcelEmailsFailedException.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\PublicationConfiguration.cs` (`emailed_at_utc`)
- Create: migration `{timestamp}_AddTeacherExcelEmailedAt`
- Test: `tests\DrivingLessons.Domain.Test\Entities\PublicationTest.cs` (new cases)
- Test: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs` (rewritten)
- Test: `tests\DrivingLessons.Application.Test\Consumers\TeacherExcelEmailConsumerTest.cs` (new; takes over every US-44 email test)
- Modify: `docs\decisions\0005-email-over-ses-smtp.md` (amendment), `docs\requirements.md` (§6.4, §9 Delivery), `docs\modules\excel\us-44-versioned-excel-email-plan\README.md` (Open Item 1 → resolved, link here)

**Interfaces:**
- Consumes: task 1–2 outbox (`IIntegrationEventOutbox`, `IIntegrationEventConsumer<T>`, `AddIntegrationEventConsumer`, `DeliveryAttempt`); on `main`: `IPublicationRepository.GetAsync`, `ITeacherRepository.GetAsync` (null for a soft-deleted Teacher), `IExcelGenerator.GenerateAsync(PublicationId, TeacherId)`, `IEmailSender.SendAsync(EmailMessage)`, `IUnitOfWork.CommitAsync`, `TimeProvider`.
- Produces:
  - `Publication.RecordExcelEmailed(TeacherId teacherId, DateTimeOffset emailedAtUtc)`, which raises `PublicationExcelEmailed(PublicationId, TeacherId, int Version)`.
  - `TeacherExcelVersion.EmailedAtUtc : DateTimeOffset?` and `IsEmailed`. Task 13's summary reads them.
  - `record PublicationClosedIntegrationEvent(Guid PublicationId, DateOnly WeekStart, IReadOnlyList<TeacherVersionForPublicationClosedIntegrationEvent> Teachers, DateTimeOffset ClosedAtUtc)` with `EventType "publication-closed"`, v1, and `record TeacherVersionForPublicationClosedIntegrationEvent(Guid TeacherId, int ExcelVersion)`.
  - `TeacherExcelEmailConsumer : IIntegrationEventConsumer<PublicationClosedIntegrationEvent>`.

After this task `IEmailSender` has a single caller, the consumer, and the close no longer waits on SMTP.

- [ ] **Step 1: Write the failing domain tests**

Add to `PublicationTest` (closed publications built through `Publish → Open → Close([teacherId])` as today):

| Test | Asserts |
|------|---------|
| `Record_Excel_Emailed` | the Teacher's version `IsEmailed`, `EmailedAtUtc == at` |
| `Record_Excel_Emailed__Add_Event` | `PublicationExcelEmailed(Id, teacherId, 1)` |
| `Record_Excel_Emailed__Must_Have_Excel_Version_For_Teacher` | another `TeacherId` → `PublicationTeacherExcelVersionMustExistException` |
| `Record_Excel_Emailed__Must_Not_Be_Emailed_Already` | twice → `PublicationExcelAlreadyEmailedException` |
| `Close__Clears_The_Emailed_Mark_When_The_Version_Goes_Up` | emailed v1 → `Reopen` → `Close` → v2, `IsEmailed` false |

- [ ] **Step 2: Implement the domain change**

`TeacherExcelVersion`: `public DateTimeOffset? EmailedAtUtc { get; private set; }`, `public bool IsEmailed => EmailedAtUtc is not null;`, `internal void RecordEmailed(DateTimeOffset emailedAtUtc)`, and `Increment()` also sets `EmailedAtUtc = null`.

`Publication.RecordExcelEmailed` follows guards → mutation → event:

```csharp
    public void RecordExcelEmailed(TeacherId teacherId, DateTimeOffset emailedAtUtc)
    {
        var teacherVersion = MustHaveExcelVersionFor(teacherId);
        ExcelMustNotBeEmailed(teacherVersion);

        teacherVersion.RecordEmailed(emailedAtUtc);

        AddEvent(new PublicationExcelEmailed(Id, teacherId, teacherVersion.Version));
    }
```

`MustHaveExcelVersionFor` returns the version or throws `PublicationTeacherExcelVersionMustExistException(Id, teacherId)`. `ExcelMustNotBeEmailed` throws `PublicationExcelAlreadyEmailedException(Id, teacherVersion.TeacherId, teacherVersion.Version)`. The messages follow the existing ones: `$"Publication {id.Value} has no Excel version for teacher {teacherId.Value}."`.

Run `dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~PublicationTest"`. Expected: PASS.

- [ ] **Step 3: Write the failing mapping test**

Rewrite `PublicationClosedHandlerTest`. The handler now takes `IPublicationRepository`, `IIntegrationEventOutbox` and `TimeProvider`:
- `Writes_The_Closed_Event_With_Every_Teachers_Version`: two Teachers → one `outbox.Add(A<PublicationClosedIntegrationEvent>…)` whose `Teachers` hold both `(TeacherId, 1)` in close order, plus `WeekStart` and `ClosedAtUtc` from the fake clock.
- `Writes_The_Version_After_A_Reopen_And_Close`: `ExcelVersion == 2`.
- `Writes_Nothing_When_The_Publication_Is_Gone`: the repository returns `null` → no `Add`.

- [ ] **Step 4: Write the failing consumer tests**

Create `tests\DrivingLessons.Application.Test\Consumers\TeacherExcelEmailConsumerTest.cs`. **Move** every test of the old `PublicationClosedHandlerTest` here unchanged in what it asserts: subject, body, attachment name, unsafe characters, own file per Teacher, deleted Teacher skipped, missing publication. Invoke them through `consumer.HandleAsync(Event(publication), new DeliveryAttempt(1, false))`, where `Event(…)` builds the integration event from the publication's versions. The fake `IUnitOfWork` records commits. Then add:

| Test | Asserts |
|------|---------|
| `Records_The_Email_On_The_Teachers_Version` | after the send, `publication.TeacherVersions.Single().IsEmailed`, one commit |
| `Commits_After_Each_Teacher` | two Teachers → two commits, each after its send (`.Then(...)` ordering as in ADR 0007 tests) |
| `Skips_A_Teacher_Already_Emailed_For_This_Version` | Teacher already recorded → no send, no commit, `Done` |
| `Skips_A_Version_Superseded_By_A_Later_Close` | event says v1, publication is at v2 → no send |
| `A_Failed_Send_Throws_After_Every_Teacher_Was_Tried` | Cohen throws `IOException` → Levi still sent and recorded, then `TeacherExcelEmailsFailedException` (one failure) |
| `Retries_Only_The_Teachers_Not_Yet_Emailed` | run 1: Cohen fails, Levi sent; run 2 with the same publication object: only Cohen sent |
| `A_Failed_Excel_Is_Treated_Like_A_Failed_Send` | the generator throws for Cohen → Levi sent, then throws |
| `Logs_Each_Failed_Teacher_With_Ids_Only` | one Error per failed Teacher (the US-44 log line, unchanged) |

- [ ] **Step 5: Run the tests to see them fail**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~PublicationClosedHandlerTest|FullyQualifiedName~TeacherExcelEmailConsumerTest"`
Expected: FAIL, because `TeacherExcelEmailConsumer` and `PublicationClosedIntegrationEvent` don't exist yet.

- [ ] **Step 6: Implement the contract, the mapping and the consumer**

`TeacherExcelEmail` (static) holds `Subject(int week, string teacherName, int version)`, `Body(…)` and `AttachmentFileName(string subject)`, with the constants and `UnsafeFileNameCharacters`, cut from today's handler **without a single wording change**.

`PublicationClosedHandler.HandleAsync`:

```csharp
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        var teachers = publication
                       .TeacherVersions
                       .Select(x => new TeacherVersionForPublicationClosedIntegrationEvent(x.TeacherId.Value, x.Version))
                       .ToList();

        var closed = new PublicationClosedIntegrationEvent(
            publication.Id.Value,
            publication.WeekStart.Value,
            teachers,
            timeProvider.GetUtcNow());

        outbox.Add(closed);
```

`TeacherExcelEmailConsumer.HandleAsync`:
1. Load the publication (`PublicationId.Of(…)`). If it's gone → `Done`.
2. For each Teacher in the event, in order, inside `try/catch`:
   - find the publication's `TeacherExcelVersion` for that Teacher. If it is missing, or its `Version != ExcelVersion` (superseded), or `IsEmailed` → `continue`;
   - load the Teacher. If deleted → `continue`;
   - build the message exactly as US-44 did, then `await emailSender.SendAsync(message)`;
   - `publication.RecordExcelEmailed(teacherId, timeProvider.GetUtcNow())`, `await unitOfWork.CommitAsync()`.
   - `catch (Exception)` → the US-44 Error log line, `failures++`.
3. If `failures > 0` → `throw new TeacherExcelEmailsFailedException(publicationId, failures)`. Otherwise → `Done`.

Register in `Application\DependencyInjection.cs`: `services.AddIntegrationEventConsumer<PublicationClosedIntegrationEvent, TeacherExcelEmailConsumer>();`. The `IDomainEventHandler<PublicationClosed>` registration stays and points at the slimmer handler.

- [ ] **Step 7: Persist the emailed mark**

`PublicationConfiguration`, inside `OwnsMany(x => x.TeacherVersions, …)`: `versions.Property(v => v.EmailedAtUtc).HasColumnName("emailed_at_utc").HasColumnType("timestamptz");`.

```bash
dotnet ef migrations add AddTeacherExcelEmailedAt --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

Expected migration: one nullable `AddColumn` on `publication_teacher_versions`. Existing rows stay `NULL`: emails sent before this change were never recorded, and the summary (task 13) shows them as "not recorded" rather than "not sent".

- [ ] **Step 8: Run every suite**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Expected: all pass, no pending changes.

- [ ] **Step 9: Update the docs**

- ADR 0005: add an **Amendment (stage 1 notifications, ADR 0010)** section. US-44 decision 4 (catch, log, never retry) is replaced: the email is sent by `TeacherExcelEmailConsumer` after the close commits, retried with the outbox backoff for up to 8 attempts, and recorded per Teacher per version. A mail outage no longer costs the file, and the Download button stays the fallback for a dead delivery.
- `docs\requirements.md` §9 **Delivery**, *Automatic*: add "if the mail server is unreachable, the email is retried for several hours, and each Teacher receives each version at most once (barring a crash in mid-send)". §6.4 is unchanged in meaning.
- US-44 plan README, Open Item 1: append "Resolved by the [student notifications plan](../../notifications/student-notifications-plan/README.md), task 3."

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Domain src/DrivingLessons.Application src/DrivingLessons.Infrastructure/EntityFramework \
        tests/DrivingLessons.Domain.Test/Entities/PublicationTest.cs \
        tests/DrivingLessons.Application.Test/EventHandlers/PublicationClosedHandlerTest.cs \
        tests/DrivingLessons.Application.Test/Consumers \
        docs/decisions/0005-email-over-ses-smtp.md docs/requirements.md \
        docs/modules/excel/us-44-versioned-excel-email-plan/README.md
git commit -m "feat(outbox): the teacher's Excel email is sent by an outbox consumer, retried and recorded per version"
```
