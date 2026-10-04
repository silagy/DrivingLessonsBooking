using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserDetailsChanged(UserId UserId, UserName Name, Email SignInEmail) : IDomainEvent;
