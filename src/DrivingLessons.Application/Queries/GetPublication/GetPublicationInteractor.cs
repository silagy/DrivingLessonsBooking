using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Application.Queries.GetPublication;

public class GetPublicationInteractor
{
    private readonly IPublicationQueries queries;

    public GetPublicationInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetPublicationResponse> ExecuteAsync(DateOnly weekStart)
    {
        var publication = await queries.GetByWeekAsync(weekStart);

        if (publication is null)
        {
            throw new PublicationNotFoundException(weekStart);
        }

        return publication;
    }
}
