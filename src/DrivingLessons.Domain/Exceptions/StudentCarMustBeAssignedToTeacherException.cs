using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentCarMustBeAssignedToTeacherException : DomainException
{
    public StudentCarMustBeAssignedToTeacherException(CarId carId, TeacherId teacherId)
        : base($"Car {carId.Value} is not assigned to teacher {teacherId.Value}.")
    {
    }
}
