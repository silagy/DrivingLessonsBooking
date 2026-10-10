using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class CarMustNotHaveActiveStudentsException : DomainException
{
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public CarMustNotHaveActiveStudentsException(CarId id, IReadOnlyList<StudentName> activeStudentNames)
        : base($"Car {id.Value} still has {activeStudentNames.Count} active Students.")
    {
        ActiveStudentNames = activeStudentNames;
    }
}
