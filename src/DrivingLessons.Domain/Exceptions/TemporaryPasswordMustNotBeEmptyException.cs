using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TemporaryPasswordMustNotBeEmptyException : DomainException
{
    public TemporaryPasswordMustNotBeEmptyException()
        : base("Temporary password must not be empty.")
    {
    }
}
