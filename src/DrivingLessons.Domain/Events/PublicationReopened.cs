using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationReopened(PublicationId PublicationId, DateTimeOffset NewEndUtc) : IDomainEvent;
