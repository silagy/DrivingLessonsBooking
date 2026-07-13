using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationCreated(PublicationId PublicationId, WeekStart WeekStart, ShareableLinkToken LinkToken) : IDomainEvent;
