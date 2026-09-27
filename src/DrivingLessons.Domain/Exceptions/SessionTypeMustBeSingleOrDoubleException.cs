using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SessionTypeMustBeSingleOrDoubleException : DomainException
{
    public SessionTypeMustBeSingleOrDoubleException()
        : base("Session type must be Single or Double.")
    {
    }
}
