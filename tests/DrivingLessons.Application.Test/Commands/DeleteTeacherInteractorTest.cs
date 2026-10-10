using DrivingLessons.Application.Commands.DeleteTeacher;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class DeleteTeacherInteractorTest
{
    private ITeacherRepository repository = null!;
    private IUserQueries userQueries = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteTeacherInteractor interactor = null!;
    private Teacher teacher = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<ITeacherRepository>();
        userQueries = A.Fake<IUserQueries>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteTeacherInteractor(repository, userQueries, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));

        A.CallTo(() => repository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => repository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(A<TeacherId>._)).Returns(false);
        A.CallTo(() => studentRepository.FindByTeacherAsync(A<TeacherId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Without_A_User()
    {
        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Whose_Linked_User_Is_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(false);

        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_User_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveUserException>(act);
        teacher.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_User_Is_Refused_Before_Students_Are_Loaded()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveUserException>(act);
        A.CallTo(() => studentRepository.FindByTeacherAsync(A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_Student_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveStudentsException>(act);
        teacher.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: false)]);

        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student StudentOf(Teacher teacher, bool isActive)
    {
        var car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

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
