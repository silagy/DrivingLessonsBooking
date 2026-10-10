using DrivingLessons.Application.Commands.DeleteCar;
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
public class DeleteCarInteractorTest
{
    private ICarRepository repository = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteCarInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<ICarRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteCarInteractor(repository, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

        A.CallTo(() => repository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => repository.GetAsync(car.Id)).Returns(car);
        A.CallTo(() => studentRepository.FindByCarAsync(A<CarId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Deletes_A_Car_Without_Students()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value);

        //then
        car.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Car_With_An_Active_Student_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value);

        //then
        await Should.ThrowAsync<CarMustNotHaveActiveStudentsException>(act);
        car.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deletes_A_Car_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: false)]);

        //when
        await interactor.ExecuteAsync(car.Id.Value);

        //then
        car.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private Student StudentOn(bool isActive)
    {
        var student = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("Noa Mizrahi"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);

        if (!isActive)
        {
            student.Deactivate();
        }

        return student;
    }
}
