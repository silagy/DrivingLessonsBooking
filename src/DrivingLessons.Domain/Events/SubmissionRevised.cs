using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SubmissionRevised(
    SubmissionId SubmissionId,
    WeekScheduleId WeekScheduleId,
    TargetSessionCount TargetCount,
    DateTimeOffset RevisedAtUtc) : IDomainEvent;
