using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationPublished(PublicationId PublicationId, SubmissionWindow Window) : IDomainEvent;
