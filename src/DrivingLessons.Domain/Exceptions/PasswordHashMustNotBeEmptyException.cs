using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PasswordHashMustNotBeEmptyException : DomainException
{
    public PasswordHashMustNotBeEmptyException()
        : base("Password hash must not be empty.")
    {
    }
}
