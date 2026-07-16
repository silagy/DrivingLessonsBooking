using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyActiveException : DomainException
{
    public StudentAlreadyActiveException(StudentId id)
        : base($"Student {id.Value} is already active.")
    {
    }
}
