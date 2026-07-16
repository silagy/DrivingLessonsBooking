using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyDeactivatedException : DomainException
{
    public StudentAlreadyDeactivatedException(StudentId id)
        : base($"Student {id.Value} is already deactivated.")
    {
    }
}
