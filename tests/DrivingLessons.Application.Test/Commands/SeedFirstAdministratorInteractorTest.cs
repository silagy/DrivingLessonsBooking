using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.SeedFirstAdministrator;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class SeedFirstAdministratorInteractorTest
{
    private const string ConfiguredName = "School Owner";
    private const string ConfiguredEmail = "owner@school.example";
    private const string ConfiguredPassword = "Configured#2026";

    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private SeedFirstAdministratorInteractor interactor = null!;
    private SeedFirstAdministratorRequest request = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new SeedFirstAdministratorInteractor(repository, passwordHasher, unitOfWork);
        request = new SeedFirstAdministratorRequest(ConfiguredName, ConfiguredEmail, ConfiguredPassword);
    }

    [TestMethod]
    public async Task Creates_The_First_Administrator_When_No_Users_Exist()
    {
        //given
        var hashed = PasswordHash.Of("hashed-configured-password");
        User? added = null;

        A.CallTo(() => repository.AnyExistAsync()).Returns(false);
        A.CallTo(() => passwordHasher.Hash(ConfiguredPassword)).Returns(hashed);
        A.CallTo(() => repository.Add(A<User>._)).Invokes((User user) => added = user);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Name.ShouldBe(UserName.Of(ConfiguredName));
        added.SignInEmail.ShouldBe(Email.Of(ConfiguredEmail));
        added.PasswordHash.ShouldBe(hashed);
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBeNull();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Leaves_Existing_Users_Untouched()
    {
        //given
        A.CallTo(() => repository.AnyExistAsync()).Returns(true);

        //when
        await interactor.ExecuteAsync(request);

        //then
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
