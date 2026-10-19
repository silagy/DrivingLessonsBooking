# Task 16 of 18: Scheduled reminders and their switch

> Part of [Student Notifications (stage 1)](README.md), milestone C. Work on branch `notifications-reminders`. Requires task 15 committed.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\ReminderSchedule.cs`, `Exceptions\ReminderScheduleMustBeValidException.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Publication.cs` (`ScheduledRemindersOn`, `TurnOffScheduledReminders`, `TurnOnScheduledReminders`)
- Create: `src\DrivingLessons.Domain\Events\PublicationScheduledRemindersTurnedOff.cs`, `PublicationScheduledRemindersTurnedOn.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PublicationScheduledRemindersAlreadyOffException.cs`, `PublicationScheduledRemindersAlreadyOnException.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\IPublicationRepository.cs` (+ `FindOpenAsync()`) and `PublicationRepository.cs`
- Create: `src\DrivingLessons.Application\Commands\RequestScheduledReminders\RequestScheduledRemindersInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\TurnOffScheduledReminders\…Interactor.cs`, `Commands\TurnOnScheduledReminders\…Interactor.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\SubmissionReminderCheckJob.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (the Quartz cron), `Options\NotificationOptions.cs` (+ `Reminders:CheckTimes`, `Reminders:LastCallHoursBeforeEnd`, registers `ReminderSchedule`)
- Modify: `PublicationConfiguration.cs` (`scheduled_reminders_on`, default `true`) + migration `{timestamp}_AddScheduledRemindersSwitch`
- Modify: `PublicationCommandController.cs` (+ `turn-off` / `turn-on`), `Queries\GetPublicationNotifications\…` (projects the real switch, replacing task 13's constant)
- Test: `Domain.Test\Values\ReminderScheduleTest.cs` (new), `Domain.Test\Entities\PublicationTest.cs`
- Test: `Application.Test\Commands\RequestScheduledRemindersInteractorTest.cs`, `TurnOffScheduledRemindersInteractorTest.cs`, `TurnOnScheduledRemindersInteractorTest.cs` (new)

**Interfaces:**
- Consumes: task 15 (`SubmissionReminderRequestedIntegrationEvent`, `ReminderSource.Scheduled`, `SubmissionReminderConsumer`), task 8 (`QuietHours`, `SchoolTimeZone`), Quartz (`AddQuartz`, already registered without configuration).
- Produces:
  - `ReminderSchedule.Of(IReadOnlyCollection<TimeOnly> checkTimes, TimeSpan lastCallBeforeEnd)`, `.IsCheckDue(SubmissionWindow window, DateTimeOffset tickUtc, TimeSpan tickLength, QuietHours quietHours) : bool`.
  - `Publication.ScheduledRemindersOn`, `TurnOffScheduledReminders()` and `TurnOnScheduledReminders()`.
  - `POST api/publications/{id}/scheduled-reminders/turn-off` and `…/turn-on` → 204 / 404 / 409 (Administrator).
  - `RequestScheduledRemindersInteractor.ExecuteAsync(DateTimeOffset checkUtc)`.

- [ ] **Step 1: Write the failing schedule tests (Review Focus 11)**

`ReminderScheduleTest`, with check times 10:00, 14:00, 19:00, a last call 3 h before the end, a 15-minute tick, the default quiet hours, and a window ending Wed 20:00 local:

| Tick (local) | Due | Why |
|--------------|-----|-----|
| Tue 10:00 | yes | check time |
| Tue 10:15 | no | |
| Tue 09:45 | no | the 10:00 check belongs to the 10:00 tick, `[tick, tick + 15m)` |
| Wed 17:00 | yes | last call (20:00 − 3 h) |
| Wed 19:00 | yes | check time; the last call already went at 17:00, and the per-Student gap stops a second message |
| window ending Wed 22:00, tick Wed 19:00 | yes, **once** | the last call (19:00) and a check time in the same tick are one check |
| window ending Thu 09:00, tick Thu 06:00 | no | the last call falls in quiet hours: skipped, not postponed (decision 16) |
| Fri 14:00 | yes | |
| window ending Sun 12:00, tick Sat 10:00 | no | Shabbat |
| tick after the window end | no | |

`Must_Be_Valid`: no check times and no last call, or a negative last call → `ReminderScheduleMustBeValidException`.

- [ ] **Step 2: Write the failing switch tests**

`PublicationTest`: `New_Publication_Has_Scheduled_Reminders_On`; `Turn_Off_Scheduled_Reminders` / `__Add_Event` / `__Must_Be_On`; `Turn_On_Scheduled_Reminders` / `__Add_Event` / `__Must_Be_Off`. Allowed in every state: a Draft can be prepared with them off, and a Closed one may be reopened.

- [ ] **Step 3: Write the failing interactor tests (Review Focus 10)**

`RequestScheduledRemindersInteractorTest` (fake `IPublicationRepository.FindOpenAsync`, fake outbox, the real `ReminderSchedule` and `QuietHours`):
- `Writes_A_Scheduled_Request_For_Each_Open_Publication_That_Is_Due`.
- `Skips_A_Publication_With_Scheduled_Reminders_Off`.
- `Skips_A_Publication_For_Which_The_Check_Is_Not_Due`.
- `The_Same_Check_Twice_Writes_The_Same_Trigger`: two calls with the same `checkUtc` → two events with the same `CheckUtc` (the consumer's plan step and the unique index make the second a no-op; task 18 Step 5 proves it end to end).
- `Commits_Once_And_Not_At_All_When_Nothing_Is_Due`.

`Turn{Off,On}ScheduledRemindersInteractorTest`: resolve, call, commit; not found; already in that state → the domain exception.

- [ ] **Step 4: Implement**

`ReminderSchedule.IsCheckDue`: false if `tickUtc` is outside `[window.StartUtc, window.EndUtc)` or in quiet hours. Otherwise convert the tick to local time; due if any check time `t` on the tick's local date satisfies `tick ≤ t < tick + tickLength`, or if `window.EndUtc − lastCallBeforeEnd` falls in `[tickUtc, tickUtc + tickLength)`.

The interactor, for each `FindOpenAsync()` publication with `ScheduledRemindersOn` and `IsCheckDue(…)`, calls `outbox.Add(new SubmissionReminderRequestedIntegrationEvent(id, ReminderSource.Scheduled, Guid.CreateVersion7(), checkUtc, now))`. Then one `CommitAsync()` if anything was added. There is no domain change here (intent §5.1); `CommitAsync` still gives the transaction.

`SubmissionReminderCheckJob` (`[DisallowConcurrentExecution]`): `checkUtc = context.ScheduledFireTimeUtc ?? context.FireTimeUtc`, then `interactor.ExecuteAsync(checkUtc)`. Using the **scheduled** time is what makes two instances agree on the trigger.

Quartz registration, replacing the bare `services.AddQuartz()`:

```csharp
        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey("submission-reminder-check", "notifications");

            quartz.AddJob<SubmissionReminderCheckJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity("submission-reminder-check", "notifications")
                .WithCronSchedule("0 0/15 * * * ?", cron => cron
                    .InTimeZone(SchoolTimeZone.Info)
                    .WithMisfireHandlingInstructionDoNothing()));
        });
```

`WithMisfireHandlingInstructionDoNothing`: a 10:00 check missed while the app was down is **not** fired late at boot. That matches "skipped, not postponed". `SchoolTimeZone.Info` is exposed by task 8's class. Add it there if it's private.

`NotificationOptions.Reminders`: `CheckTimes: ["10:00","14:00","19:00"]`, `LastCallHoursBeforeEnd: 3`, both validated, building `ReminderSchedule` as a singleton.

Migration: `scheduled_reminders_on boolean NOT NULL DEFAULT true`. Existing publications are on, matching open question 7's default.

The endpoints: `[HttpPost("{id:guid}/scheduled-reminders/turn-off")]` and `turn-on`, shaped like `extend-window` without a body, returning 204.

- [ ] **Step 5: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
git add src tests
git commit -m "feat(notifications): scheduled reminder checks at configured times, switchable per publication"
```
