using ClosedXML.Excel;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public static class SummarySheet
{
    public const string Name = "סיכום";

    private const string BlockedLabel = "לא זמין";
    private const int HeaderRow = 1;
    private const int LabelColumn = 1;
    private const int FirstSlotRow = 2;
    private const int FirstDayColumn = 2;
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
        var sheet = workbook.Worksheets.Add(Name);
        sheet.RightToLeft = true;

        FormatColumns(sheet);
        WriteDayHeaders(sheet, weekStart);
        WriteSlotLabels(sheet);
        WriteSlotCells(sheet, slots, counts);
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

    private static void WriteDayHeaders(IXLWorksheet sheet, DateOnly weekStart)
    {
        for (var index = 0; index < WeekGridDefinition.Days.Count; index++)
        {
            var day = WeekGridDefinition.Days[index];
            var date = weekStart.AddDays(index);
            var cell = sheet.Cell(HeaderRow, index + FirstDayColumn);

            cell.Value = $"{HebrewExcelLabels.DayOf(day)} {date.Day}.{date.Month}";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
    }

    private static void WriteSlotLabels(IXLWorksheet sheet)
    {
        for (var index = 0; index < Windows.Length; index++)
        {
            var window = Windows[index];
            var start = SlotWindowTimes.StartOf(window);
            var end = SlotWindowTimes.EndOf(window);

            sheet.Cell(index + FirstSlotRow, LabelColumn).Value =
                $"{HebrewExcelLabels.WindowOf(window)} {start:HH\\:mm}–{end:HH\\:mm}";
        }
    }

    private static void WriteSlotCells(
        IXLWorksheet sheet,
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
                var cell = sheet.Cell(row + FirstSlotRow, column + FirstDayColumn);

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
