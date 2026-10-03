using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserNameMustNotBeEmptyException : DomainException
{
    public UserNameMustNotBeEmptyException()
        : base("User name must not be empty.")
    {
    }
}
