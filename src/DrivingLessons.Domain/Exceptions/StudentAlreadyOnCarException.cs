using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyOnCarException : DomainException
{
    public StudentAlreadyOnCarException(StudentId id, CarId carId)
        : base($"Student {id.Value} is already on car {carId.Value}.")
    {
    }
}
