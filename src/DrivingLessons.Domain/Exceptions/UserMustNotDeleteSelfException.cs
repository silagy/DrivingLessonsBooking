using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotDeleteSelfException : DomainException
{
    public UserMustNotDeleteSelfException()
        : base("A User must not delete themselves.")
    {
    }
}
