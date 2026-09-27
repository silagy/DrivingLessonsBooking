using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class SubmissionFakeBuilder
{
    public static SubmissionScenario BuildScenario()
    {
        var publication = PublicationFakeBuilder.BuildOpen();

        return BuildScenarioFor(publication);
    }

    public static SubmissionScenario BuildScenarioFor(Publication publication)
    {
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var weekSchedule = WeekSchedule.Create(teacher, publication.WeekStart);

        return new SubmissionScenario(publication, teacher, student, weekSchedule);
    }

    public static IReadOnlyList<SlotPick> PicksOf(WeekSchedule weekSchedule, int count)
    {
        return weekSchedule
                   .Slots
                   .Take(count)
                   .Select(slot => SlotPick.Of(slot, SessionType.Single, null))
                   .ToList();
    }

    public static Submission Build(
        SubmissionScenario scenario,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks)
    {
        var submittedAtUtc = Faker.FakeUtcInstant();

        return Submission.Create(
            scenario.Publication,
            scenario.Student,
            scenario.WeekSchedule,
            targetCount,
            picks,
            submittedAtUtc);
    }

    public static Submission Build(SubmissionScenario scenario)
    {
        var targetCount = TargetSessionCount.Of(1);
        var picks = PicksOf(scenario.WeekSchedule, 2);

        return Build(scenario, targetCount, picks);
    }
}
