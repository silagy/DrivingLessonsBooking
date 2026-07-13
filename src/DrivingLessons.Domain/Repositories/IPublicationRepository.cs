using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IPublicationRepository
{
    Task<Publication?> GetAsync(PublicationId id);

    Task<Publication?> GetByWeekAsync(WeekStart weekStart);

    Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token);

    Task<IReadOnlyList<Publication>> GetPublishedDueToOpenAsync(DateTimeOffset asOfUtc);

    Task<IReadOnlyList<Publication>> GetOpenDueToCloseAsync(DateTimeOffset asOfUtc);

    void Add(Publication publication);
}
