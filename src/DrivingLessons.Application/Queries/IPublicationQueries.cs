using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;

namespace DrivingLessons.Application.Queries;

public interface IPublicationQueries
{
    Task<GetPublicationResponse?> GetByWeekAsync(DateOnly weekStart);

    Task<GetPublicationDashboardResponse?> GetDashboardAsync(Guid publicationId, Guid teacherId);

    Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync();

    Task<IReadOnlyList<Guid>> GetTeacherIdsWithScheduleForWeekAsync(DateOnly weekStart);
}
