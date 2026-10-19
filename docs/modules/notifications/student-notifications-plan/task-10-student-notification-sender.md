# Task 10 of 18: Plan and send: the loop every Student consumer shares

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires tasks 8 and 9 committed.

**Files:**
- Create: `src\DrivingLessons.Application\Notifications\StudentNotificationPlanner.cs`
- Create: `src\DrivingLessons.Application\Notifications\StudentNotificationSender.cs`
- Create: `src\DrivingLessons.Application\Notifications\StudentNotificationsPendingException.cs`
- Create: `src\DrivingLessons.Application\Notifications\NotificationTemplateParameters.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs` (+ `FindByIdsAsync(IReadOnlyCollection<StudentId>)`) and `StudentRepository.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\Notifications\StudentNotificationPlannerTest.cs`, `StudentNotificationSenderTest.cs`, `NotificationTemplateParametersTest.cs` (new)

**Interfaces:**
- Consumes: task 8 (`StudentNotification.Plan / MarkSent / MarkFailed / MarkSkipped`, `NotificationTrigger`, `IStudentNotificationRepository`, `IStudentNotificationQueries.FindRecipientIdsAsync`, `SchoolTimeZone`, `QuietHours`); task 9 (`IWhatsAppSender`, `WhatsAppMessage`, `WhatsAppTemplate`, `WhatsAppSendResult`, `WhatsAppUnavailableException`); task 1 (`DeliveryAttempt`, `DeliveryOutcome`).
- Produces (tasks 11, 12 and 15 call these three, in this order):
  - `StudentNotificationPlanner.PlanAsync(Publication publication, NotificationKind kind, NotificationTrigger trigger, IReadOnlyCollection<StudentId> studentIds) : Task`. If the trigger already has rows, it does nothing: the plan of a redelivered event is already in place. Otherwise it writes one row per Student (`Pending` or `Skipped`) in **one** commit.
  - `QuietHours.Contains(now)` / `EndAfter(now)`. Each consumer holds itself: `return DeliveryOutcome.HoldUntil(quietHours.EndAfter(now))`.
  - `StudentNotificationSender.SendPendingAsync(Publication publication, NotificationTrigger trigger, DeliveryAttempt attempt, Func<StudentId, Task<bool>>? isStillWanted = null) : Task`. It throws `StudentNotificationsPendingException` while rows are still pending after a transient failure.
  - `NotificationTemplateParameters.For(NotificationKind kind, Publication publication, Student student) : IReadOnlyList<string>`.

- [ ] **Step 1: Write the failing text tests**

`NotificationTemplateParametersTest`, with a Publication for the week starting Sunday 4 October 2026 whose window ends `2026-10-07T17:00Z` (Wednesday 20:00 local):

| Test | Expected |
|------|----------|
| `Opening_Names_The_Student_The_Week_And_The_End` | `["דנה לוי", "4.10–9.10", "יום ד׳ 7.10 בשעה 20:00"]` |
| `Window_Change_Names_The_Week_And_The_New_End` | `["4.10–9.10", "יום ד׳ 7.10 בשעה 20:00"]` |
| `Reminder_Names_The_Student_And_The_End` | `["דנה לוי", "יום ד׳ 7.10 בשעה 20:00"]` |
| `The_End_Is_In_Jerusalem_Time_In_Winter` | end `2026-12-02T18:00Z` → `"יום ד׳ 2.12 בשעה 20:00"` |
| `A_Week_Across_Two_Months` | Sunday 27 September → `"27.9–2.10"` |
| `Collapses_Whitespace_WhatsApp_Refuses_In_A_Parameter` | the name `"דנה\tלוי   כהן\n"` → `"דנה לוי כהן"` (Meta rejects tabs, newlines and runs of 4+ spaces) |
| `Day_Letters` `[DataRow]` Sunday → `א׳` … Saturday → `ש׳` | fixed letters, not a culture's day names, so the text doesn't depend on the server's ICU data |

- [ ] **Step 2: Write the failing planner tests**

`StudentNotificationPlannerTest` (fake repositories, a real Open Publication, real Students):
- `Writes_One_Row_Per_Student_In_One_Commit`: 3 Students → 3 `Add`s, 1 commit.
- `Records_Skips_With_Their_Reason`: one without consent, one with a landline → `Skipped NoConsent`, `Skipped InvalidNumber`.
- `Does_Nothing_When_The_Trigger_Is_Already_Planned`: `FindForTriggerAsync` returns a row → no `Add`, no commit.
- `Skips_A_Student_Gone_Since_The_Recipients_Were_Read`: `FindByIdsAsync` returns 2 of 3 → 2 rows.
- `Writes_Nothing_For_No_Recipients`: no commit.

