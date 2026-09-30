# Task 1 of 3: The request detail sheet — Hebrew, right-to-left, one sorted row per Slot Request

> Part of [US-46: Request Detail Sheet](README.md). Work on branch `46-us-46-excel-detail-sheet`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (+ `SlotRequestDetail` record)
- Create: `src\DrivingLessons.Infrastructure\Excel\HebrewExcelLabels.cs`
- Create: `src\DrivingLessons.Infrastructure\Excel\RequestDetailSheet.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`

**Interfaces:**
- Consumes (on `main`): `DayOfWeek`, `SlotWindowType` (`Morning=10 … Evening=40`), `Transmission` (`Automatic=10, Manual=20`), `SessionType` (`Single=10, Double=20`) from `DrivingLessons.Domain.Values`; ClosedXML 0.105 (`XLWorkbook`, `IXLWorksheet`, `XLCellValue`, `Blank.Value`).
- Produces:
  - `public record SlotRequestDetail(DayOfWeek Day, SlotWindowType Window, string StudentName, string NationalId, string Phone, Transmission Transmission, SessionType SessionType, int Rank, int TargetCount, string? Constraint)` in namespace `DrivingLessons.Application.Queries` — task 2's query returns it.
  - `public static class RequestDetailSheet` in `DrivingLessons.Infrastructure.Excel` with `public const string Name = "פירוט בקשות"` and `public static void AddTo(XLWorkbook workbook, IReadOnlyCollection<SlotRequestDetail> requests)` — adds the sheet to the workbook; task 2's `ExcelGenerator` calls it after the summary sheet.
  - `public static class HebrewExcelLabels` with `DayOf(DayOfWeek)`, `WindowOf(SlotWindowType)`, `TransmissionOf(Transmission)`, `SessionTypeOf(SessionType)` → Hebrew strings; slice 2 (US-45) reuses `DayOf` / `WindowOf` for the summary sheet.

