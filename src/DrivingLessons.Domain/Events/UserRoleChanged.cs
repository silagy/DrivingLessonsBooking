using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserRoleChanged(UserId UserId, Role Role) : IDomainEvent;
