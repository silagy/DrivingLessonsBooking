using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class ExcelTypographyTest
{
    private const char EnDash = (char)0x2013;
    private const char EmDash = (char)0x2014;
    private const char Ellipsis = (char)0x2026;
    private static readonly char[] TypographicPunctuation = [EnDash, EmDash, Ellipsis];

    [TestMethod]
    public async Task Workbook_Uses_A_Plain_Hyphen_And_Three_Dots_Only()
    {
        //given
        var publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        var teacherId = TeacherId.New();
        var generator = GeneratorFor(publication, teacherId);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        TextsWithTypographicPunctuation(excel).ShouldBeEmpty();
    }

    private static ExcelGenerator GeneratorFor(Publication publication, TeacherId teacherId)
    {
        var publicationRepository = A.Fake<IPublicationRepository>();
        var weekScheduleQueries = A.Fake<IWeekScheduleQueries>();
        var submissionQueries = A.Fake<ISubmissionQueries>();
        var slots = FullGrid();
        var schedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId.Value,
            WeekStart = publication.WeekStart.Value,
            Slots = slots
        };
        var counts = slots.ToDictionary(slot => slot.Id, _ => 1);
        var request = new SlotRequestDetail(
            DayOfWeek.Sunday,
            SlotWindowType.Afternoon,
            "Smoke Student A",
            "000000018",
            "050-0000001",
            Transmission.Manual,
            SessionType.Double,
            1,
            2,
            "only after 16:00");

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);
        A.CallTo(() => weekScheduleQueries.GetByTeacherAndWeekAsync(teacherId.Value, publication.WeekStart.Value))
            .Returns(schedule);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(publication.Id.Value, teacherId.Value))
            .Returns(counts);
        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns([request]);

        return new ExcelGenerator(publicationRepository, weekScheduleQueries, submissionQueries);
    }

    private static List<SlotForGetWeekScheduleResponse> FullGrid()
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
                                              State = SlotState.Open
                                          }))
                   .ToList();
    }

    private static List<string> TextsWithTypographicPunctuation(ExcelFile excel)
    {
        var stream = new MemoryStream(excel.Content);
        var workbook = new XLWorkbook(stream);
        var texts = workbook
                        .Worksheets
                        .SelectMany(sheet => sheet
                                                 .CellsUsed()
                                                 .Select(cell => $"{sheet.Name}!{cell.Address}: {cell.GetFormattedString()}")
                                                 .Prepend(sheet.Name));

        return texts
                   .Where(text => text.IndexOfAny(TypographicPunctuation) >= 0)
                   .ToList();
    }
}
