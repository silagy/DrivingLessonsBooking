using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
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
    private const int HeaderRow = 2;
    private const int FirstRequestRow = 3;
    private const int StudentNameColumn = 3;
    private const int TeacherColumnWithTeacher = 3;
    private const int StudentNameColumnWithTeacher = 4;

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
        A.CallTo(() => weekScheduleQueries.FindByWeekAsync(A<DateOnly>._))
            .Returns(Array.Empty<TeacherWeekGrid>());
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._))
            .Returns(new Dictionary<Guid, int>());
        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(A<Guid>._))
            .Returns(Array.Empty<SlotRequestDetail>());
    }

    [TestMethod]
    public async Task Workbook_Is_The_Summary_Only_When_There_Are_No_Requests()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        WorkbookOf(excel).Worksheets.Select(x => x.Name).ShouldBe([SummarySheet.Name]);
    }

    [TestMethod]
    public async Task Workbook_Has_The_Summary_Then_A_Sheet_Per_Car_By_Name()
    {
        //given
        SlotRequestDetail[] requests =
        [
            Request(Guid.NewGuid(), "Zeta", "Smoke Student A"),
            Request(Guid.NewGuid(), "Alpha", "Smoke Student B")
        ];

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns(requests);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        WorkbookOf(excel).Worksheets.Select(x => x.Name).ShouldBe(
            [
                SummarySheet.Name,
                "Alpha",
                "Zeta"
            ]);
    }

    [TestMethod]
    public async Task Each_Car_Sheet_Lists_Only_That_Cars_Requests()
    {
        //given
        var alpha = Guid.NewGuid();
        var zeta = Guid.NewGuid();
        SlotRequestDetail[] requests =
        [
            Request(zeta, "Zeta", "Smoke Student B"),
            Request(alpha, "Alpha", "Smoke Student A"),
            Request(zeta, "Zeta", "Smoke Student C")
        ];

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns(requests);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var workbook = WorkbookOf(excel);
        StudentsOn(workbook.Worksheet("Alpha")).ShouldBe(["Smoke Student A"]);
        StudentsOn(workbook.Worksheet("Zeta")).ShouldBe(["Smoke Student B", "Smoke Student C"]);
    }

    [TestMethod]
    public async Task Two_Cars_With_The_Same_Name_Get_A_Sheet_Each()
    {
        //given
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        SlotRequestDetail[] requests =
        [
            Request(second, "Corolla", "Smoke Student B"),
            Request(first, "Corolla", "Smoke Student A")
        ];

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns(requests);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var workbook = WorkbookOf(excel);
        workbook.Worksheets.Select(x => x.Name).ShouldBe(
            [
                SummarySheet.Name,
                "Corolla",
                "Corolla (2)"
            ]);
        workbook.Worksheet("Corolla").Cell(1, 1).GetText().ShouldBe("Corolla");
        workbook.Worksheet("Corolla (2)").Cell(1, 1).GetText().ShouldBe("Corolla");
        StudentsOn(workbook.Worksheet("Corolla")).ShouldBe(["Smoke Student A"]);
        StudentsOn(workbook.Worksheet("Corolla (2)")).ShouldBe(["Smoke Student B"]);
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
    public async Task Keeps_The_File_Name_And_Content_Type()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        excel.FileName.ShouldBe($"week-2026-10-04-{teacherId.Value}.xlsx");
        excel.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [TestMethod]
    public async Task School__Workbook_Is_The_Summary_Only_When_There_Are_No_Requests()
    {
        //given

        //when
        var excel = await generator.GenerateForSchoolAsync(publication.Id);

        //then
        WorkbookOf(excel).Worksheets.Select(x => x.Name).ShouldBe([SummarySheet.Name]);
    }

    [TestMethod]
    public async Task School__One_Sheet_Per_Car_Holds_The_Requests_Of_Every_Teacher()
    {
        //given
        var shared = Guid.NewGuid();
        var alpha = Guid.NewGuid();
        SlotRequestDetail[] requests =
        [
            Request(shared, "Corolla", "Smoke Student B", "Smoke Teacher B"),
            Request(alpha, "Alpha", "Smoke Student C", "Smoke Teacher B"),
            Request(shared, "Corolla", "Smoke Student A", "Smoke Teacher A")
        ];

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value))
            .Returns(requests);

        //when
        var excel = await generator.GenerateForSchoolAsync(publication.Id);

        //then
        var workbook = WorkbookOf(excel);
        workbook.Worksheets.Select(x => x.Name).ShouldBe(
            [
                SummarySheet.Name,
                "Alpha",
                "Corolla"
            ]);
        var corolla = workbook.Worksheet("Corolla");
        corolla.Cell(HeaderRow, TeacherColumnWithTeacher).GetText().ShouldBe("מורה");
        corolla.Cell(FirstRequestRow, TeacherColumnWithTeacher).GetText().ShouldBe("Smoke Teacher A");
        corolla.Cell(FirstRequestRow, StudentNameColumnWithTeacher).GetText().ShouldBe("Smoke Student A");
        corolla.Cell(FirstRequestRow + 1, TeacherColumnWithTeacher).GetText().ShouldBe("Smoke Teacher B");
        corolla.Cell(FirstRequestRow + 1, StudentNameColumnWithTeacher).GetText().ShouldBe("Smoke Student B");
    }

    [TestMethod]
    public async Task School__Summary_Stacks_Every_Teachers_Grid_By_Teacher_Name()
    {
        //given
        var bSlot = Slot();
        var aSlot = Slot();
        TeacherWeekGrid[] grids =
        [
            new(Guid.NewGuid(), "Smoke Teacher B", [bSlot]),
            new(Guid.NewGuid(), "Smoke Teacher A", [aSlot])
        ];
        var counts = new Dictionary<Guid, int>
        {
            [aSlot.Id] = 1,
            [bSlot.Id] = 3
        };

        A.CallTo(() => weekScheduleQueries.FindByWeekAsync(publication.WeekStart.Value))
            .Returns(grids);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(publication.Id.Value))
            .Returns(counts);

        //when
        var excel = await generator.GenerateForSchoolAsync(publication.Id);

        //then
        var summary = WorkbookOf(excel).Worksheet(SummarySheet.Name);
        summary.Cell(1, 1).GetText().ShouldBe("Smoke Teacher A");
        summary.Cell(3, 2).GetValue<int>().ShouldBe(1);
        summary.Cell(8, 1).GetText().ShouldBe("Smoke Teacher B");
        summary.Cell(10, 2).GetValue<int>().ShouldBe(3);
    }

    [TestMethod]
    public async Task School__Names_The_File_After_The_Week()
    {
        //given

        //when
        var excel = await generator.GenerateForSchoolAsync(publication.Id);

        //then
        excel.FileName.ShouldBe("week-2026-10-04-school.xlsx");
        excel.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [TestMethod]
    public async Task School__Publication_Must_Exist()
    {
        //given
        var missingId = PublicationId.New();

        A.CallTo(() => publicationRepository.GetAsync(missingId))
            .Returns((Publication?)null);

        //when
        var act = () => generator.GenerateForSchoolAsync(missingId);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
    }

    private static SlotForGetWeekScheduleResponse Slot()
    {
        return new SlotForGetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            Day = DayOfWeek.Sunday,
            Window = SlotWindowType.Morning,
            State = SlotState.Open
        };
    }

    private static SlotRequestDetail Request(Guid carId, string carName, string studentName)
    {
        return Request(carId, carName, studentName, "Smoke Teacher");
    }

    private static SlotRequestDetail Request(Guid carId, string carName, string studentName, string teacherName)
    {
        return new SlotRequestDetail(
            Guid.NewGuid(),
            DayOfWeek.Sunday,
            SlotWindowType.Morning,
            studentName,
            teacherName,
            carId,
            carName,
            SessionType.Single,
            1,
            1,
            null);
    }

    private static string[] StudentsOn(IXLWorksheet sheet)
    {
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        return Enumerable
                   .Range(FirstRequestRow, lastRow - FirstRequestRow + 1)
                   .Select(row => sheet.Cell(row, StudentNameColumn).GetText())
                   .ToArray();
    }

    private static XLWorkbook WorkbookOf(ExcelFile excel)
    {
        var stream = new MemoryStream(excel.Content);

        return new XLWorkbook(stream);
    }
}
