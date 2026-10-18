using ClosedXML.Excel;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class RequestDetailSheetTest
{
    private const int ColumnCount = 8;
    private const int CarColumn = 4;
    private const int RequestedLessonsColumn = 7;
    private const int ConstraintsColumn = 8;

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
                "רכב",
                "סוג שיעור",
                "דירוג",
                "שיעורים מבוקשים",
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
            carName: "Smoke Manual",
            sessionType: SessionType.Double,
            targetCount: 3,
            pickCount: 5,
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
                "Smoke Manual",
                "כפול",
                "2",
                "3 מתוך 5",
                "רק אחרי 16:00"
            ]);
        sheet.Cell(2, 6).DataType.ShouldBe(XLDataType.Number);
        sheet.Cell(2, RequestedLessonsColumn).DataType.ShouldBe(XLDataType.Text);
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
        RequestRowsOf(sheet).Select(row => $"{row[0]} {row[1]} {row[5]}").ShouldBe(
            [
                "ראשון בוקר 1",
                "ראשון בוקר 3",
                "ראשון ערב 1",
                "שני בוקר 2",
                "שישי צהריים 1"
            ]);
    }

    [TestMethod]
    public void Orders_Students_With_The_Same_Slot_And_Rank_By_Name_Then_Car()
    {
        //given
        var workbook = new XLWorkbook();
        SlotRequestDetail[] requests =
        [
            Request(studentName: "דנה כהן", carName: "Smoke Old"),
            Request(studentName: "Smoke Student A", carName: "Smoke Manual"),
            Request(studentName: "Smoke Student A", carName: "Smoke Auto")
        ];

        //when
        RequestDetailSheet.AddTo(workbook, requests);

        //then
        var sheet = Reloaded(workbook);
        RequestRowsOf(sheet).Select(row => $"{row[2]} {row[3]}").ShouldBe(
            [
                "Smoke Student A Smoke Auto",
                "Smoke Student A Smoke Manual",
                "דנה כהן Smoke Old"
            ]);
    }

    [TestMethod]
    [DataRow(1, 1, "1 מתוך 1")]
    [DataRow(1, 3, "1 מתוך 3")]
    [DataRow(2, 5, "2 מתוך 5")]
    public void Writes_The_Requested_Lessons_As_Target_Out_Of_Picks(int targetCount, int pickCount, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(targetCount: targetCount, pickCount: pickCount)]);

        //then
        Reloaded(workbook).Cell(2, RequestedLessonsColumn).GetText().ShouldBe(expected);
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
        RequestDetailSheet.AddTo(workbook, [Request(carName: carName)]);

        //then
        var cell = Reloaded(workbook).Cell(2, CarColumn);
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
        sheet.AutoFilter.Range.RangeAddress.ToString().ShouldBe("A1:H3");
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
    [DataRow(SessionType.Single, "יחיד")]
    [DataRow(SessionType.Double, "כפול")]
    public void Names_The_Session_Type_In_Hebrew(SessionType sessionType, string expected)
    {
        //given
        var workbook = new XLWorkbook();

        //when
        RequestDetailSheet.AddTo(workbook, [Request(sessionType: sessionType)]);

        //then
        Reloaded(workbook).Cell(2, 5).GetText().ShouldBe(expected);
    }

    private static SlotRequestDetail Request(
        DayOfWeek day = DayOfWeek.Sunday,
        SlotWindowType window = SlotWindowType.Morning,
        int rank = 1,
        string studentName = "Smoke Student A",
        string carName = "Smoke Auto",
        SessionType sessionType = SessionType.Single,
        int targetCount = 1,
        int pickCount = 1,
        string? constraint = null)
    {
        return new SlotRequestDetail(
            day,
            window,
            studentName,
            carName,
            sessionType,
            rank,
            targetCount,
            pickCount,
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
