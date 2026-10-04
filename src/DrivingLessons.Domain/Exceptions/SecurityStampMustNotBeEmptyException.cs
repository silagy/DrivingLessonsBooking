using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SecurityStampMustNotBeEmptyException : DomainException
{
    public SecurityStampMustNotBeEmptyException()
        : base("Security stamp must not be empty.")
    {
    }
}
