using ClosedXML.Excel;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class RequestDetailSheetTest
{
    private const int ColumnCount = 7;
    private const int TitleRow = 1;
    private const int HeaderRow = 2;
    private const int FirstRequestRow = 3;
    private const int SessionTypeColumn = 4;
    private const int PositionColumn = 5;
    private const int TargetCountColumn = 6;
    private const int ConstraintsColumn = 7;
    private const string SheetName = "Smoke Auto";
    private const string CarName = "Smoke Auto";
    private static readonly Guid CarId = Guid.NewGuid();

    [TestMethod]
    public void Writes_The_Hebrew_Header_Row()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook, Request());

        //then
        var sheet = Reloaded(workbook);
        RowOf(sheet, HeaderRow).ShouldBe(
            [
                "יום",
                "משבצת",
                "שם התלמיד/ה",
                "סוג שיעור",
                "שיעור אפשרי",
                "שיעורים מבוקשים",
                "אילוצים"
            ]);
        sheet.Cell(HeaderRow, 1).Style.Font.Bold.ShouldBeTrue();
    }

    [TestMethod]
    public void Writes_The_Full_Car_Name_As_A_Bold_Title_Across_The_Columns()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, "Corolla (2)", "Corolla", [Request()]);

        //then
        var sheet = Reloaded(workbook, "Corolla (2)");
        var title = sheet.Cell(TitleRow, 1);
        title.GetText().ShouldBe("Corolla");
        title.Style.Font.Bold.ShouldBeTrue();
        title.MergedRange().RangeAddress.ToString().ShouldBe("A1:G1");
    }

    [TestMethod]
    public void Lists_Only_The_Title_And_Header_When_There_Are_No_Requests()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook);

        //then
        var sheet = Reloaded(workbook);
        sheet.LastRowUsed()!.RowNumber().ShouldBe(HeaderRow);
    }

    [TestMethod]
    public void Writes_Every_Booking_Field_Of_A_Slot_Request()
    {
        //given
        var workbook = new XLWorkbook();
        var request = Request(
            day: DayOfWeek.Tuesday,
            window: SlotWindowType.Afternoon,
            studentName: "דנה כהן",
            sessionType: SessionType.Double,
            targetCount: 3,
            pickCount: 5,
            constraint: "רק אחרי 16:00");

        //when
        AddTo(workbook, [request]);

        //then
        var sheet = Reloaded(workbook);
        RowOf(sheet, FirstRequestRow).ShouldBe(
            [
                "שלישי",
                "אחה״צ",
                "דנה כהן",
                "כפול",
                "1 מתוך 5",
                "3",
                "רק אחרי 16:00"
            ]);
        sheet.Cell(FirstRequestRow, PositionColumn).DataType.ShouldBe(XLDataType.Text);
        sheet.Cell(FirstRequestRow, TargetCountColumn).DataType.ShouldBe(XLDataType.Number);
    }

    [TestMethod]
    public void Sorts_By_Day_Then_Slot()
    {
        //given
        var workbook = new XLWorkbook();
        SlotRequestDetail[] requests =
        [
            Request(day: DayOfWeek.Friday, window: SlotWindowType.Noon),
            Request(day: DayOfWeek.Sunday, window: SlotWindowType.Evening),
            Request(day: DayOfWeek.Sunday, window: SlotWindowType.Morning),
            Request(day: DayOfWeek.Monday, window: SlotWindowType.Morning)
        ];

        //when
        AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => $"{row[0]} {row[1]}").ShouldBe(
            [
                "ראשון בוקר",
                "ראשון ערב",
                "שני בוקר",
                "שישי צהריים"
            ]);
    }

    [TestMethod]
    public void Orders_Students_In_The_Same_Slot_By_Name()
    {
        //given
        var workbook = new XLWorkbook();
        SlotRequestDetail[] requests =
        [
            Request(studentName: "דנה כהן"),
            Request(studentName: "Smoke Student B"),
            Request(studentName: "Smoke Student A")
        ];

        //when
        AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => row[2]).ShouldBe(
            [
                "Smoke Student A",
                "Smoke Student B",
                "דנה כהן"
            ]);
    }

    [TestMethod]
    public void Numbers_A_Students_Slots_From_Sunday_Morning_To_Friday_Evening()
    {
        //given
        var workbook = new XLWorkbook();
        var submissionId = Guid.NewGuid();
        SlotRequestDetail[] requests =
        [
            Request(submissionId, DayOfWeek.Friday, SlotWindowType.Noon, pickCount: 4),
            Request(submissionId, DayOfWeek.Sunday, SlotWindowType.Evening, pickCount: 4),
            Request(submissionId, DayOfWeek.Monday, SlotWindowType.Morning, pickCount: 4),
            Request(submissionId, DayOfWeek.Sunday, SlotWindowType.Morning, pickCount: 4)
        ];

        //when
        AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => $"{row[0]} {row[1]} {row[4]}").ShouldBe(
            [
                "ראשון בוקר 1 מתוך 4",
                "ראשון ערב 2 מתוך 4",
                "שני בוקר 3 מתוך 4",
                "שישי צהריים 4 מתוך 4"
            ]);
    }

    [TestMethod]
    public void Numbers_Each_Submission_On_Its_Own_Even_With_The_Same_Name()
    {
        //given
        var workbook = new XLWorkbook();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        SlotRequestDetail[] requests =
        [
            Request(first, DayOfWeek.Monday, SlotWindowType.Morning, "דנה כהן", pickCount: 2),
            Request(first, DayOfWeek.Sunday, SlotWindowType.Morning, "דנה כהן", pickCount: 2),
            Request(second, DayOfWeek.Sunday, SlotWindowType.Evening, "דנה כהן", pickCount: 1)
        ];

        //when
        AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => $"{row[0]} {row[1]} {row[4]}").ShouldBe(
            [
                "ראשון בוקר 1 מתוך 2",
                "ראשון ערב 1 מתוך 1",
                "שני בוקר 2 מתוך 2"
            ]);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void Writes_The_Target_Count_As_A_Number(int targetCount)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook, Request(targetCount: targetCount));

        //then
        var cell = Reloaded(workbook).Cell(FirstRequestRow, TargetCountColumn);
        cell.DataType.ShouldBe(XLDataType.Number);
        cell.GetValue<int>().ShouldBe(targetCount);
    }

    [TestMethod]
    [DataRow("12-345-67")]
    [DataRow("0501234")]
    [DataRow("=A1")]
    public void Keeps_A_Car_Name_That_Looks_Like_A_Number_Or_A_Formula_As_Text(string carName)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, SheetName, carName, [Request()]);

        //then
        var cell = Reloaded(workbook).Cell(TitleRow, 1);
        cell.HasFormula.ShouldBeFalse();
        cell.DataType.ShouldBe(XLDataType.Text);
        cell.GetText().ShouldBe(carName);
    }

    [TestMethod]
    public void Leaves_The_Constraint_Empty_When_There_Is_None()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook, Request(constraint: null));

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(FirstRequestRow, ConstraintsColumn).IsEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public void Writes_A_Formula_Like_Constraint_As_Plain_Text()
    {
        //given
        var workbook = new XLWorkbook();
        const string constraint = "=HYPERLINK(\"http://example.com\",\"click\")";

        //when
        AddTo(workbook, Request(constraint: constraint));

        //then
        var sheet = Reloaded(workbook);
        var cell = sheet.Cell(FirstRequestRow, ConstraintsColumn);
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
        AddTo(workbook, Request(constraint: constraint));

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(FirstRequestRow, ConstraintsColumn).GetText().ShouldBe(constraint);
        sheet.Column(ConstraintsColumn).Style.Alignment.WrapText.ShouldBeTrue();
        sheet.Column(ConstraintsColumn).Width.ShouldBe(40);
    }

    [TestMethod]
    public void Is_Right_To_Left_With_A_Frozen_Filtered_Header()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook, Request(), Request(day: DayOfWeek.Monday));

        //then
        var sheet = Reloaded(workbook);
        sheet.RightToLeft.ShouldBeTrue();
        sheet.SheetView.SplitRow.ShouldBe(HeaderRow);
        sheet.AutoFilter.IsEnabled.ShouldBeTrue();
        sheet.AutoFilter.Range.RangeAddress.ToString().ShouldBe("A2:G4");
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
        AddTo(workbook, Request(day: day));

        //then
        Reloaded(workbook).Cell(FirstRequestRow, 1).GetText().ShouldBe(expected);
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
        AddTo(workbook, Request(window: window));

        //then
        Reloaded(workbook).Cell(FirstRequestRow, 2).GetText().ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(SessionType.Single, "יחיד")]
    [DataRow(SessionType.Double, "כפול")]
    public void Names_The_Session_Type_In_Hebrew(SessionType sessionType, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        AddTo(workbook, Request(sessionType: sessionType));

        //then
        Reloaded(workbook).Cell(FirstRequestRow, SessionTypeColumn).GetText().ShouldBe(expected);
    }

    private static SlotRequestDetail Request(
        Guid? submissionId = null,
        DayOfWeek day = DayOfWeek.Sunday,
        SlotWindowType window = SlotWindowType.Morning,
        string studentName = "Smoke Student A",
        SessionType sessionType = SessionType.Single,
        int targetCount = 1,
        int pickCount = 1,
        string? constraint = null)
    {
        return new SlotRequestDetail(
            submissionId ?? Guid.NewGuid(),
            day,
            window,
            studentName,
            CarId,
            CarName,
            sessionType,
            targetCount,
            pickCount,
            constraint);
    }

    private static void AddTo(XLWorkbook workbook, params SlotRequestDetail[] requests)
    {
        RequestDetailSheet.AddTo(workbook, SheetName, CarName, requests);
    }

    private static IXLWorksheet Reloaded(XLWorkbook workbook)
    {
        return Reloaded(workbook, SheetName);
    }

    private static IXLWorksheet Reloaded(XLWorkbook workbook, string sheetName)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return new XLWorkbook(stream).Worksheet(sheetName);
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
                   .Range(FirstRequestRow, lastRow - HeaderRow)
                   .Select(row => RowOf(sheet, row))
                   .ToList();
    }
}
