using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationWindowExtended(PublicationId PublicationId, DateTimeOffset NewEndUtc) : IDomainEvent;
