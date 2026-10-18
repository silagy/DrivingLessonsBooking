using ClosedXML.Excel;
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Infrastructure.Excel;

public static class RequestDetailSheet
{
    public const string Name = "פירוט בקשות";

    private const int HeaderRow = 1;
    private const int FirstRequestRow = 2;
    private const int FirstColumn = 1;
    private const int ConstraintsColumn = 8;

    private static readonly string[] Headers =
    [
        "יום",
        "משבצת",
        "שם התלמיד/ה",
        "רכב",
        "סוג שיעור",
        "דירוג",
        "שיעורים מבוקשים",
        "אילוצים"
    ];

    private static readonly double[] ColumnWidths =
    [
        8,
        10,
        24,
        18,
        10,
        7,
        14,
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
                          .ThenBy(x => x.CarName, StringComparer.Ordinal);

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
            request.CarName,
            HebrewExcelLabels.SessionTypeOf(request.SessionType),
            request.Rank,
            HebrewExcelLabels.RequestedLessonsOf(request.TargetCount, request.PickCount),
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
