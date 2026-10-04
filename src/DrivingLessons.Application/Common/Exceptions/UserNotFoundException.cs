using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(UserId id)
        : base($"User {id.Value} was not found.")
    {
    }
}
