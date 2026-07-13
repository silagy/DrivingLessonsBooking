using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublication;

public class GetPublicationResponse
{
    public Guid Id { get; init; }
    public DateOnly WeekStart { get; init; }
    public PublicationState State { get; init; }
    public string LinkToken { get; init; } = string.Empty;
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }

    public static Expression<Func<Publication, GetPublicationResponse>> Selector =>
        x => new GetPublicationResponse
        {
            Id = x.Id.Value,
            WeekStart = x.WeekStart.Value,
            State = x.State,
            LinkToken = x.LinkToken.Value,
            WindowStartUtc = x.Window == null
                ? (DateTimeOffset?)null
                : x.Window.StartUtc,
            WindowEndUtc = x.Window == null
                ? (DateTimeOffset?)null
                : x.Window.EndUtc
        };
}
