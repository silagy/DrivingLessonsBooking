using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class LoginInteractorTest
{
    private const string SignInEmail = "owner@school.example";
    private const string CorrectPassword = "Correct#Horse2026";
    private const string IssuedAccessToken = "issued-access-token";

    private IUserRepository users = null!;
    private IPasswordHasher passwords = null!;
    private IJwtTokenGenerator tokens = null!;
    private LoginInteractor interactor = null!;
    private User user = null!;
    private DateTimeOffset expiresAtUtc;

    [TestInitialize]
    public void Init()
    {
        users = A.Fake<IUserRepository>();
        passwords = A.Fake<IPasswordHasher>();
        tokens = A.Fake<IJwtTokenGenerator>();
        interactor = new LoginInteractor(users, passwords, tokens);
        user = User.Create(
            UserName.Of("School Owner"),
            Email.Of(SignInEmail),
            PasswordHash.Of("stored-hash"),
            Role.Administrator,
            null);
        expiresAtUtc = new DateTimeOffset(2026, 10, 3, 22, 0, 0, TimeSpan.Zero);

        A.CallTo(() => users.GetByEmailAsync(A<Email>._)).Returns((User?)null);
        A.CallTo(() => users.GetByEmailAsync(Email.Of(SignInEmail))).Returns(user);
        A.CallTo(() => passwords.Verify(user.PasswordHash, CorrectPassword)).Returns(true);
        A.CallTo(() => tokens.Generate(user)).Returns(new IssuedToken(IssuedAccessToken, expiresAtUtc));
    }

    [TestMethod]
    public async Task Signs_In_A_User_With_The_Right_Password()
    {
        //given
        var command = new LoginCommand(SignInEmail, CorrectPassword);

        //when
        var result = await interactor.ExecuteAsync(command);

        //then
        result.AccessToken.ShouldBe(IssuedAccessToken);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
    }

    [TestMethod]
    public async Task Sign_In_Email_Ignores_Case_And_Surrounding_Spaces()
    {
        //given
        var command = new LoginCommand("  Owner@School.Example ", CorrectPassword);

        //when
        var result = await interactor.ExecuteAsync(command);

        //then
        result.AccessToken.ShouldBe(IssuedAccessToken);
    }

    [TestMethod]
    public async Task Wrong_Password_Is_Rejected()
    {
        //given
        var command = new LoginCommand(SignInEmail, "Wrong#Horse2026");

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Missing_Password_Is_Rejected()
    {
        //given
        var command = new LoginCommand(SignInEmail, null!);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Unknown_Email_Is_Rejected()
    {
        //given
        var command = new LoginCommand("stranger@school.example", CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_Like_A_Wrong_Password()
    {
        //given
        user.Delete();
        var command = new LoginCommand(SignInEmail, CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
        A.CallTo(() => tokens.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("not-an-email")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Malformed_Email_Is_Rejected_Like_A_Wrong_Password(string? email)
    {
        //given
        var command = new LoginCommand(email!, CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
        A.CallTo(() => users.GetByEmailAsync(A<Email>._)).MustNotHaveHappened();
    }
}