`RequestDetailSheet` owns every rule of sheet 2 (README decision 3): the order, the labels, the cell types, the layout. The tests write a workbook, save it to a stream, load it back and read the cells — what a teacher's Excel would read.

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`:

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class RequestDetailSheetTest
{
    private const int ColumnCount = 10;
    private const int ConstraintsColumn = 10;

    [TestMethod]
    public void Writes_The_Hebrew_Header_Row()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request()]);

        //then
        var sheet = Reloaded(workbook);
        RowOf(sheet, 1).ShouldBe(
            [
                "יום",
                "משבצת",
                "שם התלמיד/ה",
                "תעודת זהות",
                "טלפון",
                "תיבת הילוכים",
                "סוג שיעור",
                "דירוג",
                "יעד שיעורים",
                "אילוצים"
            ]);
        sheet.Cell(1, 1).Style.Font.Bold.ShouldBeTrue();
    }

    [TestMethod]
    public void Lists_Only_The_Header_When_There_Are_No_Requests()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, []);

        //then
        var sheet = Reloaded(workbook);
        sheet.LastRowUsed()!.RowNumber().ShouldBe(1);
    }

    [TestMethod]
    public void Writes_Every_Booking_Field_Of_A_Slot_Request()
    {
        //given
        var workbook = new XLWorkbook();
        var request = Request(
            DayOfWeek.Tuesday,
            SlotWindowType.Afternoon,
            rank: 2,
            studentName: "דנה כהן",
            nationalId: "000000034",
            phone: "050-0000003",
            transmission: Transmission.Manual,
            sessionType: SessionType.Double,
            targetCount: 3,
            constraint: "רק אחרי 16:00");

        //when
        RequestDetailSheet.AddTo(workbook, [request]);

        //then
        var sheet = Reloaded(workbook);
        RowOf(sheet, 2).ShouldBe(
            [
                "שלישי",
                "אחה״צ",
                "דנה כהן",
                "000000034",
                "050-0000003",
                "ידני",
                "כפול",
                "2",
                "3",
                "רק אחרי 16:00"
            ]);
        sheet.Cell(2, 8).DataType.ShouldBe(XLDataType.Number);
        sheet.Cell(2, 9).DataType.ShouldBe(XLDataType.Number);
    }

    [TestMethod]
    public void Sorts_By_Day_Then_Slot_Then_Rank()
    {
        //given
        var workbook = new XLWorkbook();
        SlotRequestDetail[] requests =
        [
            Request(DayOfWeek.Friday, SlotWindowType.Noon, rank: 1),
            Request(DayOfWeek.Sunday, SlotWindowType.Evening, rank: 1),
            Request(DayOfWeek.Sunday, SlotWindowType.Morning, rank: 3),
            Request(DayOfWeek.Monday, SlotWindowType.Morning, rank: 2),
            Request(DayOfWeek.Sunday, SlotWindowType.Morning, rank: 1)
        ];

        //when
        RequestDetailSheet.AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => $"{row[0]} {row[1]} {row[7]}").ShouldBe(
            [
                "ראשון בוקר 1",
                "ראשון בוקר 3",
                "ראשון ערב 1",
                "שני בוקר 2",
                "שישי צהריים 1"
            ]);
    }

    [TestMethod]
    public void Orders_Students_With_The_Same_Slot_And_Rank_By_Name()
    {
        //given
        var workbook = new XLWorkbook();
        SlotRequestDetail[] requests =
        [
            Request(studentName: "דנה כהן", nationalId: "000000034"),
            Request(studentName: "Smoke Student E", nationalId: "000000067"),
            Request(studentName: "Smoke Student A", nationalId: "000000018")
        ];

        //when
        RequestDetailSheet.AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => row[2]).ShouldBe(
            [
                "Smoke Student A",
                "Smoke Student E",
                "דנה כהן"
            ]);
    }

    [TestMethod]
    public void Keeps_National_Id_And_Phone_As_Text()
    {
        //given
        var workbook = new XLWorkbook();
        var request = Request(nationalId: "000000018", phone: "050-0000001");

        //when
        RequestDetailSheet.AddTo(workbook, [request]);

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(2, 4).DataType.ShouldBe(XLDataType.Text);
        sheet.Cell(2, 4).GetText().ShouldBe("000000018");
        sheet.Cell(2, 5).DataType.ShouldBe(XLDataType.Text);
        sheet.Cell(2, 5).GetText().ShouldBe("050-0000001");
        sheet.Column(4).Style.NumberFormat.Format.ShouldBe("@");
        sheet.Column(5).Style.NumberFormat.Format.ShouldBe("@");
    }

    [TestMethod]
    public void Leaves_The_Constraint_Empty_When_There_Is_None()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(constraint: null)]);

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(2, ConstraintsColumn).IsEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public void Writes_A_Formula_Like_Constraint_As_Plain_Text()
    {
        //given
        var workbook = new XLWorkbook();
        const string constraint = "=HYPERLINK(\"http://example.com\",\"click\")";

        //when
        RequestDetailSheet.AddTo(workbook, [Request(constraint: constraint)]);

        //then
        var sheet = Reloaded(workbook);
        var cell = sheet.Cell(2, ConstraintsColumn);
        cell.HasFormula.ShouldBeFalse();
        cell.DataType.ShouldBe(XLDataType.Text);
        cell.GetText().ShouldBe(constraint);
    }

    [TestMethod]
    public void Wraps_Long_Constraints_In_A_Fixed_Width_Column()
    {
        //given
        var workbook = new XLWorkbook();
        var constraint = new string('א', SlotConstraint.MaxLength);

        //when
        RequestDetailSheet.AddTo(workbook, [Request(constraint: constraint)]);

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(2, ConstraintsColumn).GetText().ShouldBe(constraint);
        sheet.Column(ConstraintsColumn).Style.Alignment.WrapText.ShouldBeTrue();
        sheet.Column(ConstraintsColumn).Width.ShouldBe(40);
    }

    [TestMethod]
    public void Is_Right_To_Left_With_A_Frozen_Filtered_Header()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(), Request(DayOfWeek.Monday)]);

        //then
        var sheet = Reloaded(workbook);
        sheet.RightToLeft.ShouldBeTrue();
        sheet.SheetView.SplitRow.ShouldBe(1);
        sheet.AutoFilter.IsEnabled.ShouldBeTrue();
        sheet.AutoFilter.Range.RangeAddress.ToString().ShouldBe("A1:J3");
    }

    [TestMethod]
    [DataRow(DayOfWeek.Sunday, "ראשון")]
    [DataRow(DayOfWeek.Monday, "שני")]
    [DataRow(DayOfWeek.Tuesday, "שלישי")]
    [DataRow(DayOfWeek.Wednesday, "רביעי")]
    [DataRow(DayOfWeek.Thursday, "חמישי")]
    [DataRow(DayOfWeek.Friday, "שישי")]
    public void Names_The_Day_In_Hebrew(DayOfWeek day, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(day)]);

        //then
        Reloaded(workbook).Cell(2, 1).GetText().ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(SlotWindowType.Morning, "בוקר")]
    [DataRow(SlotWindowType.Noon, "צהריים")]
    [DataRow(SlotWindowType.Afternoon, "אחה״צ")]
    [DataRow(SlotWindowType.Evening, "ערב")]
    public void Names_The_Slot_In_Hebrew(SlotWindowType window, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(window: window)]);

        //then
        Reloaded(workbook).Cell(2, 2).GetText().ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(Transmission.Automatic, "אוטומטי")]
    [DataRow(Transmission.Manual, "ידני")]
    public void Names_The_Transmission_In_Hebrew(Transmission transmission, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(transmission: transmission)]);

        //then
        Reloaded(workbook).Cell(2, 6).GetText().ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(SessionType.Single, "יחיד")]
    [DataRow(SessionType.Double, "כפול")]
    public void Names_The_Session_Type_In_Hebrew(SessionType sessionType, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(sessionType: sessionType)]);

        //then
        Reloaded(workbook).Cell(2, 7).GetText().ShouldBe(expected);
    }

    private static SlotRequestDetail Request(
        DayOfWeek day = DayOfWeek.Sunday,
        SlotWindowType window = SlotWindowType.Morning,
        int rank = 1,
        string studentName = "Smoke Student A",
        string nationalId = "000000018",
        string phone = "050-0000001",
        Transmission transmission = Transmission.Automatic,
        SessionType sessionType = SessionType.Single,
        int targetCount = 1,
        string? constraint = null)
    {
        return new SlotRequestDetail(
            day,
            window,
            studentName,
            nationalId,
            phone,
            transmission,
            sessionType,
            rank,
            targetCount,
            constraint);
    }

    private static IXLWorksheet Reloaded(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return new XLWorkbook(stream).Worksheet(RequestDetailSheet.Name);
    }

    private static string[] RowOf(IXLWorksheet sheet, int row)
    {
        return Enumerable
                   .Range(1, ColumnCount)
                   .Select(column => sheet.Cell(row, column).GetFormattedString())
                   .ToArray();
    }

    private static IReadOnlyList<string[]> RequestRowsOf(IXLWorksheet sheet)
    {
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        return Enumerable
                   .Range(2, lastRow - 1)
                   .Select(row => RowOf(sheet, row))
                   .ToList();
    }
}
```

