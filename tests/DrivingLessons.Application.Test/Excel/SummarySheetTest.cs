using ClosedXML.Excel;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class SummarySheetTest
{
    private const int FirstDayColumn = 2;
    private const int FridayColumn = 7;
    private const int MorningRow = 2;
    private const int NoonRow = 3;
    private const int AfternoonRow = 4;
    private const int EveningRow = 5;
    private static readonly DateOnly WeekStart = new(2026, 10, 4);

    [TestMethod]
    public void Is_A_Right_To_Left_Sheet_Named_In_Hebrew()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, Grid(), NoCounts());

        //then
        var sheet = Reloaded(workbook);
        sheet.Name.ShouldBe("סיכום");
        sheet.RightToLeft.ShouldBeTrue();
    }

    [TestMethod]
    public void Heads_The_Columns_With_Sunday_To_Friday_And_Their_Dates()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, Grid(), NoCounts());

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(1, 1).IsEmpty().ShouldBeTrue();
        Enumerable
            .Range(FirstDayColumn, 6)
            .Select(column => sheet.Cell(1, column).GetText())
            .ShouldBe(
                [
                    "ראשון 4.10",
                    "שני 5.10",
                    "שלישי 6.10",
                    "רביעי 7.10",
                    "חמישי 8.10",
                    "שישי 9.10"
                ]);
        sheet.Cell(1, FirstDayColumn).Style.Font.Bold.ShouldBeTrue();
    }

    [TestMethod]
    public void Labels_The_Rows_With_The_Four_Slots_And_Their_Hours()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, Grid(), NoCounts());

        //then
        var sheet = Reloaded(workbook);
        Enumerable
            .Range(MorningRow, 4)
            .Select(row => sheet.Cell(row, 1).GetText())
            .ShouldBe(
                [
                    "בוקר 07:00-12:00",
                    "צהריים 12:00-15:00",
                    "אחה״צ 15:00-18:00",
                    "ערב 18:00-22:00"
                ]);
        sheet.Cell(MorningRow, 1).Style.Font.Bold.ShouldBeTrue();
    }

    [TestMethod]
    public void Writes_The_Request_Count_Of_Each_Open_Slot()
    {
        //given
        var workbook = new XLWorkbook();
        var slots = Grid();
        var counts = new Dictionary<Guid, int>
        {
            [SlotAt(slots, DayOfWeek.Sunday, SlotWindowType.Morning)] = 3,
            [SlotAt(slots, DayOfWeek.Wednesday, SlotWindowType.Evening)] = 1,
            [SlotAt(slots, DayOfWeek.Friday, SlotWindowType.Noon)] = 2
        };

        //when
        SummarySheet.AddTo(workbook, WeekStart, slots, counts);

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(MorningRow, 2).GetValue<int>().ShouldBe(3);
        sheet.Cell(EveningRow, 5).GetValue<int>().ShouldBe(1);
        sheet.Cell(NoonRow, FridayColumn).GetValue<int>().ShouldBe(2);
        sheet.Cell(MorningRow, 2).DataType.ShouldBe(XLDataType.Number);
    }

    [TestMethod]
    public void Writes_Zero_For_An_Open_Slot_Without_Requests()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, Grid(), NoCounts());

        //then
        var cell = Reloaded(workbook).Cell(AfternoonRow, 3);
        cell.DataType.ShouldBe(XLDataType.Number);
        cell.GetValue<int>().ShouldBe(0);
    }

    [TestMethod]
    public void Marks_An_Unavailable_Slot_Blocked()
    {
        //given
        var workbook = new XLWorkbook();
        var slots = Grid((DayOfWeek.Tuesday, SlotWindowType.Noon));

        //when
        SummarySheet.AddTo(workbook, WeekStart, slots, NoCounts());

        //then
        var cell = Reloaded(workbook).Cell(NoonRow, 4);
        cell.GetText().ShouldBe("לא זמין");
        cell.Style.Fill.BackgroundColor.Color.ToArgb().ShouldBe(XLColor.LightGray.Color.ToArgb());
    }

    [TestMethod]
    public void Hides_The_Count_Of_A_Slot_Marked_Unavailable_After_It_Was_Requested()
    {
        //given
        var workbook = new XLWorkbook();
        var slots = Grid((DayOfWeek.Monday, SlotWindowType.Morning));
        var counts = new Dictionary<Guid, int>
        {
            [SlotAt(slots, DayOfWeek.Monday, SlotWindowType.Morning)] = 2
        };

        //when
        SummarySheet.AddTo(workbook, WeekStart, slots, counts);

        //then
        var cell = Reloaded(workbook).Cell(MorningRow, 3);
        cell.DataType.ShouldBe(XLDataType.Text);
        cell.GetText().ShouldBe("לא זמין");
    }

    [TestMethod]
    public void Leaves_Friday_Afternoon_And_Evening_Out_Of_The_Grid()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, Grid(), NoCounts());

        //then
        var sheet = Reloaded(workbook);
        ShouldBeOutsideTheGrid(sheet.Cell(AfternoonRow, FridayColumn));
        ShouldBeOutsideTheGrid(sheet.Cell(EveningRow, FridayColumn));
    }

    [TestMethod]
    public void Borders_Every_Slot_Of_The_Grid()
    {
        //given
        var workbook = new XLWorkbook();
        var slots = Grid((DayOfWeek.Thursday, SlotWindowType.Evening));

        //when
        SummarySheet.AddTo(workbook, WeekStart, slots, NoCounts());

        //then
        var sheet = Reloaded(workbook);
        sheet.Cell(MorningRow, 2).Style.Border.TopBorder.ShouldBe(XLBorderStyleValues.Thin);
        sheet.Cell(EveningRow, 6).Style.Border.BottomBorder.ShouldBe(XLBorderStyleValues.Thin);
        sheet.Cell(NoonRow, FridayColumn).Style.Border.LeftBorder.ShouldBe(XLBorderStyleValues.Thin);
    }

    [TestMethod]
    public void Leaves_The_Grid_Empty_When_There_Are_No_Slots()
    {
        //given
        var workbook = new XLWorkbook();

        //when
        SummarySheet.AddTo(workbook, WeekStart, [], NoCounts());

        //then
        var sheet = Reloaded(workbook);
        sheet.Range(MorningRow, FirstDayColumn, EveningRow, FridayColumn).IsEmpty().ShouldBeTrue();
        sheet.Cell(1, FirstDayColumn).GetText().ShouldBe("ראשון 4.10");
        sheet.Cell(MorningRow, 1).GetText().ShouldBe("בוקר 07:00-12:00");
    }

    private static void ShouldBeOutsideTheGrid(IXLCell cell)
    {
        cell.IsEmpty().ShouldBeTrue();
        cell.Style.Border.TopBorder.ShouldBe(XLBorderStyleValues.None);
        cell.Style.Fill.BackgroundColor.Color.ToArgb().ShouldNotBe(XLColor.LightGray.Color.ToArgb());
    }

    private static IReadOnlyCollection<SlotForGetWeekScheduleResponse> Grid(
        params (DayOfWeek Day, SlotWindowType Window)[] unavailable)
    {
        return WeekGridDefinition
                   .Days
                   .SelectMany(day => WeekGridDefinition
                                          .WindowsFor(day)
                                          .Select(window => new SlotForGetWeekScheduleResponse
                                          {
                                              Id = Guid.NewGuid(),
                                              Day = day,
                                              Window = window,
                                              State = unavailable.Contains((day, window))
                                                          ? SlotState.Unavailable
                                                          : SlotState.Open
                                          }))
                   .ToList();
    }

    private static Guid SlotAt(
        IReadOnlyCollection<SlotForGetWeekScheduleResponse> slots,
        DayOfWeek day,
        SlotWindowType window)
    {
        return slots.Single(x => x.Day == day && x.Window == window).Id;
    }

    private static IReadOnlyDictionary<Guid, int> NoCounts()
    {
        return new Dictionary<Guid, int>();
    }

    private static IXLWorksheet Reloaded(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return new XLWorkbook(stream).Worksheet(SummarySheet.Name);
    }
}
