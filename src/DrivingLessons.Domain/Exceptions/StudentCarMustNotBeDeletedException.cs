using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentCarMustNotBeDeletedException : DomainException
{
    public StudentCarMustNotBeDeletedException(StudentId studentId, CarId carId)
        : base($"Car {carId.Value} of student {studentId.Value} is deleted.")
    {
    }
}