The test data reuses the smoke roster's valid national IDs (`000000018`, `000000034`, `000000067`) so the two stay recognisable side by side. `Orders_Students_With_The_Same_Slot_And_Rank_By_Name` pins ordinal order: Latin names sort before Hebrew ones, and the national ID breaks a tie between equal names (README decision 3).

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~RequestDetailSheetTest"`
Expected: FAIL — the build breaks with `CS0246: The type or namespace name 'SlotRequestDetail' could not be found` and `CS0103: The name 'RequestDetailSheet' does not exist in the current context`.

- [ ] **Step 3: The `SlotRequestDetail` record**

Replace `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` with:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface ISubmissionQueries
{
    Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId);

    Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId);
}

public record SubmissionStats(int StudentsSubmitted, int TotalPicks, DateTimeOffset? LastSubmissionAtUtc);

public record SlotRequestDetail(
    DayOfWeek Day,
    SlotWindowType Window,
    string StudentName,
    string NationalId,
    string Phone,
    Transmission Transmission,
    SessionType SessionType,
    int Rank,
    int TargetCount,
    string? Constraint);
```

The query member that returns it arrives in task 2 together with its implementation, so every commit builds.

- [ ] **Step 4: The Hebrew labels**

Create `src\DrivingLessons.Infrastructure\Excel\HebrewExcelLabels.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public static class HebrewExcelLabels
{
    public static string DayOf(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Sunday => "ראשון",
            DayOfWeek.Monday => "שני",
            DayOfWeek.Tuesday => "שלישי",
            DayOfWeek.Wednesday => "רביעי",
            DayOfWeek.Thursday => "חמישי",
            DayOfWeek.Friday => "שישי",
            _ => throw new ArgumentOutOfRangeException(nameof(day), day, null)
        };
    }

    public static string WindowOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => "בוקר",
            SlotWindowType.Noon => "צהריים",
            SlotWindowType.Afternoon => "אחה״צ",
            SlotWindowType.Evening => "ערב",
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, null)
        };
    }

    public static string TransmissionOf(Transmission transmission)
    {
        return transmission switch
        {
            Transmission.Automatic => "אוטומטי",
            Transmission.Manual => "ידני",
            _ => throw new ArgumentOutOfRangeException(nameof(transmission), transmission, null)
        };
    }

    public static string SessionTypeOf(SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Single => "יחיד",
            SessionType.Double => "כפול",
            _ => throw new ArgumentOutOfRangeException(nameof(sessionType), sessionType, null)
        };
    }
}
```

`אחה״צ` uses the Hebrew gershayim `״` (U+05F4), exactly as `client\public\i18n\he.json` → `weekGrid.windows.afternoon`. Saturday has no slot in the grid (requirements §5.3), so it falls to the throwing arm like any undefined value.

- [ ] **Step 5: The sheet**

Create `src\DrivingLessons.Infrastructure\Excel\RequestDetailSheet.cs`:

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Infrastructure.Excel;

public static class RequestDetailSheet
{
    public const string Name = "פירוט בקשות";

    private const int HeaderRow = 1;
    private const int FirstRequestRow = 2;
    private const int FirstColumn = 1;
    private const int NationalIdColumn = 4;
    private const int PhoneColumn = 5;
    private const int ConstraintsColumn = 10;
    private const string TextFormat = "@";

    private static readonly string[] Headers =
    [
        "יום",
        "משבצת",
        "שם התלמיד/ה",
        "תעודת זהות",
        "טלפון",
        "תיבת הילוכים",
        "סוג שיעור",
        "דירוג",
        "יעד שיעורים",
        "אילוצים"
    ];

    private static readonly double[] ColumnWidths =
    [
        8,
        10,
        24,
        12,
        14,
        13,
        10,
        7,
        11,
        40
    ];

    public static void AddTo(XLWorkbook workbook, IReadOnlyCollection<SlotRequestDetail> requests)
    {
        var sheet = workbook.Worksheets.Add(Name);
        sheet.RightToLeft = true;

        FormatColumns(sheet);
        WriteHeader(sheet);
        WriteRequests(sheet, requests);

        var lastRow = HeaderRow + requests.Count;
        sheet.Range(HeaderRow, FirstColumn, lastRow, Headers.Length).SetAutoFilter();
        sheet.SheetView.FreezeRows(HeaderRow);
    }

    private static void FormatColumns(IXLWorksheet sheet)
    {
        for (var index = 0; index < ColumnWidths.Length; index++)
        {
            sheet.Column(index + FirstColumn).Width = ColumnWidths[index];
        }

        sheet.Column(NationalIdColumn).Style.NumberFormat.Format = TextFormat;
        sheet.Column(PhoneColumn).Style.NumberFormat.Format = TextFormat;
        sheet.Column(ConstraintsColumn).Style.Alignment.WrapText = true;
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        for (var index = 0; index < Headers.Length; index++)
        {
            sheet.Cell(HeaderRow, index + FirstColumn).Value = Headers[index];
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
    }

    private static void WriteRequests(IXLWorksheet sheet, IReadOnlyCollection<SlotRequestDetail> requests)
    {
        var ordered = requests
                          .OrderBy(x => x.Day)
                          .ThenBy(x => x.Window)
                          .ThenBy(x => x.Rank)
                          .ThenBy(x => x.StudentName, StringComparer.Ordinal)
                          .ThenBy(x => x.NationalId, StringComparer.Ordinal);

        var row = FirstRequestRow;

        foreach (var request in ordered)
        {
            WriteRequest(sheet.Row(row), request);
            row++;
        }
    }

    private static void WriteRequest(IXLRow row, SlotRequestDetail request)
    {
        var values = new XLCellValue[]
        {
            HebrewExcelLabels.DayOf(request.Day),
            HebrewExcelLabels.WindowOf(request.Window),
            request.StudentName,
            request.NationalId,
            request.Phone,
            HebrewExcelLabels.TransmissionOf(request.Transmission),
            HebrewExcelLabels.SessionTypeOf(request.SessionType),
            request.Rank,
            request.TargetCount,
            ConstraintOf(request.Constraint)
        };

        for (var index = 0; index < values.Length; index++)
        {
            row.Cell(index + FirstColumn).Value = values[index];
        }
    }

    private static XLCellValue ConstraintOf(string? constraint)
    {
        if (constraint is null)
        {
            return Blank.Value;
        }

        return constraint;
    }
}
```

