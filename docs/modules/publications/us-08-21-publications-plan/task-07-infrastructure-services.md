# Task 7 of 14: Infrastructure — services (ClosedXML Excel, logging email)

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–6 complete (through infrastructure persistence + migration). Work on branch `9-us-08-21-publications-module`, all commands from the repo root.

This task fills the two content seams defined in the application layer: `IExcelGenerator` (a real two-sheet ClosedXML workbook — summary grid wired to counts, detail sheet header-only) and `IEmailSender` (`LoggingEmailSender`, gated by `Email:Enabled`). Both drop behind their interfaces, so `module:excel` and a future SMTP/SES sender replace them without touching the close pipeline.

Read the existing options idiom first: `src\DrivingLessons.Infrastructure\Options\JwtOptions.cs` and the `services.AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` block in `DependencyInjection.cs`. Read `appsettings.json` and `appsettings.Development.json` before editing them.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj` (add `ClosedXML`)
- Create: `src\DrivingLessons.Infrastructure\Excel\PlaceholderExcelGenerator.cs`
- Create: `src\DrivingLessons.Infrastructure\Options\EmailOptions.cs`
- Create: `src\DrivingLessons.Infrastructure\Email\LoggingEmailSender.cs`
- Modify: `src\DrivingLessons.Presentation.Web\appsettings.json`, `src\DrivingLessons.Presentation.Web\appsettings.Development.json`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`

---

- [ ] **Step 1: Add the ClosedXML package**

Run:

```bash
dotnet add src\DrivingLessons.Infrastructure package ClosedXML
```

Expected: a `PackageReference` is added to `DrivingLessons.Infrastructure.csproj`. Confirm the `ItemGroup` now contains it (version pinned by the restore — the current stable line is `0.104.*`):

```xml
<ItemGroup>
  <PackageReference Include="ClosedXML" Version="0.104.2" />
  <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="10.0.9" />
  <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.9" />
  <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" Version="10.0.9" />
  <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" Version="8.19.1" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
</ItemGroup>
```

- [ ] **Step 2: The placeholder Excel generator**

`Excel\PlaceholderExcelGenerator.cs`. It resolves the Publication's week via `IPublicationRepository`, the teacher's slots for that week via `IWeekScheduleQueries`, and the per-slot counts via `ISubmissionQueries` (zeros until `module:student-form`). It writes a **Summary** sheet — rows = slot windows Morning/Noon/Afternoon/Evening, columns = Sunday…Friday, each cell = that slot's request count; a day/window with no slot stays blank (e.g. Friday has no Evening), and `Unavailable` slots are shaded — and a **Detail** sheet with the header row only.

