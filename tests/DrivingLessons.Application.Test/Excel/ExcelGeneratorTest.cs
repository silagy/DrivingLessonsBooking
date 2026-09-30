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
public class ExcelGeneratorTest
{
    private IPublicationRepository publicationRepository = null!;
    private IWeekScheduleQueries weekScheduleQueries = null!;
    private ISubmissionQueries submissionQueries = null!;
    private ExcelGenerator generator = null!;
    private Publication publication = null!;
    private TeacherId teacherId = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        weekScheduleQueries = A.Fake<IWeekScheduleQueries>();
        submissionQueries = A.Fake<ISubmissionQueries>();
        generator = new ExcelGenerator(publicationRepository, weekScheduleQueries, submissionQueries);
        publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        teacherId = TeacherId.New();

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);
        A.CallTo(() => weekScheduleQueries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._))
            .Returns((GetWeekScheduleResponse?)null);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._))
            .Returns(new Dictionary<Guid, int>());
        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(A<Guid>._, A<Guid>._))
            .Returns(Array.Empty<SlotRequestDetail>());
    }

    [TestMethod]
    public async Task Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication()
    {
        //given
        var request = new SlotRequestDetail(
            DayOfWeek.Sunday,
            SlotWindowType.Morning,
            "Smoke Student A",
            "000000018",
            "050-0000001",
            Transmission.Automatic,
            SessionType.Single,
            1,
            2,
            null);

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns([request]);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var detail = WorkbookOf(excel).Worksheet(RequestDetailSheet.Name);
        detail.LastRowUsed()!.RowNumber().ShouldBe(2);
        detail.Cell(2, 3).GetText().ShouldBe("Smoke Student A");
        detail.Cell(2, 4).GetText().ShouldBe("000000018");
    }

    [TestMethod]
    public async Task Summary_Sheet_Counts_The_Requests_Of_The_Teacher_And_Publication()
    {
        //given
        var slot = new SlotForGetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            Day = DayOfWeek.Sunday,
            Window = SlotWindowType.Morning,
            State = SlotState.Open
        };
        var schedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId.Value,
            WeekStart = publication.WeekStart.Value,
            Slots = [slot]
        };
        var counts = new Dictionary<Guid, int>
        {
            [slot.Id] = 2
        };

        A.CallTo(() => weekScheduleQueries.GetByTeacherAndWeekAsync(teacherId.Value, publication.WeekStart.Value))
            .Returns(schedule);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(publication.Id.Value, teacherId.Value))
            .Returns(counts);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var summary = WorkbookOf(excel).Worksheet(SummarySheet.Name);
        summary.Cell(1, 2).GetText().ShouldBe("ראשון 4.10");
        summary.Cell(2, 2).GetValue<int>().ShouldBe(2);
    }

    [TestMethod]
    public async Task Summary_Grid_Is_Empty_When_The_Teacher_Has_No_Week_Schedule()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var summary = WorkbookOf(excel).Worksheet(SummarySheet.Name);
        summary.Range(2, 2, 5, 7).IsEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public async Task Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        WorkbookOf(excel).Worksheets.Select(x => x.Name).ShouldBe(
            [
                SummarySheet.Name,
                RequestDetailSheet.Name
            ]);
    }

    [TestMethod]
    public async Task Keeps_The_File_Name_And_Content_Type()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        excel.FileName.ShouldBe($"week-2026-10-04-{teacherId.Value}.xlsx");
        excel.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static XLWorkbook WorkbookOf(ExcelFile excel)
    {
        var stream = new MemoryStream(excel.Content);

        return new XLWorkbook(stream);
    }
}
