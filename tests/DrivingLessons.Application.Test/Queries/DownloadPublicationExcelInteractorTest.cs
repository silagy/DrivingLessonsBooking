using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class DownloadPublicationExcelInteractorTest
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private IPublicationRepository repository = null!;
    private IExcelGenerator excelGenerator = null!;
    private ICurrentUser currentUser = null!;
    private DownloadPublicationExcelInteractor interactor = null!;
    private Publication publication = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;
    private ExcelFile ownExcel = null!;
    private ExcelFile otherExcel = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IPublicationRepository>();
        excelGenerator = A.Fake<IExcelGenerator>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new DownloadPublicationExcelInteractor(repository, excelGenerator, currentUser);

        publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        ownExcel = new ExcelFile("own.xlsx", [1, 2, 3], ExcelContentType);
        otherExcel = new ExcelFile("other.xlsx", [4, 5, 6], ExcelContentType);

        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).Returns((Publication?)null);
        A.CallTo(() => repository.GetAsync(publication.Id)).Returns(publication);
        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, ownTeacherId)).Returns(ownExcel);
        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, otherTeacherId)).Returns(otherExcel);
    }

    [TestMethod]
    public async Task Teacher_Downloads_Own_Excel()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, ownTeacherId.Value);

        //then
        result.ShouldBe(ownExcel);
    }

    [TestMethod]
    public async Task Other_Teachers_Excel_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).MustNotHaveHappened();
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Downloads_No_Excel()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publication.Id.Value, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).MustNotHaveHappened();
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Downloads_Any_Teachers_Excel()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        result.ShouldBe(otherExcel);
    }

    [TestMethod]
    public async Task Linked_Administrator_Downloads_Another_Teachers_Excel()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        result.ShouldBe(otherExcel);
    }

    [TestMethod]
    public async Task Missing_Publication_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);
        var missingPublicationId = Guid.NewGuid();

        //when
        var act = () => interactor.ExecuteAsync(missingPublicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
