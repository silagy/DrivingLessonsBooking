using DrivingLessons.Application.Queries.GetLatestRosterImport;

namespace DrivingLessons.Application.Queries;

public interface IRosterImportQueries
{
    Task<GetLatestRosterImportResponse?> GetLatestAsync();
}
