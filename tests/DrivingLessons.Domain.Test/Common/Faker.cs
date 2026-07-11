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
}
