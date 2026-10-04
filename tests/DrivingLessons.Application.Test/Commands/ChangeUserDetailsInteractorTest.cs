using DrivingLessons.Application.Commands.ChangeUserDetails;
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
public class ChangeUserDetailsInteractorTest
{
    private const string OwnEmail = "dana.signin@school.example";
    private const string NewName = "Dana Levi-Cohen";
    private const string FreeEmail = "dana.new@school.example";
    private const string TakenEmail = "ronit@school.example";

    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeUserDetailsInteractor interactor = null!;
    private Teacher teacher = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeUserDetailsInteractor(repository, queries, unitOfWork);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        user = User.Create(
            UserName.Of("Dana Levi"),
            Email.Of(OwnEmail),
            PasswordHash.Of("hash"),
            Role.Teacher,
            teacher);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).Returns(false);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(Email.Of(TakenEmail))).Returns(true);
    }

    [TestMethod]
    public async Task Changes_The_Name_And_Sign_In_Email()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.Name.ShouldBe(UserName.Of(NewName));
        user.SignInEmail.ShouldBe(Email.Of(FreeEmail));
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Keeping_The_Own_Email_Does_Not_Check_Uniqueness()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, OwnEmail.ToUpperInvariant());

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.Name.ShouldBe(UserName.Of(NewName));
        user.SignInEmail.ShouldBe(Email.Of(OwnEmail));
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow(TakenEmail)]
    [DataRow("RONIT@school.example")]
    [DataRow(" ronit@school.example ")]
    public async Task Email_Another_User_Has_Is_Rejected(string signInEmail)
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, signInEmail);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<UserSignInEmailAlreadyInUseException>(act);
        user.SignInEmail.ShouldBe(Email.Of(OwnEmail));
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Changing_Details_Keeps_The_Linked_Teacher()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
