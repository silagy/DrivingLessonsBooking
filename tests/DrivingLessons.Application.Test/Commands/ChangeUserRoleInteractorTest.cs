using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.ChangeUserRole;
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
public class ChangeUserRoleInteractorTest
{
    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeUserRoleInteractor interactor = null!;
    private User signedInAdministrator = null!;
    private Teacher ronitTeacher = null!;
    private User linkedAdministrator = null!;
    private User unlinkedAdministrator = null!;
    private Teacher danaTeacher = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeUserRoleInteractor(repository, queries, currentUser, unitOfWork);

        signedInAdministrator = UserOf("owner@school.example", Role.Administrator, null);
        ronitTeacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        linkedAdministrator = UserOf("ronit.signin@school.example", Role.Administrator, ronitTeacher);
        unlinkedAdministrator = UserOf("amit@school.example", Role.Administrator, null);
        danaTeacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, danaTeacher);

        A.CallTo(() => currentUser.Id).Returns(signedInAdministrator.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(signedInAdministrator.Id)).Returns(signedInAdministrator);
        A.CallTo(() => repository.GetAsync(linkedAdministrator.Id)).Returns(linkedAdministrator);
        A.CallTo(() => repository.GetAsync(unlinkedAdministrator.Id)).Returns(unlinkedAdministrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(3);
    }

    [TestMethod]
    public async Task Gives_A_Teacher_Role_User_The_Administrator_Role_And_Keeps_The_Link()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Administrator);

        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value, request);

        //then
        teacherUser.Role.ShouldBe(Role.Administrator);
        teacherUser.TeacherId.ShouldBe(danaTeacher.Id);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Gives_A_Linked_Administrator_The_Teacher_Role()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        await interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        linkedAdministrator.Role.ShouldBe(Role.Teacher);
        linkedAdministrator.TeacherId.ShouldBe(ronitTeacher.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Unlinked_Administrator_Does_Not_Get_The_Teacher_Role()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(unlinkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        unlinkedAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Demoting_Yourself_Is_Rejected_Before_The_Teacher_Link_Rule()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(signedInAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserMustNotChangeOwnRoleException>(act);
        signedInAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Last_Active_Administrator_Keeps_The_Administrator_Role()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserMustNotDemoteLastActiveAdministratorException>(act);
        linkedAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Same_Role_Is_Rejected_Without_Counting_Administrators()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(teacherUser.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyHasRoleException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        linkedAdministrator.Delete();
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

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
