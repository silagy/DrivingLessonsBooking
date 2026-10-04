using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotChangeOwnRoleException : DomainException
{
    public UserMustNotChangeOwnRoleException()
        : base("A User must not change their own Role.")
    {
    }
}
