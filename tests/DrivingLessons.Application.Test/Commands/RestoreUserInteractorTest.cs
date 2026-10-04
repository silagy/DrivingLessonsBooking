using DrivingLessons.Application.Commands.RestoreUser;
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
public class RestoreUserInteractorTest
{
    private IUserRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private RestoreUserInteractor interactor = null!;
    private Teacher teacher = null!;
    private User administrator = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new RestoreUserInteractor(repository, teacherRepository, unitOfWork);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        administrator = UserOf("ronit@school.example", Role.Administrator, null);
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, teacher);
        administrator.Delete();
        teacherUser.Delete();

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(administrator.Id)).Returns(administrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
    }

    [TestMethod]
    public async Task Restores_A_Deleted_User_Without_A_Linked_Teacher()
    {
        //when
        await interactor.ExecuteAsync(administrator.Id.Value);

        //then
        administrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Restores_A_Deleted_User_Whose_Linked_Teacher_Is_Active()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task User_Whose_Linked_Teacher_Is_Deleted_Is_Not_Restored()
    {
        //given
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns((Teacher?)null);

        //when
        var act = () => interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        await Should.ThrowAsync<UserLinkedTeacherMustNotBeDeletedException>(act);
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Active_User_Is_Rejected_As_Already_Active()
    {
        //given
        administrator.Restore();

        //when
        var act = () => interactor.ExecuteAsync(administrator.Id.Value);

        //then
        await Should.ThrowAsync<UserAlreadyActiveException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static User UserOf(string signInEmail, Role role, Teacher? linkedTeacher)
    {
        return User.Create(
            UserName.Of("Test User"),
            Email.Of(signInEmail),
            PasswordHash.Of("hash"),
            role,
            linkedTeacher);
    }
}
