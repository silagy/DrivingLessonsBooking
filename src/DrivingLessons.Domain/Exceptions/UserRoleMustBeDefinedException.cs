using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserRoleMustBeDefinedException : DomainException
{
    public UserRoleMustBeDefinedException()
        : base("A User's Role must be Administrator or Teacher.")
    {
    }
}