- [ ] **Step 3: Write the failing sender tests**

`StudentNotificationSenderTest` (`A.Fake<IWhatsAppSender>`, fake repositories, fake `IUnitOfWork`, a fixed clock):

| Test | Asserts |
|------|---------|
| `Sends_Each_Pending_Row_And_Marks_It_Sent` | 2 rows → 2 sends to `9725…`, template by kind, `LinkToken == publication.LinkToken.Value`, both `Sent` with the provider ID, a commit after each |
| `Leaves_Skipped_Rows_Alone` | a skipped row → no send |
| `A_Rejected_Row_Fails_With_The_Reason_And_The_Others_Still_Go` | |
| `A_Disabled_Sender_Marks_The_Row_Skipped` | `WhatsAppDisabled` |
| `An_Unavailable_Row_Stays_Pending_And_The_Run_Throws_At_The_End` | rows A (unavailable), B (sent) → B `Sent`, A `Pending`, then `StudentNotificationsPendingException(1)` |
| `On_The_Last_Attempt_Unavailable_Rows_Fail_Instead` | `DeliveryAttempt(8, true)` → A `Failed("WhatsApp unavailable after 8 attempts")`, no throw |
| `A_Row_No_Longer_Wanted_Is_Skipped_As_Already_Submitted` | `isStillWanted` false for A → `Skipped AlreadySubmitted`, no send |
| `Every_Row_Is_Skipped_When_The_Window_Has_Closed` | the publication is Closed → each pending row `Skipped WindowClosed`, no send |
| `A_Student_Deactivated_Since_The_Plan_Is_Superseded` | Inactive Student → `Superseded`, no send (the plan no longer holds) |
| `Consent_Withdrawn_Since_The_Plan_Is_Skipped` | `Skipped NoConsent`, no send |
| `A_Number_Edited_Into_A_Landline_Since_The_Plan_Is_Skipped` | phone changed to `02-6251234` after the plan → `Skipped InvalidNumber`, no send |
| `Logs_Ids_Only` | no log argument contains a number, a name or a parameter |

- [ ] **Step 4: Run to see them fail**

`dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Notifications"` → FAIL.

- [ ] **Step 5: Implement**

`NotificationTemplateParameters`: the formats of Step 1. The week runs Sunday to Friday (`weekStart`, `weekStart + 5`) and is formatted `d.M–d.M`. The end is `SchoolTimeZone.ToLocal(window.EndUtc)` as `"יום {letter} {d.M} בשעה {HH:mm}"`. One private `Clean(string)` collapses whitespace.

`StudentNotificationSender.SendPendingAsync`:

```text
rows = repository.FindForTriggerAsync(publication.Id, trigger) where IsPending
students = studentRepository.FindByIdsAsync(rows.StudentId)
unavailable = []
for row in rows:
    if !publication.IsOpen               → row.MarkSkipped(WindowClosed); commit; continue
    student = students[row.StudentId]
    if student is null or !IsActive      → row.Supersede(); commit; continue
    if isStillWanted is not null and !await isStillWanted(row.StudentId)
                                          → row.MarkSkipped(AlreadySubmitted); commit; continue
    if !student.HasWhatsAppConsent       → row.MarkSkipped(NoConsent); commit; continue
    number = student.Phone.ToWhatsAppNumber()
    if number is null                    → row.MarkSkipped(InvalidNumber); commit; continue
    message = new WhatsAppMessage(number.Value, TemplateOf(row.Kind),
                                  NotificationTemplateParameters.For(row.Kind, publication, student),
                                  publication.LinkToken.Value)
    try   result = await whatsAppSender.SendAsync(message)
    catch WhatsAppUnavailableException → unavailable.Add(row); Warning log (row, publication IDs); continue
    result.IsSent     → row.MarkSent(result.ProviderMessageId, now)
    result.IsRejected → row.MarkFailed(result.RejectionReason)
    result.IsDisabled → row.MarkSkipped(WhatsAppDisabled)
    commit
if unavailable is empty → return
if attempt.IsLast → each row.MarkFailed($"WhatsApp unavailable after {attempt.Number} attempts"); commit; return
throw new StudentNotificationsPendingException(publication.Id, unavailable.Count)
```

The number is converted again at send time, not read from the plan: an Administrator may have edited the phone or withdrawn consent between the plan and a held or retried send.

`StudentRepository.FindByIdsAsync`: `Where(x => ids.Contains(x.Id)).ToListAsync()`.

Register `StudentNotificationPlanner` and `StudentNotificationSender` as scoped.

- [ ] **Step 6: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
git add src tests
git commit -m "feat(notifications): plan-then-send loop for student WhatsApp notifications, Hebrew template parameters"
```