> **`module:excel` boundary:** `BuildDetailSheet` writes only the header row per requirements §9. The Excel module (US-44/45/46) appends the per-student detail rows beneath that header and adds summary formatting; it plugs in here without touching the close/email pipeline. Keep this task to the header + summary counts — do **not** implement detail content now (plan risk #6).

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public class PlaceholderExcelGenerator : IExcelGenerator
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly DayOfWeek[] Days =
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    private static readonly SlotWindowType[] Windows =
    [
        SlotWindowType.Morning,
        SlotWindowType.Noon,
        SlotWindowType.Afternoon,
        SlotWindowType.Evening
    ];

    private readonly IPublicationRepository publicationRepository;
    private readonly IWeekScheduleQueries weekScheduleQueries;
    private readonly ISubmissionQueries submissionQueries;

    public PlaceholderExcelGenerator(
        IPublicationRepository publicationRepository,
        IWeekScheduleQueries weekScheduleQueries,
        ISubmissionQueries submissionQueries)
    {
        this.publicationRepository = publicationRepository;
        this.weekScheduleQueries = weekScheduleQueries;
        this.submissionQueries = submissionQueries;
    }

    public async Task<ExcelFile> GenerateAsync(PublicationId publicationId, TeacherId teacherId)
    {
        var publication = await publicationRepository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var weekStart = publication.WeekStart.Value;
        var teacherGuid = teacherId.Value;

        var schedule = await weekScheduleQueries.GetByTeacherAndWeekAsync(teacherGuid, weekStart);
        var counts = await submissionQueries.GetSlotRequestCountsAsync(publicationId.Value, teacherGuid);

        var workbook = new XLWorkbook();

        BuildSummarySheet(workbook, schedule, counts);
        BuildDetailSheet(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"week-{weekStart:yyyy-MM-dd}-{teacherGuid}.xlsx";

        return new ExcelFile(fileName, content, ContentType);
    }

    private static void BuildSummarySheet(
        XLWorkbook workbook,
        GetWeekScheduleResponse? schedule,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var sheet = workbook.Worksheets.Add("Summary");

        for (var column = 0; column < Days.Length; column++)
        {
            sheet.Cell(1, column + 2).Value = Days[column].ToString();
        }

        var slotsByCell = schedule?
                              .Slots
                              .ToDictionary(slot => (slot.Day, slot.Window))
                          ?? [];

        for (var row = 0; row < Windows.Length; row++)
        {
            var window = Windows[row];
            sheet.Cell(row + 2, 1).Value = window.ToString();

            for (var column = 0; column < Days.Length; column++)
            {
                var day = Days[column];
                var cell = sheet.Cell(row + 2, column + 2);

                if (!slotsByCell.TryGetValue((day, window), out var slot))
                {
                    continue;
                }

                if (slot.State == SlotState.Unavailable)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    continue;
                }

                cell.Value = counts.TryGetValue(slot.Id, out var count) ? count : 0;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildDetailSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Detail");

        sheet.Cell(1, 1).Value = "Student";
        sheet.Cell(1, 2).Value = "Day";
        sheet.Cell(1, 3).Value = "Window";
        sheet.Cell(1, 4).Value = "Session Type";
        sheet.Cell(1, 5).Value = "Rank";
        sheet.Cell(1, 6).Value = "Constraint";
    }
}
```

`PublicationNotFoundException` (task-04, `Application\Common\Exceptions\`) takes a `PublicationId`; `GetWeekScheduleResponse`/`SlotForGetWeekScheduleResponse` are the DTOs from the week-schedules slice (`Slots` exposes `Id`/`Day`/`Window`/`State`).

- [ ] **Step 3: Email options**

`Options\EmailOptions.cs` — mirror `JwtOptions` (sealed, `SectionName` const, `init` props):

```csharp
namespace DrivingLessons.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }
}
```

- [ ] **Step 4: The logging email sender**

`Email\LoggingEmailSender.cs`. When `Email:Enabled` is `false`, it logs recipient/subject/attachment size and returns without sending. When a real sender is wired later it replaces this registration; the `Enabled` gate then flips on for that implementation.

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Email;

public class LoggingEmailSender : IEmailSender
{
    private readonly EmailOptions options;
    private readonly ILogger<LoggingEmailSender> logger;

    public LoggingEmailSender(IOptions<EmailOptions> options, ILogger<LoggingEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public Task SendAsync(EmailMessage message)
    {
        if (!options.Enabled)
        {
            logger.LogInformation(
                "Email disabled. Skipped send to [{Recipient}] subject [{Subject}] attachment [{AttachmentBytes}] bytes.",
                message.ToEmail,
                message.Subject,
                message.Attachment.Content.Length);

            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Configuration**

Add an `Email` section (`Enabled = false`) to both settings files. In `src\DrivingLessons.Presentation.Web\appsettings.json`, add after the `Jwt` block:

```json
  "Jwt": {
    "Issuer": "DrivingLessons",
    "Audience": "DrivingLessons",
    "SigningKey": "",
    "ExpiryHours": 12
  },
  "Email": {
    "Enabled": false
  }
```

In `src\DrivingLessons.Presentation.Web\appsettings.Development.json`, add the same after its `Jwt` block:

```json
  "Jwt": {
    "Issuer": "DrivingLessons",
    "Audience": "DrivingLessons",
    "SigningKey": "dev-only-signing-key-at-least-32-characters-long!",
    "ExpiryHours": 12
  },
  "Email": {
    "Enabled": false
  }
```

- [ ] **Step 6: DI registration**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, bind `EmailOptions` next to the other `AddOptions` calls:

```csharp
services.AddOptions<EmailOptions>()
    .Bind(configuration.GetSection(EmailOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

and register the two services next to the publication registrations:

```csharp
services.AddScoped<IExcelGenerator, PlaceholderExcelGenerator>();
services.AddScoped<IEmailSender, LoggingEmailSender>();
```

Add the required usings at the top of the file: `using DrivingLessons.Application.Abstractions;`, `using DrivingLessons.Infrastructure.Excel;`, `using DrivingLessons.Infrastructure.Email;`. (`DrivingLessons.Infrastructure.Options` is already imported.)

- [ ] **Step 7: Build**

Run: `dotnet build`

Expected: `Build succeeded.` with zero errors.

- [ ] **Step 8: Run domain tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`

Expected: all tests PASS (no domain change; confirms no regression and that the new package restored cleanly).

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Infrastructure src/DrivingLessons.Presentation.Web
git commit -m "feat(infra): closedxml placeholder excel generator and logging email sender"
```

---

**Next:** [task-08-scheduling.md](task-08-scheduling.md)
