# Task 12 of 18: The Window Change Notification

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires task 11 committed.

**Files:**
- Create: `src\DrivingLessons.Application\IntegrationEvents\PublicationWindowChangedIntegrationEvent.cs`
- Create: `src\DrivingLessons.Application\EventHandlers\PublicationWindowExtendedHandler.cs`, `PublicationReopenedHandler.cs`
- Create: `src\DrivingLessons.Application\Consumers\WindowChangeNotificationConsumer.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationWindowExtendedHandlerTest.cs`, `PublicationReopenedHandlerTest.cs`, `tests\DrivingLessons.Application.Test\Consumers\WindowChangeNotificationConsumerTest.cs` (new)

**Interfaces:**
- Consumes: the domain events `PublicationWindowExtended(PublicationId, NewEndUtc)` and `PublicationReopened(PublicationId, NewEndUtc)`; task 8 (`IStudentNotificationRepository.FindPendingWindowChangesAsync`, `StudentNotification.Supersede`, `NotificationTrigger.WindowChange(Guid)`); task 10; task 11's consumer shape.
- Produces:
  - `record PublicationWindowChangedIntegrationEvent(Guid PublicationId, Guid ChangeId, DateTimeOffset NewEndUtc, bool WasReopened, DateTimeOffset ChangedAtUtc)`, with `EventType "publication-window-changed"`, v1. `ChangeId` is `Guid.CreateVersion7()` in the handler. `WasReopened` is carried for a later fourth template (decision 15), and unused in stage 1.
  - `WindowChangeNotificationConsumer`.

Intent §4.3: "Every later way of changing the window raises its own domain event and joins this trigger." That means one more handler mapping to this same integration event, not a new consumer.

- [ ] **Step 1: Write the failing tests**

Handler tests: each one writes the changed event with `NewEndUtc`, its own `WasReopened`, a fresh `ChangeId` each time (two calls → two IDs) and the clock.

`WindowChangeNotificationConsumerTest`:

| Test | Asserts |
|------|---------|
| `Sends_The_New_End_To_Every_Recipient` | Students with and without a Submission both get it (decision 17, open question 5) |
| `Supersedes_The_Pending_Rows_Of_An_Earlier_Change` | change A planned and held; change B arrives → A's pending rows `Superseded`, B's planned; A's later run sends nothing |
| `Does_Not_Supersede_Rows_Of_The_Same_Change` | redelivery of B → nothing superseded, nothing re-planned |
| `Does_Not_Touch_Sent_Rows_Of_An_Earlier_Change` | A was sent → A stays `Sent` |
| `Never_Supersedes_Opening_Or_Reminder_Rows` | |
| `The_Text_Shows_The_Current_End_Not_The_Event_End` | the window was extended again after the event → the sent parameter is the publication's current end (decision 14) |
| `Holds_During_Quiet_Hours` | as task 11 |
| `Sends_Nothing_When_The_Window_Closed_Before_The_Send` | the publication is Closed → rows `Skipped WindowClosed` |

- [ ] **Step 2: Run to see them fail, then implement**

The consumer is task 11's, with one extra step between loading the publication and planning, and only when this trigger has no rows yet:

```csharp
        var trigger = NotificationTrigger.WindowChange(integrationEvent.ChangeId);
        var alreadyPlanned = await notificationRepository.FindForTriggerAsync(publication.Id, trigger);

        if (alreadyPlanned.Count == 0)
        {
            var earlier = await notificationRepository.FindPendingWindowChangesAsync(publication.Id);
            earlier.ToList().ForEach(x => x.Supersede());
        }
```

`FindPendingWindowChangesAsync` returns only `Kind == WindowChange && State == Pending`. The supersede and the new plan commit together: `PlanAsync` commits the tracked superseded rows with its new ones. If the new trigger has no recipients, `PlanAsync` doesn't commit, so the consumer commits itself when `earlier` was not empty. Pin this with a test: `Supersedes_Even_When_The_New_Change_Has_No_Recipients`.

Register both domain handlers and the consumer.

- [ ] **Step 3: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
git add src tests
git commit -m "feat(notifications): window change notification on extend and reopen, earlier pending changes superseded"
```
