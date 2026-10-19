# Task 8 of 18: The `StudentNotification` aggregate, recipients and quiet hours

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires task 5 committed (task 7 optional).

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\StudentNotification.cs`
- Create: `src\DrivingLessons.Domain\Values\StudentNotificationId.cs`, `NotificationKind.cs`, `NotificationState.cs`, `NotificationSkipReason.cs`, `NotificationTrigger.cs`, `SchoolTimeZone.cs`, `QuietHours.cs`
- Create: `src\DrivingLessons.Domain\Events\StudentNotificationCreated.cs`, `StudentNotificationSent.cs`, `StudentNotificationFailed.cs`, `StudentNotificationSkipped.cs`, `StudentNotificationSuperseded.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentNotificationMustBePendingException.cs`, `NotificationTriggerMustNotBeEmptyException.cs`, `QuietHoursMustBeValidException.cs`
- Create: `src\DrivingLessons.Domain\Repositories\IStudentNotificationRepository.cs`
- Create: `src\DrivingLessons.Application\Queries\IStudentNotificationQueries.cs` (+ `NotificationRecipient` record)
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\StudentNotificationConfiguration.cs` (+ converters `StudentNotificationIdConverter`, `NotificationTriggerConverter`)
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentNotificationRepository.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentNotificationQueries.cs`
- Create: `src\DrivingLessons.Infrastructure\Options\NotificationOptions.cs` (binds `Notifications:QuietHours`, registers `QuietHours` as a singleton)
- Modify: `DrivingLessonsDbContext.cs` (`DbSet<StudentNotification>`), `Infrastructure\DependencyInjection.cs`, `appsettings.json`
- Create: migration `{timestamp}_AddStudentNotifications`
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentNotificationTest.cs`, `Values\NotificationTriggerTest.cs`, `Values\QuietHoursTest.cs`, `Values\SchoolTimeZoneTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Notifications\NotificationOptionsTest.cs` (new)
- Modify: `docs\requirements.md`, `CONTEXT.md`

**Interfaces:**
- Consumes (task 5): `PhoneNumber.ToWhatsAppNumber()`, `Student.HasWhatsAppConsent`; on `main`: `WeekSchedules` by `WeekStart`, `Students.IsActive`, `Submissions.PublicationId / StudentId`.
- Produces:
  - `StudentNotification.Plan(Publication publication, Student student, NotificationKind kind, NotificationTrigger trigger, DateTimeOffset createdAtUtc)`, which returns a `Pending` notification, or a `Skipped` one when the Student has no consent or no WhatsApp number. Raises `StudentNotificationCreated`, plus `StudentNotificationSkipped` when skipped.
  - `.MarkSent(string providerMessageId, DateTimeOffset sentAtUtc)` · `.MarkFailed(string reason)` · `.MarkSkipped(NotificationSkipReason reason)` · `.Supersede()`, each with `MustBePending`.
  - `NotificationTrigger.Opening()` · `.WindowChange(Guid changeId)` · `.Reminder(Guid requestId)` · `.ScheduledReminder(DateTimeOffset checkUtc)` · `.Of(string)`, plus `.Value`.
  - `SchoolTimeZone.ToLocal(DateTimeOffset instantUtc) : DateTime` and `.ToUtc(DateOnly, TimeOnly) : DateTimeOffset` for Asia/Jerusalem.
  - `QuietHours.Of(TimeOnly nightFrom, TimeOnly nightTo, DayOfWeek weeklyFromDay, TimeOnly weeklyFrom, DayOfWeek weeklyToDay, TimeOnly weeklyTo)`, `.Contains(DateTimeOffset instantUtc) : bool`, `.EndAfter(DateTimeOffset instantUtc) : DateTimeOffset`.
  - `IStudentNotificationRepository`: `GetAsync(StudentNotificationId)`, `FindForTriggerAsync(PublicationId, NotificationTrigger)`, `FindPendingWindowChangesAsync(PublicationId)`, `Add(StudentNotification)`.
  - `IStudentNotificationQueries.FindRecipientIdsAsync(Guid publicationId) : Task<IReadOnlyList<Guid>>`, the Student IDs of intent §4.1.

- [ ] **Step 1: Write the failing value tests**

`NotificationTriggerTest`: `Opening_Is_Opening` (`"opening"`), `Window_Change_Carries_Its_Id` (`"window-change:{guid}"`), `Reminder_Carries_Its_Request` (`"reminder:{guid}"`), `Scheduled_Reminder_Carries_The_Check_Instant` (`"scheduled-reminder:2026-10-21T07:00:00.0000000+00:00"`, round-trip `O` of the UTC instant), `Must_Not_Be_Empty` `[DataRow("")] [DataRow("  ")]`.

