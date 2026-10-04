using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyActiveException : DomainException
{
    public UserAlreadyActiveException(UserId id)
        : base($"User {id.Value} is already active.")
    {
    }
}
