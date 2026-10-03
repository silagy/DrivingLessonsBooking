using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAlreadyLinkedToUserException : DomainException
{
    public TeacherAlreadyLinkedToUserException(TeacherId id)
        : base($"Teacher {id.Value} is already linked to a User.")
    {
    }
}
