using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.CreateUser;
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
public class CreateUserInteractorTest
{
    private const string UserNameValue = "Dana Levi";
    private const string SignInEmail = "dana@school.example";
    private const string Password = "Temporary#2026";

    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ITeacherRepository teacherRepository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private CreateUserInteractor interactor = null!;
    private Teacher teacher = null!;
    private PasswordHash hashed = null!;
    private User? added;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        teacherRepository = A.Fake<ITeacherRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new CreateUserInteractor(repository, queries, teacherRepository, passwordHasher, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana.teaches@school.example"));
        hashed = PasswordHash.Of("hashed-temporary-password");
        added = null;

        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).Returns(false);
        A.CallTo(() => queries.ExistsLinkedToTeacherAsync(A<TeacherId>._)).Returns(false);
        A.CallTo(() => passwordHasher.Hash(Password)).Returns(hashed);
        A.CallTo(() => repository.Add(A<User>._)).Invokes((User user) => added = user);
    }

    [TestMethod]
    public async Task Creates_A_Teacher_Role_User_Linked_To_Its_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, teacher.Id.Value, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Name.ShouldBe(UserName.Of(UserNameValue));
        added.SignInEmail.ShouldBe(Email.Of(SignInEmail));
        added.PasswordHash.ShouldBe(hashed);
        added.Role.ShouldBe(Role.Teacher);
        added.TeacherId.ShouldBe(teacher.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Creates_An_Administrator_Without_A_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBeNull();
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Creates_An_Administrator_Linked_To_A_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, teacher.Id.Value, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Returns_The_New_User_Id()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, Password);

        //when
        var response = await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        response.Id.ShouldBe(added.Id.Value);
    }

    [TestMethod]
    public async Task Sign_In_Email_In_Use_Is_Rejected_Ignoring_Case_And_Spaces()
    {
        //given
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(Email.Of(SignInEmail))).Returns(true);
        var request = new CreateUserRequest(UserNameValue, " Dana@School.Example ", Role.Administrator, null, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserSignInEmailAlreadyInUseException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Already_Linked_To_A_User_Is_Rejected()
    {
        //given
        A.CallTo(() => queries.ExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, teacher.Id.Value, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TeacherAlreadyLinkedToUserException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Role_Without_A_Teacher_Is_Rejected()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, null, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unknown_Teacher_Is_Not_Found()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, Guid.NewGuid(), Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Temporary_Password_Is_Rejected_Before_Hashing(string blankPassword)
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, blankPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TemporaryPasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
    }
}
