using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.ChangeMyPassword;
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
public class ChangeMyPasswordInteractorTest
{
    private const string CurrentPassword = "Current#2026";
    private const string NewPassword = "Fresh#2027";
    private const string IssuedAccessToken = "fresh-access-token";

    private ICurrentUser currentUser = null!;
    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IJwtTokenGenerator tokenGenerator = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeMyPasswordInteractor interactor = null!;
    private PasswordHash storedHash = null!;
    private PasswordHash hashed = null!;
    private User user = null!;
    private DateTimeOffset expiresAtUtc;

    [TestInitialize]
    public void Init()
    {
        currentUser = A.Fake<ICurrentUser>();
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        tokenGenerator = A.Fake<IJwtTokenGenerator>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeMyPasswordInteractor(currentUser, repository, passwordHasher, tokenGenerator, unitOfWork);

        storedHash = PasswordHash.Of("stored-hash");
        hashed = PasswordHash.Of("hashed-new-password");
        expiresAtUtc = new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero);
        user = User.Create(
            UserName.Of("Yael Carmi"),
            Email.Of("yael@school.example"),
            storedHash,
            Role.Administrator,
            null);

        A.CallTo(() => currentUser.Id).Returns(user.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => passwordHasher.Verify(A<PasswordHash>._, A<string>._)).Returns(false);
        A.CallTo(() => passwordHasher.Verify(storedHash, CurrentPassword)).Returns(true);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).Returns(hashed);
        A.CallTo(() => tokenGenerator.Generate(user)).Returns(new IssuedToken(IssuedAccessToken, expiresAtUtc));
    }

    [TestMethod]
    public async Task Changes_The_Password_And_Returns_A_Fresh_Token()
    {
        //given
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var result = await interactor.ExecuteAsync(request);

        //then
        user.PasswordHash.ShouldBe(hashed);
        result.AccessToken.ShouldBe(IssuedAccessToken);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        A.CallTo(() => passwordHasher.Hash(NewPassword)).MustHaveHappenedOnceExactly();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly()
         .Then(A.CallTo(() => tokenGenerator.Generate(user)).MustHaveHappenedOnceExactly());
    }

    [TestMethod]
    public async Task The_Fresh_Token_Carries_The_New_Security_Stamp()
    {
        //given
        var stampBefore = user.SecurityStamp;
        SecurityStamp? stampAtIssue = null;
        A.CallTo(() => tokenGenerator.Generate(user))
         .ReturnsLazily(() =>
         {
             stampAtIssue = user.SecurityStamp;
             return new IssuedToken(IssuedAccessToken, expiresAtUtc);
         });
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        await interactor.ExecuteAsync(request);

        //then
        stampAtIssue.ShouldNotBeNull();
        stampAtIssue.ShouldNotBe(stampBefore);
        stampAtIssue.ShouldBe(user.SecurityStamp);
    }

    [TestMethod]
    public async Task Surrounding_Spaces_In_The_New_Password_Are_Kept()
    {
        //given
        const string spaced = "  Fresh 2027  ";
        var request = new ChangeMyPasswordRequest(CurrentPassword, spaced);

        //when
        await interactor.ExecuteAsync(request);

        //then
        A.CallTo(() => passwordHasher.Hash(spaced)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("Wrong#2026")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Wrong_Current_Password_Is_Rejected(string? currentPassword)
    {
        //given
        var stampBefore = user.SecurityStamp;
        var request = new ChangeMyPasswordRequest(currentPassword!, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserCurrentPasswordMustBeCorrectException>(act);
        user.PasswordHash.ShouldBe(storedHash);
        user.SecurityStamp.ShouldBe(stampBefore);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
        A.CallTo(() => tokenGenerator.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task Blank_New_Password_Is_Rejected(string? newPassword)
    {
        //given
        var request = new ChangeMyPasswordRequest(CurrentPassword, newPassword!);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<PasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Verify(A<PasswordHash>._, A<string>._)).MustNotHaveHappened();
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        user.Delete();
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
        A.CallTo(() => tokenGenerator.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Signed_In_User_Is_Not_Found()
    {
        //given
        A.CallTo(() => currentUser.Id).Returns(UserId.New());
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
