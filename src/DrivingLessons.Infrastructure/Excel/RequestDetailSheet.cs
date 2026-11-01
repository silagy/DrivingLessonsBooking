using ClosedXML.Excel;
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Infrastructure.Excel;

public static class RequestDetailSheet
{
    private sealed record Column(string Header, double Width, Func<SlotRequestDetail, int, XLCellValue> Value);

    private const int TitleRow = 1;
    private const int HeaderRow = 2;
    private const int FirstRequestRow = 3;
    private const int FirstColumn = 1;

    private static readonly Column DayColumn = new(
        "יום",
        8,
        (request, _) => HebrewExcelLabels.DayOf(request.Day));

    private static readonly Column SlotColumn = new(
        "משבצת",
        10,
        (request, _) => HebrewExcelLabels.WindowOf(request.Window));

    private static readonly Column TeacherColumn = new(
        "מורה",
        16,
        (request, _) => request.TeacherName);

    private static readonly Column StudentColumn = new(
        "שם התלמיד/ה",
        24,
        (request, _) => request.StudentName);

    private static readonly Column SessionTypeColumn = new(
        "סוג שיעור",
        10,
        (request, _) => HebrewExcelLabels.SessionTypeOf(request.SessionType));

    private static readonly Column PositionColumn = new(
        "שיעור אפשרי",
        12,
        (request, position) => HebrewExcelLabels.PickPositionOf(position, request.PickCount));

    private static readonly Column TargetCountColumn = new(
        "שיעורים מבוקשים",
        14,
        (request, _) => request.TargetCount);

    private static readonly Column ConstraintsColumn = new(
        "אילוצים",
        40,
        (request, _) => ConstraintOf(request.Constraint));

    private static readonly Column[] PerTeacherColumns =
    [
        DayColumn,
        SlotColumn,
        StudentColumn,
        SessionTypeColumn,
        PositionColumn,
        TargetCountColumn,
        ConstraintsColumn
    ];

    private static readonly Column[] AllTeachersColumns =
    [
        DayColumn,
        SlotColumn,
        TeacherColumn,
        StudentColumn,
        SessionTypeColumn,
        PositionColumn,
        TargetCountColumn,
        ConstraintsColumn
    ];

    public static void AddTo(
        XLWorkbook workbook,
        string sheetName,
        string carName,
        IReadOnlyCollection<SlotRequestDetail> requests)
    {
        AddTo(workbook, sheetName, carName, requests, PerTeacherColumns);
    }

    public static void AddWithTeacherTo(
        XLWorkbook workbook,
        string sheetName,
        string carName,
        IReadOnlyCollection<SlotRequestDetail> requests)
    {
        AddTo(workbook, sheetName, carName, requests, AllTeachersColumns);
    }

    private static void AddTo(
        XLWorkbook workbook,
        string sheetName,
        string carName,
        IReadOnlyCollection<SlotRequestDetail> requests,
        IReadOnlyList<Column> columns)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        sheet.RightToLeft = true;

        FormatColumns(sheet, columns);
        WriteTitle(sheet, carName, columns.Count);
        WriteHeader(sheet, columns);
        WriteRequests(sheet, requests, columns);

        var lastRow = HeaderRow + requests.Count;
        sheet.Range(HeaderRow, FirstColumn, lastRow, columns.Count).SetAutoFilter();
        sheet.SheetView.FreezeRows(HeaderRow);
    }

    private static void FormatColumns(IXLWorksheet sheet, IReadOnlyList<Column> columns)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            sheet.Column(index + FirstColumn).Width = columns[index].Width;
        }

        var constraintsColumn = columns.Count - 1 + FirstColumn;
        sheet.Column(constraintsColumn).Style.Alignment.WrapText = true;
    }

    private static void WriteTitle(IXLWorksheet sheet, string carName, int columnCount)
    {
        var title = sheet.Cell(TitleRow, FirstColumn);
        title.Value = carName;
        title.Style.Font.Bold = true;

        sheet.Range(TitleRow, FirstColumn, TitleRow, columnCount).Merge();
    }

    private static void WriteHeader(IXLWorksheet sheet, IReadOnlyList<Column> columns)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            sheet.Cell(HeaderRow, index + FirstColumn).Value = columns[index].Header;
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
    }

    private static void WriteRequests(
        IXLWorksheet sheet,
        IReadOnlyCollection<SlotRequestDetail> requests,
        IReadOnlyList<Column> columns)
    {
        var numbered = requests
                           .GroupBy(x => x.SubmissionId)
                           .SelectMany(NumberedFromSundayMorning);

        var ordered = numbered
                          .OrderBy(x => x.Request.Day)
                          .ThenBy(x => x.Request.Window)
                          .ThenBy(x => x.Request.TeacherName, StringComparer.Ordinal)
                          .ThenBy(x => x.Request.StudentName, StringComparer.Ordinal)
                          .ThenBy(x => x.Request.SubmissionId);

        var row = FirstRequestRow;

        foreach (var (request, position) in ordered)
        {
            WriteRequest(sheet.Row(row), request, position, columns);
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

    private static void WriteRequest(
        IXLRow row,
        SlotRequestDetail request,
        int position,
        IReadOnlyList<Column> columns)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            row.Cell(index + FirstColumn).Value = columns[index].Value(request, position);
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
