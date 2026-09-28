using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SubmissionCreated(
    SubmissionId SubmissionId,
    PublicationId PublicationId,
    StudentId StudentId,
    WeekScheduleId WeekScheduleId,
    TargetSessionCount TargetCount,
    DateTimeOffset SubmittedAtUtc) : IDomainEvent;
