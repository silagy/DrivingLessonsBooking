using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class CarNotInTeacherException : DomainException
{
    public CarNotInTeacherException(TeacherId teacherId, CarId carId)
        : base($"Teacher {teacherId.Value} does not own car {carId.Value}.")
    {
    }
}
