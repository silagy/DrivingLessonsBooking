# Task 13 of 18: What the Administrator sees, the notifications summary

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires tasks 3, 8, 11 and 12 committed. Read the client rules before the client half.

**Files (backend):**
- Create: `src\DrivingLessons.Application\Queries\GetPublicationNotifications\GetPublicationNotificationsInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\GetPublicationNotifications\GetPublicationNotificationsResponse.cs` (+ nested `…For…` DTOs)
- Modify: `src\DrivingLessons.Application\Queries\IStudentNotificationQueries.cs` (+ `GetSummaryAsync`) and `Infrastructure\EntityFramework\Queries\StudentNotificationQueries.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs` (+ `GetNotifications`, Administrator)
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\GetPublicationNotificationsInteractorTest.cs` (new)

**Files (client):**
- Create: `client\src\app\features\publications\data\get-publication-notifications.response.ts`
- Modify: `client\src\app\features\publications\data\publications-api.service.ts` (`getNotifications`)
- Create: `client\src\app\features\publications\state\publication-notifications.store.ts` (+ spec)
- Create: `client\src\app\features\publications\ui\components\publication-notifications-panel\…` (+ spec)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.{ts,html}`
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes: `student_notifications` (task 8), `TeacherExcelVersion.EmailedAtUtc` (task 3), `Publication.ScheduledRemindersOn` (task 16; until then the field is `true` from a constant, see Step 2), `ISubmissionQueries` for "without a Submission".
- Produces: `GET api/publications/{id}/notifications` → `200 GetPublicationNotificationsResponse` / `404`, Administrator only (no attribute: the fallback policy).

```csharp
public class GetPublicationNotificationsResponse
{
    public int Recipients { get; init; }
    public int RecipientsWithoutSubmission { get; init; }
    public NotificationCountsForGetPublicationNotificationsResponse Opening { get; init; } = new();
    public NotificationCountsForGetPublicationNotificationsResponse WindowChange { get; init; } = new();
    public NotificationCountsForGetPublicationNotificationsResponse Reminders { get; init; } = new();
    public bool ScheduledRemindersOn { get; init; }
    public IReadOnlyList<TeacherEmailForGetPublicationNotificationsResponse> TeacherEmails { get; init; } = [];
    public IReadOnlyList<ProblemForGetPublicationNotificationsResponse> Problems { get; init; } = [];
}

public class NotificationCountsForGetPublicationNotificationsResponse
{
    public int Sent { get; init; }
    public int Pending { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
}

public class TeacherEmailForGetPublicationNotificationsResponse
{
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public int ExcelVersion { get; init; }
    public DateTimeOffset? EmailedAtUtc { get; init; }
}

public class ProblemForGetPublicationNotificationsResponse
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public NotificationKind Kind { get; init; }
    public NotificationState State { get; init; }
    public NotificationSkipReason? SkipReason { get; init; }
    public string? FailureReason { get; init; }
}
```

Counting rules:
- **Window change counts only the latest change's rows**: superseded rows don't count, and an earlier change's sent rows are history the panel doesn't need.
- **Reminders** count the rows of every reminder trigger.
- **Problems** list `Failed` rows and the `Skipped` rows whose reason an Administrator can act on (`NoConsent`, `InvalidNumber`). `WhatsAppDisabled`, `AlreadySubmitted` and `WindowClosed` aren't problems. Each Student is listed once, under their most recent problem.
- An `EmailedAtUtc` of `null` shows `—`, with the tooltip "not sent yet, or sent before this was recorded": closes from before task 3 deployed were never recorded, and the client can't tell the two apart.

- [ ] **Step 1: Backend — failing interactor tests, then the query**

`GetPublicationNotificationsInteractorTest` (fake `IStudentNotificationQueries`, `IPublicationQueries`): `Throws_Not_Found_For_An_Unknown_Publication`, `Returns_The_Summary`. The counting rules are SQL; they are covered by the smoke (task 14 Step 7), because this repo has no database tests. Keep the query in **one** place, `StudentNotificationQueries.GetSummaryAsync`, written as grouped counts (`GroupBy(x => new { x.Kind, x.State }).Select(g => new { g.Key, Count = g.Count() })`), plus one query for the problems and one for the Teachers' versions.

- [ ] **Step 2: Backend — endpoint**

`[HttpGet("{id:guid}/notifications")]`, summary "Gets the WhatsApp notification and teacher email summary of a publication", `[Authorize(Policy = AuthorizationPolicies.Administrator)]`. The controller class is `TeacherOrAdministrator`, so this action narrows it. `ScheduledRemindersOn` is the constant `true` until task 16 adds `Publication.ScheduledRemindersOn` and projects it instead.

- [ ] **Step 3: Client — store and panel specs (failing)**

`publication-notifications.store.spec.ts`: `loads the summary for the selected publication, for administrators only`; `does not call the API for a Teacher-role user`; `exposes the problems list sorted by student name`.

`publication-notifications-panel.component.spec.ts`: `shows sent, pending, failed and skipped for the opening`; `hides the window change block when there has been no change`; `lists each problem with its reason text`; `shows each teacher's email version and time, or a dash`.

- [ ] **Step 4: Client — implement**

The store follows `PublicationsStore`: a `resource` keyed by `publication()?.id` when `auth.isAdministrator()`, readonly signals out, and a `reload()` the page calls on refresh. The panel is presentational (`input.required`). It renders under the slot grid, inside a `p-panel` titled "Notifications for all teachers", with `toggleable` collapsed state remembered in `localStorage` (try/catch). The layout:
- three stat chips (reuse `PublicationStatChipComponent`): Opening, Window change, Reminders. Each one reads `{sent} sent · {pending} waiting · {failed} failed · {skipped} skipped`;
- a short Teachers' email list: name, `גרסה {n}`, time with `formatInstantInJerusalem`, or `—`;
- a problems table (`p-table`, `size="small"`, no paginator under 50 rows): Student, Teacher, what, reason.

Reason texts are i18n keys by enum value (`notifications.skipReason.noConsent`, `.invalidNumber`; `notifications.failed`, with the provider reason shown as-is in a `<bdi>`, because it's Meta's English text).

i18n (he first; en in the same commit), under `publications.notifications.*`: `title`, `opening`, `windowChange`, `reminders`, `sent`, `pending`, `failed`, `skipped`, `teacherEmails`, `version`, `notEmailed`, `notEmailedHint`, `problems`, `student`, `teacher`, `what`, `reason`, `noProblems`.

- [ ] **Step 5: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
cd client && npx ng test --watch=false && npx ng build && cd ..
git add src tests client/src/app/features/publications client/public/i18n
git commit -m "feat(notifications): publication notifications summary for administrators"
```
