using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public class ExcelGenerator : IExcelGenerator
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

    public ExcelGenerator(
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
        var requests = await submissionQueries.GetSlotRequestDetailsAsync(publicationId.Value, teacherGuid);

        using var workbook = new XLWorkbook();

        BuildSummarySheet(workbook, schedule, counts);
        RequestDetailSheet.AddTo(workbook, requests);

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
}
