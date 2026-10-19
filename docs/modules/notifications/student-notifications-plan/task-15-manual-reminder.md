# Task 15 of 18: The manual Submission Reminder and the reminder limits

> Part of [Student Notifications (stage 1)](README.md), milestone C. Work on branch `notifications-reminders` (from `origin/main` after PR B merged), commands from the repo root in bash.

**Files:**
- Modify: `src\DrivingLessons.Domain\Entities\Publication.cs` (`RequestReminder`)
- Create: `src\DrivingLessons.Domain\Events\PublicationReminderRequested.cs`
- Create: `src\DrivingLessons.Domain\Values\ReminderLimits.cs`, `ReminderHistory.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\ReminderLimitsMustBeValidException.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\SubmissionReminderRequestedIntegrationEvent.cs` (+ `ReminderSource` enum beside it)
- Create: `src\DrivingLessons.Application\EventHandlers\PublicationReminderRequestedHandler.cs`
- Create: `src\DrivingLessons.Application\Consumers\SubmissionReminderConsumer.cs`
- Create: `src\DrivingLessons.Application\Commands\RequestSubmissionReminder\RequestSubmissionReminderInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IStudentNotificationQueries.cs` (+ `FindReminderHistoryAsync`), `ISubmissionQueries.cs` (+ `FindStudentIdsWithSubmissionAsync`, `HasSubmissionAsync`), and their implementations
- Modify: `src\DrivingLessons.Infrastructure\Options\NotificationOptions.cs` (+ `Reminders:{FirstReminderDelayHours=4, MinimumGapHours=4, MaximumReminders=3}`, registers `ReminderLimits`)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationCommandController.cs` (+ `RequestReminder`)
- Modify: both `DependencyInjection.cs`, `appsettings.json`
- Test: `Domain.Test\Entities\PublicationTest.cs`, `Domain.Test\Values\ReminderLimitsTest.cs` (new)
- Test: `Application.Test\Commands\RequestSubmissionReminderInteractorTest.cs`, `Application.Test\EventHandlers\PublicationReminderRequestedHandlerTest.cs`, `Application.Test\Consumers\SubmissionReminderConsumerTest.cs` (new)
- Modify: `docs\requirements.md` §6.3

**Interfaces:**
- Consumes: task 10 (planner, sender with `isStillWanted`), task 8 (`NotificationTrigger.Reminder(Guid)`, `.ScheduledReminder(DateTimeOffset)`, `QuietHours`), task 11's consumer shape.
- Produces:
  - `Publication.RequestReminder()`, which guards `MustBeOpen` and raises `PublicationReminderRequested(PublicationId)`.
  - `ReminderLimits.Of(TimeSpan firstReminderDelay, TimeSpan minimumGap, int maximumReminders)`, `.MayRemind(ReminderHistory history, DateTimeOffset nowUtc) : bool`.
  - `record ReminderHistory(DateTimeOffset? OpeningSentAtUtc, DateTimeOffset? LastMessageAtUtc, int Reminders)`, with `ReminderHistory.None`.
  - `record SubmissionReminderRequestedIntegrationEvent(Guid PublicationId, ReminderSource Source, Guid RequestId, DateTimeOffset? CheckUtc, DateTimeOffset RequestedAtUtc)`, with `EventType "submission-reminder-requested"`, v1, and `enum ReminderSource { Manual = 10, Scheduled = 20 }`. Task 16 writes the `Scheduled` ones.
  - `POST api/publications/{id}/reminders` → **202** / 404 / 409 (Administrator).

- [ ] **Step 1: Write the failing domain tests**

`PublicationTest`: `Request_Reminder__Add_Event`; `Request_Reminder__Must_Be_Open` `[DataRow]` over Draft, Published, Closed → `PublicationMustBeOpenException` (Review Focus 12); `Request_Reminder_Twice_Raises_Two_Events` (no state changes, so a second request isn't a repeat of a transition, intent §4.4).

`ReminderLimitsTest` (limits 4 h / 4 h / 3, now = T):

| History | `MayRemind` |
|---------|-------------|
| `None` (nothing ever sent) | true |
| opening sent at T−3h59 | false (first-reminder delay) |
| opening sent at T−4h, last message T−4h | true |
| opening T−10h, last message (window change) at T−1h | false (gap counts every kind) |
| opening T−10h, last T−5h, 2 reminders | true |
| opening T−10h, last T−5h, 3 reminders | false (cap) |
| no opening (skipped back then), last message T−5h, 0 reminders | true |

`Must_Be_Valid`: a negative delay, a negative gap or a cap below 1 → `ReminderLimitsMustBeValidException`.

- [ ] **Step 2: Implement the domain change**

`RequestReminder()` has no mutation: guard, then event. `ReminderLimits.MayRemind`:

```csharp
        var openingIsRecent = history.OpeningSentAtUtc is { } openedAt && nowUtc - openedAt < FirstReminderDelay;
        var lastIsRecent = history.LastMessageAtUtc is { } lastAt && nowUtc - lastAt < MinimumGap;
        var capReached = history.Reminders >= MaximumReminders;

        return !openingIsRecent && !lastIsRecent && !capReached;
