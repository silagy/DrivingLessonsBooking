using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserCreated(UserId UserId, UserName Name, Email SignInEmail, Role Role, TeacherId? TeacherId)
    : IDomainEvent;