`SchoolTimeZoneTest`: `To_Local` in summer (`2026-10-21T07:00Z` → `10:00`, UTC+3) and winter (`2026-12-02T08:00Z` → `10:00`, UTC+2); `To_Utc` the reverse; `To_Utc_Across_The_Change_Night` (25 October 2026 is the change back to winter time in Israel; pick 10:00 local before and after it).

`QuietHoursTest` with the defaults (21:00–08:00 nightly; Friday 15:00 → Saturday 21:00):

| Instant (local) | `Contains` | `EndAfter` (local) |
|-----------------|-----------|--------------------|
| Wed 20:59 | false | — |
| Wed 21:00 | true | Thu 08:00 |
| Thu 07:59 | true | Thu 08:00 |
| Thu 08:00 | false | — |
| Fri 14:59 | false | — |
| Fri 15:00 | true | **Sun 08:00** (Shabbat runs into the night, then the night runs to 08:00) |
| Sat 21:30 | true | Sun 08:00 |
| Sun 08:00 | false | — |

`QuietHours_Must_Be_Valid`: the weekly end must come after its start within the week → `QuietHoursMustBeValidException`.

- [ ] **Step 2: Implement the values**

`SchoolTimeZone` is a static class holding `TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem")` once, with the two conversions (`TimeZoneInfo.ConvertTime`, and `ConvertTimeToUtc` on an `Unspecified` `DateTime`). `QuietHours.EndAfter` walks forward: while the instant is inside a quiet range, jump to that range's end, and repeat. That is what makes Friday 15:00 resolve to Sunday 08:00 and not Saturday 21:00. Run the domain value tests: PASS.

- [ ] **Step 3: Write the failing aggregate tests**

`StudentNotificationTest` builds real objects: an Open `Publication`, and a `Student` built with `Student.Create(…)` (the existing domain fake builders do this) and `GiveWhatsAppConsent`.

| Test | Asserts |
|------|---------|
| `Plan` | `Pending`, kind, trigger, `PublicationId`, `StudentId`, `CreatedAtUtc` |
| `Plan__Add_Event` | `StudentNotificationCreated(Id, PublicationId, StudentId, Kind, Trigger)` |
| `Plan__Skips_A_Student_Without_Consent` | `Skipped`, `SkipReason == NoConsent`, events Created + Skipped |
| `Plan__Skips_A_Student_Without_A_WhatsApp_Number` | phone `02-6251234` → `Skipped InvalidNumber` |
| `Plan__No_Consent_Wins_Over_An_Invalid_Number` | both missing → `NoConsent` (the reason an Administrator can act on first) |
| `Mark_Sent` / `__Add_Event` | `Sent`, `ProviderMessageId`, `SentAtUtc` |
| `Mark_Failed` / `__Add_Event` | `Failed`, `FailureReason` |
| `Mark_Skipped` / `__Add_Event` | `Skipped`, reason |
| `Supersede` / `__Add_Event` | `Superseded` |
| `State_Change__Must_Be_Pending` `[DataRow]` over Sent, Failed, Skipped, Superseded × each of the four methods | `StudentNotificationMustBePendingException` |

- [ ] **Step 4: Implement the aggregate**

```csharp
public class StudentNotification : AggregateRoot<StudentNotificationId>
{
    public PublicationId PublicationId { get; private set; }
    public StudentId StudentId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public NotificationTrigger Trigger { get; private set; }
    public NotificationState State { get; private set; }
    public NotificationSkipReason? SkipReason { get; private set; }
    public string? FailureReason { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public bool IsPending => State is NotificationState.Pending;

    public static StudentNotification Plan(
        Publication publication,
        Student student,
        NotificationKind kind,
        NotificationTrigger trigger,
        DateTimeOffset createdAtUtc)
    {
        var id = StudentNotificationId.New();
        var skipReason = SkipReasonFor(student);
        var state = skipReason is null ? NotificationState.Pending : NotificationState.Skipped;

        return new StudentNotification(id, publication.Id, student.Id, kind, trigger, state, skipReason, createdAtUtc);
    }
}
```

The constructor raises `StudentNotificationCreated`, plus `StudentNotificationSkipped` when the state is `Skipped`. `SkipReasonFor` checks `!student.HasWhatsAppConsent` → `NoConsent`, then `student.Phone.ToWhatsAppNumber() is null` → `InvalidNumber`, and returns `null` otherwise. The four transitions follow guards → mutation → event. `FailureReason` is cut to 500 characters inside `MarkFailed`, so that rule lives in the domain.

