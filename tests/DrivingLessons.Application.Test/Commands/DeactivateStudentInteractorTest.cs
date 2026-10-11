using DrivingLessons.Application.Commands.DeactivateStudent;
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
public class DeactivateStudentInteractorTest
{
    private IStudentRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeactivateStudentInteractor interactor = null!;
    private Student itai = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeactivateStudentInteractor(repository, unitOfWork);
        itai = ActiveStudent();

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(itai.Id)).Returns(itai);
    }

    [TestMethod]
    public async Task Deactivates_An_Active_Student()
    {
        //when
        await interactor.ExecuteAsync(itai.Id.Value);

        //then
        itai.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Inactive_Student_Is_Rejected()
    {
        //given
        itai.Deactivate();

        //when
        var act = () => interactor.ExecuteAsync(itai.Id.Value);

        //then
        await Should.ThrowAsync<StudentAlreadyDeactivatedException>(act);
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

    private static Student ActiveStudent()
    {
        var teacher = Teacher.Create(TeacherName.Of("יעל כרמי"), Email.Of("yael@school.example"));
        var car = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        car.AssignTeacher(teacher);

        return Student.Create(
            NationalId.Of("207815432"),
            StudentName.Of("איתי פרץ"),
            PhoneNumber.Of("050-6612034"),
            teacher,
            car,
            null,
            null,
            null);
    }
}
