using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAlreadyDeletedException : DomainException
{
    public TeacherAlreadyDeletedException(TeacherId id)
        : base($"Teacher {id.Value} is already deleted.")
    {
    }
}
