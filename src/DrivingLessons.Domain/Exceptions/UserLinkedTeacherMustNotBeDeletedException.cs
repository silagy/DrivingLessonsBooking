using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserLinkedTeacherMustNotBeDeletedException : DomainException
{
    public UserLinkedTeacherMustNotBeDeletedException(UserId id)
        : base($"User {id.Value} is linked to a deleted Teacher.")
    {
    }
}
