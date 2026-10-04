using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyDeletedException : DomainException
{
    public UserAlreadyDeletedException(UserId id)
        : base($"User {id.Value} is already deleted.")
    {
    }
}
