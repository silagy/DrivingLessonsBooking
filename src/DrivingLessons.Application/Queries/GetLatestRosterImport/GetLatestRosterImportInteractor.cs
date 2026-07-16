using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetLatestRosterImport;

public class GetLatestRosterImportInteractor
{
    private readonly IRosterImportQueries queries;

    public GetLatestRosterImportInteractor(IRosterImportQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetLatestRosterImportResponse> ExecuteAsync()
    {
        var rosterImport = await queries.GetLatestAsync();

        if (rosterImport is null)
        {
            throw new RosterImportNotFoundException();
        }

        return rosterImport;
    }
}
