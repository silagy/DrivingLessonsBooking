using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class RosterImportRepository : IRosterImportRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public RosterImportRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void Add(RosterImport rosterImport)
    {
        dbContext.RosterImports.Add(rosterImport);
    }
}
