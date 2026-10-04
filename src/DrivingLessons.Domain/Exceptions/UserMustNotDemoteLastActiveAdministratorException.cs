using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotDemoteLastActiveAdministratorException : DomainException
{
    public UserMustNotDemoteLastActiveAdministratorException()
        : base("The last active Administrator must keep the Administrator Role.")
    {
    }
}
