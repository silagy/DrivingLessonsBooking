using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.DeleteUser;
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
public class DeleteUserInteractorTest
{
    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteUserInteractor interactor = null!;
    private User signedInAdministrator = null!;
    private User otherAdministrator = null!;
    private Teacher teacher = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteUserInteractor(repository, queries, currentUser, unitOfWork);

        signedInAdministrator = UserOf("owner@school.example", Role.Administrator, null);
        otherAdministrator = UserOf("ronit@school.example", Role.Administrator, null);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, teacher);

        A.CallTo(() => currentUser.Id).Returns(signedInAdministrator.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(signedInAdministrator.Id)).Returns(signedInAdministrator);
        A.CallTo(() => repository.GetAsync(otherAdministrator.Id)).Returns(otherAdministrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(2);
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Role_User()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deletes_An_Administrator_While_Another_Active_Administrator_Remains()
    {
        //when
        await interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        otherAdministrator.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deleting_A_Teacher_Role_User_Does_Not_Count_Administrators()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);

        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleting_A_User_Leaves_The_Linked_Teacher_Untouched()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeFalse();
        teacherUser.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Deleting_Yourself_Is_Rejected()
    {
        //when
        var act = () => interactor.ExecuteAsync(signedInAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserMustNotDeleteSelfException>(act);
        signedInAdministrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Last_Active_Administrator_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);

        //when
        var act = () => interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserMustNotBeLastActiveAdministratorException>(act);
        otherAdministrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        otherAdministrator.Delete();

        //when
        var act = () => interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
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
