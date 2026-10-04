using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetUserInteractorTest
{
    private IUserQueries queries = null!;
    private GetUserInteractor interactor = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IUserQueries>();
        interactor = new GetUserInteractor(queries);
    }

    [TestMethod]
    public async Task Returns_The_User()
    {
        //given
        var id = Guid.NewGuid();
        var user = new GetUserResponse
        {
            Id = id,
            Name = "Dana Levi",
            SignInEmail = "dana@school.example",
            Role = Role.Administrator,
            TeacherId = null,
            TeacherName = null,
            IsDeleted = false
        };
        A.CallTo(() => queries.GetAsync(id)).Returns(user);

        //when
        var result = await interactor.ExecuteAsync(id);

        //then
        result.ShouldBe(user);
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var id = Guid.NewGuid();
        A.CallTo(() => queries.GetAsync(id)).Returns((GetUserResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(id);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
    }
}