```

- [ ] **Step 3: Write the failing application tests**

`RequestSubmissionReminderInteractorTest`: `Requests_A_Reminder_And_Commits`, `Publication_Must_Exist`, `A_Closed_Publication_Is_Refused_And_Nothing_Is_Committed`.

`PublicationReminderRequestedHandlerTest`: `Writes_A_Manual_Request_With_A_Fresh_Request_Id`.

`SubmissionReminderConsumerTest` (real planner and sender, fake queries and sender, a clock):

| Test | Asserts |
|------|---------|
| `Reminds_Recipients_Without_A_Submission` | 4 recipients, 1 submitted → 3 `Reminder` rows, sent |
| `Leaves_Out_Students_The_Limits_Exclude_Without_A_Row` | a Student reminded 1 h ago → no row for them |
| `Counts_Manual_And_Scheduled_Together` | history with 3 reminders of either source → excluded |
| `Skips_A_Student_Who_Submitted_Before_The_Send` | Review Focus 9: planned, then held by quiet hours; `HasSubmissionAsync` is true at send time → `Skipped AlreadySubmitted` |
| `A_Manual_Trigger_Is_Its_Request_And_A_Scheduled_One_Is_Its_Check` | trigger values |
| `Delivering_The_Same_Request_Twice_Reminds_Nobody_Twice` | |
| `Does_Nothing_When_The_Publication_Is_No_Longer_Open` | `Done`, no plan (a scheduled request for a window that closed since) |
| `Holds_During_Quiet_Hours` | as task 11 |

- [ ] **Step 4: Implement**

`FindReminderHistoryAsync(publicationId)` groups `student_notifications` by Student: `OpeningSentAtUtc` is the `SentAtUtc` of the `Opening` row; `LastMessageAtUtc` is the max of `SentAtUtc` over rows `Sent`, **and** of `CreatedAtUtc` over rows still `Pending`, any kind; `Reminders` counts `Reminder` rows that are `Sent` or `Pending`. Counting pending rows stops two quick clicks from planning two reminders for one Student.

`FindStudentIdsWithSubmissionAsync(publicationId)` returns the `StudentId`s of `submissions` for that publication; `HasSubmissionAsync(publicationId, studentId)` is the same filter with `AnyAsync`.

The consumer: load the publication. If it isn't Open → `Done`. Compute the trigger from the source. If it isn't planned yet, plan `recipients − submitted − (MayRemind is false)` with the clock's now. Then quiet hours → hold. Then `SendPendingAsync(…, isStillWanted: async id => !await submissionQueries.HasSubmissionAsync(publicationId, id.Value))`.

The controller action follows `ExtendWindow`, returning `Accepted()`:

```csharp
    [HttpPost("{id:guid}/reminders")]
    [EndpointSummary("Queues a WhatsApp reminder to every recipient of the open publication who has not submitted yet")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestReminder(
        [FromServices] RequestSubmissionReminderInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return Accepted();
    }
```

`api-guidelines.md`'s table lists 204 or 200 for business actions. Add a row "Queued business action → 202, no body" with this endpoint as the example.

Register everything; `appsettings.json` gets `Notifications:Reminders`.

- [ ] **Step 5: Requirements §6.3**

"**Reminders:** while the window is Open, an Administrator can send a WhatsApp reminder to every Recipient without a Submission; the system also reminds them at set times of day (see 5.9). Nobody is reminded within 4 hours of their opening notification or of their last message, more than 3 times per week, or during Quiet Hours."

- [ ] **Step 6: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
git add src tests docs/requirements.md .claude/rules/api-guidelines.md
git commit -m "feat(notifications): manual submission reminder within the reminder limits"
```
