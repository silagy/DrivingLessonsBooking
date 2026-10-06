using DrivingLessons.Application.Commands.ChangeStudentCar;
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
public class ChangeStudentCarInteractorTest
{
    private IStudentRepository repository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeStudentCarInteractor interactor = null!;
    private Car corolla = null!;
    private Car i20 = null!;
    private Car mazda = null!;
    private Student noa = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeStudentCarInteractor(repository, carRepository, unitOfWork);

        var ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        var oren = Teacher.Create(TeacherName.Of("אורן לוי"), Email.Of("oren@school.example"));
        corolla = Car.Create(CarName.Of("קורולה לבנה"), CarType.Of("קורולה"), Transmission.Automatic);
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        mazda = Car.Create(CarName.Of("מאזדה 3 אפורה"), CarType.Of("מאזדה 3"), Transmission.Manual);
        corolla.AssignTeacher(ronit);
        i20.AssignTeacher(ronit);
        mazda.AssignTeacher(oren);
        noa = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("נועה מזרחי"),
            PhoneNumber.Of("050-1234567"),
            ronit,
            corolla,
            null,
            null,
            null);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(noa.Id)).Returns(noa);
        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(corolla.Id)).Returns(corolla);
        A.CallTo(() => carRepository.GetAsync(i20.Id)).Returns(i20);
        A.CallTo(() => carRepository.GetAsync(mazda.Id)).Returns(mazda);
    }

    [TestMethod]
    public async Task Changes_The_Car()
    {
        //given
        var request = new ChangeStudentCarRequest(i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.CarId.ShouldBe(i20.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Current_Car_Is_Rejected()
    {
        //given
        var request = new ChangeStudentCarRequest(corolla.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentAlreadyOnCarException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Of_Another_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentCarRequest(mazda.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        noa.CarId.ShouldBe(corolla.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentCarRequest(i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentCarRequest(Guid.NewGuid());

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
