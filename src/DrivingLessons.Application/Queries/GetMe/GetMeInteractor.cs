using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries.GetUser;

namespace DrivingLessons.Application.Queries.GetMe;

public class GetMeInteractor
{
    private readonly IUserQueries queries;
    private readonly ICurrentUser currentUser;

    public GetMeInteractor(IUserQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<GetUserResponse> ExecuteAsync()
    {
        var userId = currentUser.Id;
        var id = userId.Value;

        var user = await queries.GetAsync(id);

        if (user is null)
        {
            throw new UserNotFoundException(userId);
        }

        return user;
    }
}
