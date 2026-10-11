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
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ITeacherAndCarLocks locks = null!;
    private ReactivateStudentInteractor interactor = null!;
    private Teacher ronit = null!;
    private Car i20 = null!;
    private Student dana = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        locks = A.Fake<ITeacherAndCarLocks>();
        interactor = new ReactivateStudentInteractor(repository, teacherRepository, carRepository, locks, unitOfWork);

        ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        i20.AssignTeacher(ronit);
        dana = InactiveStudent(ronit, i20);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(dana.Id)).Returns(dana);
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).Returns(ronit);
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).Returns(i20);
    }

    [TestMethod]
    public async Task Locks_The_Students_Teacher_And_Car_Before_Reading_Them()
    {
        //when
        await interactor.ExecuteAsync(dana.Id.Value);

        //then
        A.CallTo(() => repository.GetAsync(dana.Id)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => locks.LockAsync(
                    A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { ronit.Id }),
                    A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { i20.Id })))
                .MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).MustHaveHappened());
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
        dana.Reactivate(ronit, i20);

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

    [TestMethod]
    public async Task Student_With_A_Deleted_Teacher_Is_Rejected()
    {
        //given
        ronit.Delete([dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentTeacherMustNotBeDeletedException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_With_A_Deleted_Car_Is_Rejected()
    {
        //given
        i20.Delete([dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentCarMustNotBeDeletedException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_Whose_Car_Is_No_Longer_Their_Teachers_Is_Rejected()
    {
        //given
        i20.UnassignTeacher(ronit, [dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //given
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).Returns((Teacher?)null);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).Returns((Car?)null);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student InactiveStudent(Teacher teacher, Car car)
    {
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
