using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetPublicationByLink;

public class GetPublicationByLinkInteractor
{
    private readonly IPublicationQueries queries;

    public GetPublicationByLinkInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetPublicationByLinkResponse> ExecuteAsync(string linkToken)
    {
        var publication = await queries.GetByLinkTokenExcludingDraftsAsync(linkToken)
                          ?? throw new PublicationLinkNotFoundException();

        return publication;
    }
}
