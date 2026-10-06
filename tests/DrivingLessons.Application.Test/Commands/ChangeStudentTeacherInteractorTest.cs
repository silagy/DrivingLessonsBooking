using DrivingLessons.Application.Commands.ChangeStudentTeacher;
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
public class ChangeStudentTeacherInteractorTest
{
    private IStudentRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeStudentTeacherInteractor interactor = null!;
    private Teacher ronit = null!;
    private Teacher yael = null!;
    private Car corolla = null!;
    private Car i20 = null!;
    private Student noa = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeStudentTeacherInteractor(repository, teacherRepository, carRepository, unitOfWork);

        ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        yael = Teacher.Create(TeacherName.Of("יעל כרמי"), Email.Of("yael@school.example"));
        corolla = Car.Create(CarName.Of("קורולה לבנה"), CarType.Of("קורולה"), Transmission.Automatic);
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        corolla.AssignTeacher(ronit);
        i20.AssignTeacher(ronit);
        i20.AssignTeacher(yael);
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
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(ronit.Id)).Returns(ronit);
        A.CallTo(() => teacherRepository.GetAsync(yael.Id)).Returns(yael);
        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(corolla.Id)).Returns(corolla);
        A.CallTo(() => carRepository.GetAsync(i20.Id)).Returns(i20);
    }

    [TestMethod]
    public async Task Changes_The_Teacher_And_Car()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.TeacherId.ShouldBe(yael.Id);
        noa.CarId.ShouldBe(i20.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Current_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentTeacherRequest(ronit.Id.Value, i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentAlreadyWithTeacherException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Of_Another_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, corolla.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        noa.TeacherId.ShouldBe(ronit.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(Guid.NewGuid(), i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, Guid.NewGuid());

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
