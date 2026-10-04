using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetUser;

public class GetUserInteractor
{
    private readonly IUserQueries queries;

    public GetUserInteractor(IUserQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetUserResponse> ExecuteAsync(Guid id)
    {
        var user = await queries.GetAsync(id);

        if (user is null)
        {
            var userId = UserId.Of(id);
            throw new UserNotFoundException(userId);
        }

        return user;
    }
}
