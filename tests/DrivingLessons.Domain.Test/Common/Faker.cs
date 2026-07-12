namespace DrivingLessons.Domain.Test.Common;

public static class Faker
{
    public static string FakeString()
    {
        return $"fake-{Guid.NewGuid():N}";
    }

    public static string FakeEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }

    public static DateOnly FakeSunday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
        var weeksAhead = Random.Shared.Next(1, 52);

        return today.AddDays(daysUntilSunday + (weeksAhead * 7));
    }
}
