using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotBeLastActiveAdministratorException : DomainException
{
    public UserMustNotBeLastActiveAdministratorException()
        : base("The last active Administrator must stay active.")
    {
    }
}
