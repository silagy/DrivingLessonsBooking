using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class WeekScheduleFakeBuilder
{
    public static WeekSchedule Build()
    {
        var teacher = TeacherFakeBuilder.Build();
        var sunday = Faker.FakeSunday();
        var weekStart = WeekStart.Of(sunday);

        return WeekSchedule.Create(teacher, weekStart);
    }
}
