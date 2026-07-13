using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAlreadyAssignedToCarException : DomainException
{
    public TeacherAlreadyAssignedToCarException(CarId carId, TeacherId teacherId)
        : base($"Teacher {teacherId.Value} is already assigned to car {carId.Value}.")
    {
    }
}
