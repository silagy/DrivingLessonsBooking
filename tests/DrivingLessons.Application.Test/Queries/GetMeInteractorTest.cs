using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetMe;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetMeInteractorTest
{
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private GetMeInteractor interactor = null!;
    private UserId signedInUserId = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetMeInteractor(queries, currentUser);

        signedInUserId = UserId.New();

        A.CallTo(() => currentUser.Id).Returns(signedInUserId);
        A.CallTo(() => queries.GetAsync(A<Guid>._)).Returns((GetUserResponse?)null);
    }

    [TestMethod]
    public async Task Returns_The_Signed_In_User_With_The_Linked_Teacher()
    {
        //given
        var teacherId = Guid.NewGuid();
        var me = new GetUserResponse
        {
            Id = signedInUserId.Value,
            Name = "Yael Carmi",
            SignInEmail = "yael.user@school.example",
            Role = Role.Teacher,
            TeacherId = teacherId,
            TeacherName = "Yael Carmi",
            IsDeleted = false
        };
        A.CallTo(() => queries.GetAsync(signedInUserId.Value)).Returns(me);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(me);
        result.TeacherId.ShouldBe(teacherId);
        result.TeacherName.ShouldBe("Yael Carmi");
        A.CallTo(() => queries.GetAsync(signedInUserId.Value)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Signed_In_User_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync();

        //then
        var exception = await Should.ThrowAsync<UserNotFoundException>(act);
        exception.Message.ShouldContain(signedInUserId.Value.ToString());
    }
}
