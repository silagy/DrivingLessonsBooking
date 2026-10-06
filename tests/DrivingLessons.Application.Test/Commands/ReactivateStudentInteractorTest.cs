using DrivingLessons.Application.Commands.ReactivateStudent;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ReactivateStudentInteractorTest
{
    private IStudentRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ReactivateStudentInteractor interactor = null!;
    private Student dana = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ReactivateStudentInteractor(repository, unitOfWork);
        dana = InactiveStudent();

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(dana.Id)).Returns(dana);
    }

    [TestMethod]
    public async Task Reactivates_An_Inactive_Student()
    {
        //when
        await interactor.ExecuteAsync(dana.Id.Value);

        //then
        dana.IsActive.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Active_Student_Is_Rejected()
    {
        //given
        dana.Reactivate();

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentAlreadyActiveException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student InactiveStudent()
    {
        var teacher = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        var car = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        car.AssignTeacher(teacher);

        var student = Student.Create(
            NationalId.Of("311078547"),
            StudentName.Of("דנה ששון"),
            PhoneNumber.Of("052-9038816"),
            teacher,
            car,
            null,
            null,
            null);
        student.Deactivate();

        return student;
    }
}
