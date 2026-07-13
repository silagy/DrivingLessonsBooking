using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class PublicationRepository : IPublicationRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public PublicationRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Publication?> GetAsync(PublicationId id)
    {
        return await dbContext.Publications.FindAsync(id);
    }

    public async Task<Publication?> GetByWeekAsync(WeekStart weekStart)
    {
        return await dbContext
                         .Publications
                         .FirstOrDefaultAsync(x => x.WeekStart == weekStart);
    }

    public async Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token)
    {
        return await dbContext
                         .Publications
                         .FirstOrDefaultAsync(x => x.LinkToken == token);
    }

    public async Task<IReadOnlyList<Publication>> GetPublishedDueToOpenAsync(DateTimeOffset asOfUtc)
    {
        return await dbContext
                         .Publications
                         .Where(x => x.State == PublicationState.Published && x.Window!.StartUtc <= asOfUtc)
                         .ToListAsync();
    }

    public async Task<IReadOnlyList<Publication>> GetOpenDueToCloseAsync(DateTimeOffset asOfUtc)
    {
        return await dbContext
                         .Publications
                         .Where(x => x.State == PublicationState.Open && x.Window!.EndUtc <= asOfUtc)
                         .ToListAsync();
    }

    public void Add(Publication publication)
    {
        dbContext.Publications.Add(publication);
    }
}
