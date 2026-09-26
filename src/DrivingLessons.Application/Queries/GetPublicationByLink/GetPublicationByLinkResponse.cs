using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationByLink;

public class GetPublicationByLinkResponse
{
    public DateOnly WeekStart { get; init; }
    public int WeekNumber { get; init; }
    public PublicationState State { get; init; }
    public DateTimeOffset WindowStartUtc { get; init; }
    public DateTimeOffset WindowEndUtc { get; init; }

    public static Expression<Func<Publication, GetPublicationByLinkResponse>> Selector =>
        x => new GetPublicationByLinkResponse
        {
            WeekStart = x.WeekStart.Value,
            WeekNumber = x.WeekStart.WeekNumber,
            State = x.State,
            WindowStartUtc = x.Window!.StartUtc,
            WindowEndUtc = x.Window!.EndUtc
        };
}
