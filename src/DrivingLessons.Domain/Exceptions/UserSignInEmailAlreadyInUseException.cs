using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserSignInEmailAlreadyInUseException : DomainException
{
    public UserSignInEmailAlreadyInUseException()
        : base("Another User already signs in with this email.")
    {
    }
}
