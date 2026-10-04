using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.SetUserTemporaryPassword;
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
public class SetUserTemporaryPasswordInteractorTest
{
    private const string Password = "Temporary#2027";

    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private SetUserTemporaryPasswordInteractor interactor = null!;
    private PasswordHash hashed = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new SetUserTemporaryPasswordInteractor(repository, passwordHasher, unitOfWork);

        hashed = PasswordHash.Of("hashed-temporary-password");
        user = User.Create(
            UserName.Of("Michal Dahan"),
            Email.Of("michal@school.example"),
            PasswordHash.Of("old-hash"),
            Role.Administrator,
            null);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => passwordHasher.Hash(Password)).Returns(hashed);
    }

    [TestMethod]
    public async Task Stores_The_Hashed_Temporary_Password_And_Signs_The_User_Out()
    {
        //given
        var stampBefore = user.SecurityStamp;
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.PasswordHash.ShouldBe(hashed);
        user.SecurityStamp.ShouldNotBe(stampBefore);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Temporary_Password_Is_Rejected(string password)
    {
        //given
        var request = new SetUserTemporaryPasswordRequest(password);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<TemporaryPasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        user.Delete();
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