Enums: `NotificationKind { Opening = 10, WindowChange = 20, Reminder = 30 }`, `NotificationState { Pending = 10, Sent = 20, Failed = 30, Skipped = 40, Superseded = 50 }`, `NotificationSkipReason { NoConsent = 10, InvalidNumber = 20, WhatsAppDisabled = 30, AlreadySubmitted = 40, WindowClosed = 50 }`. `WindowClosed` covers a held message whose window closed before quiet hours ended (task 10).

- [ ] **Step 5: Persist, query and configure**

`StudentNotificationConfiguration`: table `student_notifications`; snake_case columns; `trigger` `text`; `failure_reason` `text`; `provider_message_id` `text`; unique index `(publication_id, student_id, kind, trigger)` named `ux_student_notifications_once`; index `(publication_id, kind, state)`. No FK to `students` or `publications`: the aggregates reference each other by ID only, as `submissions` does. Check `SubmissionConfiguration` and follow it exactly if it does declare FKs.

`StudentNotificationRepository`: `FindAsync` for the key, `Where(…).ToListAsync()` for the two lists, in `CreatedAtUtc` order.

`StudentNotificationQueries.FindRecipientIdsAsync` (decision 10):

```csharp
var resolvedId = PublicationId.Of(publicationId);

var weekStart = await dbContext.Publications
                               .Where(x => x.Id == resolvedId)
                               .Select(x => x.WeekStart)
                               .FirstAsync();

var teacherIds = dbContext.WeekSchedules
                          .Where(x => x.WeekStart == weekStart)
                          .Select(x => x.TeacherId);

return await dbContext.Students
                      .Where(x => x.IsActive && teacherIds.Contains(x.TeacherId))
                      .OrderBy(x => x.Id)
                      .Select(x => x.Id.Value)
                      .ToListAsync();
```

Consent and the number are **not** filtered here: excluded Students are recorded as skipped (decision 10), so the summary can list them.

`NotificationOptions` (`SectionName = "Notifications"`) has a nested `QuietHours` section with six strings (`NightFrom "21:00"`, `NightTo "08:00"`, `WeeklyFromDay "Friday"`, `WeeklyFrom "15:00"`, `WeeklyToDay "Saturday"`, `WeeklyTo "21:00"`), validated at start-up. Registration: `services.AddSingleton(sp => sp.GetRequiredService<IOptions<NotificationOptions>>().Value.ToQuietHours())`. `NotificationOptionsTest`: `Defaults_Build_The_Quiet_Hours_Of_Decision_16`, `An_Unparsable_Time_Fails_Validation`.

```bash
dotnet ef migrations add AddStudentNotifications --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

- [ ] **Step 6: Record the terms**

`CONTEXT.md`: a new `### Notifications` section with the glossary of decision 25. One line each, `_Avoid_:` lines as in the file:
- **Recipient**: an active Student whose Teacher has a Week Schedule in the Publication. _Avoid_: subscriber, audience.
- **WhatsApp Consent**: the Student's recorded agreement to WhatsApp messages about their weekly submission. _Avoid_: opt-in flag, permission.
- **Student Notification**: one WhatsApp message to one Student for one Publication, kind and trigger, with its outcome. _Avoid_: message blast, broadcast, push.
- **Opening Notification**, **Window Change Notification**, **Submission Reminder** (manual or scheduled), **Quiet Hours**.

`docs\requirements.md` (intent §6):
- §3 in scope: "WhatsApp notifications to Students: the weekly link when the window opens, the new end time when it changes, and reminders to Students who haven't submitted".
- §6.2 step 4: "The system sends the link on WhatsApp to every Recipient with WhatsApp Consent when the window opens; it can still be shared by hand."
- §6.3: a "Reminders" bullet (filled in by task 15).
- A new §5.9 **Student Notification**, summing up decisions 10–17.
- §11: decision **#26**, "Students are notified on WhatsApp through approved Utility templates, only with WhatsApp Consent, never during Quiet Hours; every side effect goes through the outbox (ADR 0010, ADR 0011)". Decision #13 is narrowed to "Booking and notification of the final schedule are out of scope".
- §12 deferred: delivery and read statuses, opt-out by reply.

- [ ] **Step 7: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
git add src tests docs/requirements.md CONTEXT.md
git commit -m "feat(notifications): student notification aggregate, recipients and quiet hours"
```
