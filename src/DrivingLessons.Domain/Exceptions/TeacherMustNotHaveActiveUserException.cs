using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherMustNotHaveActiveUserException : DomainException
{
    public TeacherMustNotHaveActiveUserException(TeacherId id)
        : base($"Teacher {id.Value} still has an active User.")
    {
    }
}
