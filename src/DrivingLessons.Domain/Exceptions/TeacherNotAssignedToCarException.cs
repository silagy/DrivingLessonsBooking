using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherNotAssignedToCarException : DomainException
{
    public TeacherNotAssignedToCarException(CarId carId, TeacherId teacherId)
        : base($"Teacher {teacherId.Value} is not assigned to car {carId.Value}.")
    {
    }
}
