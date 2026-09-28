using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Application.Commands.Common;

public record SubmissionContext(Publication Publication, Student Student, WeekSchedule WeekSchedule);
