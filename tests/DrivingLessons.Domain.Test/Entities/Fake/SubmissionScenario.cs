using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public record SubmissionScenario(Publication Publication, Teacher Teacher, Student Student, WeekSchedule WeekSchedule);
