using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PasswordMustNotBeEmptyException : DomainException
{
    public PasswordMustNotBeEmptyException()
        : base("Password must not be empty.")
    {
    }
}
