# Task 11 of 18: The Opening Notification

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires task 10 committed.

**Files:**
- Create: `src\DrivingLessons.Application\IntegrationEvents\PublicationOpenedIntegrationEvent.cs`
- Create: `src\DrivingLessons.Application\EventHandlers\PublicationOpenedHandler.cs`
- Create: `src\DrivingLessons.Application\Consumers\OpeningNotificationConsumer.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\EventHandlers\PublicationOpenedHandlerTest.cs`, `tests\DrivingLessons.Application.Test\Consumers\OpeningNotificationConsumerTest.cs` (new)

**Interfaces:**
- Consumes: the domain event `PublicationOpened(PublicationId)` (raised by `Publication.Open()`, called by `OpenPublicationJob` and by the startup reconciliation); task 10 (`StudentNotificationPlanner`, `StudentNotificationSender`, `QuietHours`); task 8 (`IStudentNotificationQueries.FindRecipientIdsAsync`, `NotificationTrigger.Opening()`); task 1 (`IIntegrationEventOutbox`, `AddIntegrationEventConsumer`).
- Produces:
  - `record PublicationOpenedIntegrationEvent(Guid PublicationId, DateOnly WeekStart, DateTimeOffset OpenedAtUtc)`, with `EventType "publication-opened"`, v1.
  - `OpeningNotificationConsumer : IIntegrationEventConsumer<PublicationOpenedIntegrationEvent>`.

- [ ] **Step 1: Write the failing tests**

`PublicationOpenedHandlerTest`: `Writes_The_Opened_Event` (ID, week, clock); `Writes_Nothing_When_The_Publication_Is_Gone`.

`OpeningNotificationConsumerTest`, with the real `StudentNotificationPlanner` and `StudentNotificationSender` over fake repositories and a fake `IWhatsAppSender`, so the test reads like the feature:

| Test | Asserts |
|------|---------|
| `Sends_The_Opening_To_Every_Recipient_With_Consent_And_A_Number` | 3 recipients → 3 `Opening` sends with the name, week, end and link token; `Done` |
| `Records_An_Invalid_Number_As_Skipped_And_Sends_The_Others` | Review Focus 6 |
| `Records_A_Student_Without_Consent_As_Skipped` | |
| `A_Rejected_Student_Fails_And_A_Throttled_One_Stays_Pending` | Review Focus 7: one `Rejected`, one unavailable, one sent → throws `StudentNotificationsPendingException`; a second `HandleAsync` sends only the throttled one |
| `Delivering_The_Same_Event_Twice_Sends_Nobody_Twice` | two runs, everything sent on the first → the second sends nothing, `Done` |
| `Plans_During_Quiet_Hours_And_Holds_Until_They_End` | clock at Wed 23:00 local → rows `Pending`, no send, `HoldUntil(Thu 08:00 local)` |
| `After_The_Hold_Sends_The_Planned_Rows` | the same event at Thu 08:00 → sends, no second plan |
| `Sends_Nothing_When_The_Publication_Is_Gone` | `Done` |

- [ ] **Step 2: Run to see them fail**

`dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Opening|FullyQualifiedName~PublicationOpenedHandler"` → FAIL.

- [ ] **Step 3: Implement**

`PublicationOpenedHandler` mirrors task 3's `PublicationClosedHandler`: load, map, `outbox.Add`.

`OpeningNotificationConsumer.HandleAsync`:

```csharp
        var publicationId = PublicationId.Of(integrationEvent.PublicationId);
        var publication = await publicationRepository.GetAsync(publicationId);

        if (publication is null)
        {
            return DeliveryOutcome.Done;
        }

        var trigger = NotificationTrigger.Opening();
        var recipientIds = await notificationQueries.FindRecipientIdsAsync(publicationId.Value);
        var studentIds = recipientIds.Select(StudentId.Of).ToList();

        await planner.PlanAsync(publication, NotificationKind.Opening, trigger, studentIds);

        var now = timeProvider.GetUtcNow();

        if (quietHours.Contains(now))
        {
            return DeliveryOutcome.HoldUntil(quietHours.EndAfter(now));
        }

        await sender.SendPendingAsync(publication, trigger, attempt);

        return DeliveryOutcome.Done;
```

The recipients are read **every** time, but `PlanAsync` ignores them once the trigger is planned, so a redelivery never adds Students who joined later. That is deliberate: the opening is a one-time fact. A Student added mid-window gets the link from reminders, or by hand.

Register: `IDomainEventHandler<PublicationOpened>` → `PublicationOpenedHandler`, and `AddIntegrationEventConsumer<PublicationOpenedIntegrationEvent, OpeningNotificationConsumer>()`.

- [ ] **Step 4: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
git add src tests
git commit -m "feat(notifications): opening notification on WhatsApp when a publication opens"
```
