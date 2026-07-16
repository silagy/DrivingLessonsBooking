using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record RosterImportCreated(
    RosterImportId RosterImportId,
    int AddedCount,
    int UpdatedCount,
    int DeactivatedCount,
    int FailedCount,
    DateTime ImportedAtUtc) : IDomainEvent;
