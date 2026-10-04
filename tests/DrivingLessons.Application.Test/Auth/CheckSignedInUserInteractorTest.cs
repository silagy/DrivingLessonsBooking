using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class CheckSignedInUserInteractorTest
{
    private IUserRepository repository = null!;
    private CheckSignedInUserInteractor interactor = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        interactor = new CheckSignedInUserInteractor(repository);
        user = User.Create(
            UserName.Of("Dana User"),
            Email.Of("dana.user@school.example"),
            PasswordHash.Of("hash"),
            Role.Administrator,
            null);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
    }

    [TestMethod]
    public async Task Active_User_With_The_Current_Stamp_Is_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Not_Signed_In()
    {
        //given
        var stampInToken = user.SecurityStamp.Value;
        user.Delete();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), stampInToken);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Not_Signed_In_Even_With_The_New_Stamp()
    {
        //given
        user.Delete();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Restored_User_Is_Not_Signed_In_With_A_Token_From_Before_The_Delete()
    {
        //given
        var stampInToken = user.SecurityStamp.Value;
        user.Delete();
        user.Restore();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), stampInToken);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(Guid.NewGuid().ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    [DataRow("")]
    [DataRow("00000000-0000-0000-0000-000000000000")]
    public async Task Malformed_User_Id_Is_Not_Signed_In(string userIdClaim)
    {
        //when
        var signedIn = await interactor.ExecuteAsync(userIdClaim, user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_User_Id_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(null, user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Security_Stamp_Is_Not_Signed_In(string securityStampClaim)
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), securityStampClaim);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_Security_Stamp_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), null);

        //then
        signedIn.ShouldBeFalse();
    }
}
