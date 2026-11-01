using ClosedXML.Excel;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public static class SummarySheet
{
    public const string Name = "סיכום";

    private const string BlockedLabel = "לא זמין";
    private const int FirstRow = 1;
    private const int LabelColumn = 1;
    private const int FirstDayColumn = 2;
    private const int TitleRows = 1;
    private const int HeaderRows = 1;
    private const int RowsBetweenGrids = 1;
    private const double LabelColumnWidth = 18;
    private const double DayColumnWidth = 12;

    private static readonly SlotWindowType[] Windows =
    [
        SlotWindowType.Morning,
        SlotWindowType.Noon,
        SlotWindowType.Afternoon,
        SlotWindowType.Evening
    ];

    public static void AddTo(
        XLWorkbook workbook,
        DateOnly weekStart,
        IReadOnlyCollection<SlotForGetWeekScheduleResponse> slots,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var sheet = NewSheet(workbook);

        WriteGrid(sheet, FirstRow, weekStart, slots, counts);
    }

    public static void AddForTeachersTo(
        XLWorkbook workbook,
        DateOnly weekStart,
        IReadOnlyList<TeacherWeekGrid> grids,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var sheet = NewSheet(workbook);
        var blockHeight = TitleRows + HeaderRows + Windows.Length + RowsBetweenGrids;

        for (var index = 0; index < grids.Count; index++)
        {
            var grid = grids[index];
            var titleRow = FirstRow + (index * blockHeight);

            WriteTitle(sheet, titleRow, grid.TeacherName);
            WriteGrid(sheet, titleRow + TitleRows, weekStart, grid.Slots, counts);
        }
    }

    private static IXLWorksheet NewSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add(Name);
        sheet.RightToLeft = true;

        FormatColumns(sheet);

        return sheet;
    }

    private static void WriteTitle(IXLWorksheet sheet, int row, string teacherName)
    {
        var lastColumn = FirstDayColumn + WeekGridDefinition.Days.Count - 1;
        var title = sheet.Cell(row, LabelColumn);
        title.Value = teacherName;
        title.Style.Font.Bold = true;

        sheet.Range(row, LabelColumn, row, lastColumn).Merge();
    }

    private static void WriteGrid(
        IXLWorksheet sheet,
        int headerRow,
        DateOnly weekStart,
        IReadOnlyCollection<SlotForGetWeekScheduleResponse> slots,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var firstSlotRow = headerRow + HeaderRows;

        WriteDayHeaders(sheet, headerRow, weekStart);
        WriteSlotLabels(sheet, firstSlotRow);
        WriteSlotCells(sheet, firstSlotRow, slots, counts);
    }

    private static void FormatColumns(IXLWorksheet sheet)
    {
        sheet.Column(LabelColumn).Width = LabelColumnWidth;
        sheet.Column(LabelColumn).Style.Font.Bold = true;

        for (var index = 0; index < WeekGridDefinition.Days.Count; index++)
        {
            sheet.Column(index + FirstDayColumn).Width = DayColumnWidth;
        }
    }

    private static void WriteDayHeaders(IXLWorksheet sheet, int headerRow, DateOnly weekStart)
    {
        for (var index = 0; index < WeekGridDefinition.Days.Count; index++)
        {
            var day = WeekGridDefinition.Days[index];
            var date = weekStart.AddDays(index);
            var cell = sheet.Cell(headerRow, index + FirstDayColumn);

            cell.Value = $"{HebrewExcelLabels.DayOf(day)} {date.Day}.{date.Month}";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        sheet.Row(headerRow).Style.Font.Bold = true;
    }

    private static void WriteSlotLabels(IXLWorksheet sheet, int firstSlotRow)
    {
        for (var index = 0; index < Windows.Length; index++)
        {
            var window = Windows[index];
            var start = SlotWindowTimes.StartOf(window);
            var end = SlotWindowTimes.EndOf(window);

            sheet.Cell(index + firstSlotRow, LabelColumn).Value =
                $"{HebrewExcelLabels.WindowOf(window)} {start:HH\\:mm}-{end:HH\\:mm}";
        }
    }

    private static void WriteSlotCells(
        IXLWorksheet sheet,
        int firstSlotRow,
        IReadOnlyCollection<SlotForGetWeekScheduleResponse> slots,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var slotsByCell = slots.ToDictionary(x => (x.Day, x.Window));

        for (var column = 0; column < WeekGridDefinition.Days.Count; column++)
        {
            var day = WeekGridDefinition.Days[column];

            foreach (var window in WeekGridDefinition.WindowsFor(day))
            {
                if (!slotsByCell.TryGetValue((day, window), out var slot))
                {
                    continue;
                }

                var row = Array.IndexOf(Windows, window);
                var cell = sheet.Cell(row + firstSlotRow, column + FirstDayColumn);

                WriteSlotCell(cell, slot, counts);
            }
        }
    }

    private static void WriteSlotCell(
        IXLCell cell,
        SlotForGetWeekScheduleResponse slot,
        IReadOnlyDictionary<Guid, int> counts)
    {
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (slot.State == SlotState.Unavailable)
        {
            cell.Value = BlockedLabel;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            return;
        }

        cell.Value = counts.GetValueOrDefault(slot.Id);
    }
}
