using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetLatestRosterImport;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class RosterImportQueries : IRosterImportQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public RosterImportQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetLatestRosterImportResponse?> GetLatestAsync()
    {
        return await dbContext
                         .RosterImports
                         .OrderByDescending(x => x.ImportedAtUtc)
                         .Select(GetLatestRosterImportResponse.Selector)
                         .FirstOrDefaultAsync();
    }
}
