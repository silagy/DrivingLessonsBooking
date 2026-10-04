using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyHasRoleException : DomainException
{
    public UserAlreadyHasRoleException(UserId id)
        : base($"User {id.Value} already has this Role.")
    {
    }
}
