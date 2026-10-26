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
        "שיעור אפשרי",
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
        12,
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
        var numbered = requests
                           .GroupBy(x => x.SubmissionId)
                           .SelectMany(NumberedFromSundayMorning);

        var ordered = numbered
                          .OrderBy(x => x.Request.Day)
                          .ThenBy(x => x.Request.Window)
                          .ThenBy(x => x.Request.StudentName, StringComparer.Ordinal)
                          .ThenBy(x => x.Request.CarName, StringComparer.Ordinal)
                          .ThenBy(x => x.Request.SubmissionId);

        var row = FirstRequestRow;

        foreach (var (request, position) in ordered)
        {
            WriteRequest(sheet.Row(row), request, position);
            row++;
        }
    }

    private static IEnumerable<(SlotRequestDetail Request, int Position)> NumberedFromSundayMorning(
        IEnumerable<SlotRequestDetail> submissionRequests)
    {
        return submissionRequests
                   .OrderBy(x => x.Day)
                   .ThenBy(x => x.Window)
                   .Select((request, index) => (request, index + 1));
    }

    private static void WriteRequest(IXLRow row, SlotRequestDetail request, int position)
    {
        var values = new XLCellValue[]
        {
            HebrewExcelLabels.DayOf(request.Day),
            HebrewExcelLabels.WindowOf(request.Window),
            request.StudentName,
            request.CarName,
            HebrewExcelLabels.SessionTypeOf(request.SessionType),
            HebrewExcelLabels.PickPositionOf(position, request.PickCount),
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
