using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public class ExcelGenerator : IExcelGenerator
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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

        SummarySheet.AddTo(workbook, weekStart, schedule?.Slots ?? [], counts);
        AddCarSheets(workbook, requests, RequestDetailSheet.AddTo);

        var fileName = $"week-{weekStart:yyyy-MM-dd}-{teacherGuid}.xlsx";

        return FileOf(workbook, fileName);
    }

    public async Task<ExcelFile> GenerateForSchoolAsync(PublicationId publicationId)
    {
        var publication = await publicationRepository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var weekStart = publication.WeekStart.Value;
        var publicationGuid = publicationId.Value;

        var schedules = await weekScheduleQueries.FindByWeekAsync(weekStart);
        var counts = await submissionQueries.GetSlotRequestCountsAsync(publicationGuid);
        var requests = await submissionQueries.GetSlotRequestDetailsAsync(publicationGuid);

        var grids = schedules
                        .OrderBy(x => x.TeacherName, StringComparer.Ordinal)
                        .ThenBy(x => x.TeacherId)
                        .ToList();

        using var workbook = new XLWorkbook();

        SummarySheet.AddForTeachersTo(workbook, weekStart, grids, counts);
        AddCarSheets(workbook, requests, RequestDetailSheet.AddWithTeacherTo);

        var fileName = $"week-{weekStart:yyyy-MM-dd}-school.xlsx";

        return FileOf(workbook, fileName);
    }

    private static ExcelFile FileOf(XLWorkbook workbook, string fileName)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return new ExcelFile(fileName, content, ContentType);
    }

    private static void AddCarSheets(
        XLWorkbook workbook,
        IReadOnlyCollection<SlotRequestDetail> requests,
        Action<XLWorkbook, string, string, IReadOnlyCollection<SlotRequestDetail>> addCarSheet)
    {
        var cars = requests
                       .GroupBy(x => x.CarId)
                       .Select(x => x.ToList())
                       .OrderBy(x => x[0].CarName, StringComparer.Ordinal)
                       .ThenBy(x => x[0].CarId)
                       .ToList();

        var carNames = cars
                           .Select(x => x[0].CarName)
                           .ToList();

        var sheetNames = CarSheetNames.For(carNames);

        for (var index = 0; index < cars.Count; index++)
        {
            addCarSheet(workbook, sheetNames[index], carNames[index], cars[index]);
        }
    }
}