`DayOfWeek` orders Sunday (0) → Friday (5) and `SlotWindowType` Morning (10) → Evening (40), so ordering by the enums is the spec's order. Columns are formatted before any value is written, so every data cell inherits the text format and the wrap. A string assigned to `Value` is always a text value in ClosedXML — never parsed as a number or a formula — which is what keeps `000000018` and `=HYPERLINK(…)` intact.

- [ ] **Step 6: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~RequestDetailSheetTest"`
Expected: PASS — 24 tests (10 single tests + 6 + 4 + 2 + 2 data rows).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings beyond the existing `NU1903` / `MSTEST0001` / `CS8618`), every test PASS.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Queries/ISubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/Excel/HebrewExcelLabels.cs \
  src/DrivingLessons.Infrastructure/Excel/RequestDetailSheet.cs \
  tests/DrivingLessons.Application.Test/Excel/RequestDetailSheetTest.cs
git commit -m "feat(excel): Hebrew request detail sheet, one sorted row per slot request

Day, slot, student name, national ID, phone, transmission, session type,
rank, target count and constraints, sorted by day, slot and rank.
Right-to-left, text-typed IDs and phones, wrapped constraints."
```

---

**Next:** [task-02-detail-query-and-generator.md](task-02-detail-query-and-generator.md)
