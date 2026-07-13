using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class PublicationFakeBuilder
{
    public static readonly Dictionary<PublicationState, Func<Publication>> StateBuilders = new()
    {
        { PublicationState.Draft, BuildDraft },
        { PublicationState.Published, BuildPublished },
        { PublicationState.Open, BuildOpen },
        { PublicationState.Closed, BuildClosed }
    };

    public static Publication BuildDraft()
    {
        var sunday = Faker.FakeSunday();
        var weekStart = WeekStart.Of(sunday);

        return Publication.Create(weekStart);
    }

    public static Publication BuildPublished()
    {
        var publication = BuildDraft();
        var window = FakeWindow();
        publication.Publish(window);

        return publication;
    }

    public static Publication BuildOpen()
    {
        var publication = BuildPublished();
        publication.Open();

        return publication;
    }

    public static Publication BuildClosed()
    {
        var publication = BuildOpen();
        publication.Close([TeacherId.New()]);

        return publication;
    }

    public static SubmissionWindow FakeWindow()
    {
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddDays(3);

        return SubmissionWindow.Of(startUtc, endUtc);
    }
}
