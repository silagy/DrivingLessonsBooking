using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserCurrentPasswordMustBeCorrectException : DomainException
{
    public UserCurrentPasswordMustBeCorrectException(UserId id)
        : base($"The current password given for User {id.Value} is incorrect.")
    {
    }
}
