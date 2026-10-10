using DrivingLessons.Application.Commands.UnassignCarFromTeacher;
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
public class UnassignCarFromTeacherInteractorTest
{
    private ICarRepository carRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private UnassignCarFromTeacherInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;

    [TestInitialize]
    public void Init()
    {
        carRepository = A.Fake<ICarRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new UnassignCarFromTeacherInteractor(carRepository, teacherRepository, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(car.Id)).Returns(car);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => studentRepository.FindByCarAsync(A<CarId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Unassigns_A_Teacher_Without_Students()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        car.IsAssignedTo(teacher).ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_Student_On_The_Car_Is_Not_Unassigned()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherAssignmentMustNotHaveActiveStudentsException>(act);
        car.IsAssignedTo(teacher).ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unassigns_A_Teacher_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: false)]);

        //when
        await interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        car.IsAssignedTo(teacher).ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), teacher.Id.Value);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value, Guid.NewGuid());

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
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
