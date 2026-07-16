using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Common;

public static class Faker
{
    public static string FakeString()
    {
        return $"fake-{Guid.NewGuid():N}";
    }

    public static NationalId FakeNationalId()
    {
        var digits = new int[9];

        for (var i = 0; i < 8; i++)
        {
            digits[i] = Random.Shared.Next(0, 10);
        }

        var sum = 0;

        for (var i = 0; i < 8; i++)
        {
            var weight = i % 2 == 0
                ? 1
                : 2;
            var product = digits[i] * weight;
            var reduced = product > 9
                ? product - 9
                : product;
            sum += reduced;
        }

        digits[8] = (10 - (sum % 10)) % 10;

        return NationalId.Of(string.Concat(digits));
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

    public static string FakePhoneNumber()
    {
        var prefixDigit = Random.Shared.Next(0, 10);
        var subscriber = Random.Shared.Next(1000000, 10000000);

        return $"05{prefixDigit}-{subscriber}";
    }

    public static DateOnly FakeDate()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysAhead = Random.Shared.Next(1, 365);

        return today.AddDays(daysAhead);
    }

    public static DateTime FakeUtcDate()
    {
        var minutesAhead = Random.Shared.Next(1, 100000);

        return DateTime.UtcNow.AddMinutes(minutesAhead);
    }
}
