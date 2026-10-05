using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record RosterImportCreated(
    RosterImportId RosterImportId,
    int AddedCount,
    int UpdatedCount,
    int FailedCount,
    DateTime ImportedAtUtc) : IDomainEvent;
