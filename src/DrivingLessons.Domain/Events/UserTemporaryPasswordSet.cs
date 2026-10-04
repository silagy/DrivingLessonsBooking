using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserTemporaryPasswordSet(UserId UserId) : IDomainEvent;
