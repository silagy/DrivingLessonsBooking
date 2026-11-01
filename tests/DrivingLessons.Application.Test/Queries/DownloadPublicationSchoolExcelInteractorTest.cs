using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries.DownloadPublicationSchoolExcel;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class DownloadPublicationSchoolExcelInteractorTest
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private IPublicationRepository repository = null!;
    private IExcelGenerator excelGenerator = null!;
    private DownloadPublicationSchoolExcelInteractor interactor = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IPublicationRepository>();
        excelGenerator = A.Fake<IExcelGenerator>();
        interactor = new DownloadPublicationSchoolExcelInteractor(repository, excelGenerator);
    }

    [TestMethod]
    public async Task Downloads_The_School_Excel_Of_The_Publication()
    {
        //given
        var publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        var excel = new ExcelFile("week-2026-10-04-school.xlsx", [1, 2, 3], ExcelContentType);

        A.CallTo(() => repository.GetAsync(publication.Id)).Returns(publication);
        A.CallTo(() => excelGenerator.GenerateForSchoolAsync(publication.Id)).Returns(excel);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value);

        //then
        result.ShouldBe(excel);
    }

    [TestMethod]
    public async Task Publication_Must_Exist()
    {
        //given
        var id = Guid.NewGuid();
        var publicationId = PublicationId.Of(id);

        A.CallTo(() => repository.GetAsync(publicationId)).Returns((Publication?)null);

        //when
        var act = () => interactor.ExecuteAsync(id);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => excelGenerator.GenerateForSchoolAsync(A<PublicationId>._)).MustNotHaveHappened();
    }
}
