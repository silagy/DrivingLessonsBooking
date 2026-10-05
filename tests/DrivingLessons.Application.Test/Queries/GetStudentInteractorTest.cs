using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetStudent;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetStudentInteractorTest
{
    private IStudentQueries queries = null!;
    private GetStudentInteractor interactor = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IStudentQueries>();
        interactor = new GetStudentInteractor(queries);
    }

    [TestMethod]
    public async Task Returns_The_Student()
    {
        //given
        var id = Guid.NewGuid();
        var student = new GetStudentResponse
        {
            Id = id,
            NationalId = "123456782",
            Name = "Shaked Navon",
            Phone = "050-3318842",
            TeacherId = Guid.NewGuid(),
            TeacherName = "Yael Carmi",
            CarId = Guid.NewGuid(),
            CarName = "Picanto Red",
            CarTransmission = Transmission.Automatic,
            Address = "12 HaRimon St, Modiin",
            StartDate = new DateOnly(2026, 9, 1),
            LicenseType = "B",
            IsActive = true
        };
        A.CallTo(() => queries.GetAsync(id)).Returns(student);

        //when
        var result = await interactor.ExecuteAsync(id);

        //then
        result.ShouldBe(student);
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var id = Guid.NewGuid();
        A.CallTo(() => queries.GetAsync(id)).Returns((GetStudentResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(id);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }
}
