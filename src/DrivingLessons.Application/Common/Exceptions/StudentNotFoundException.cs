using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class StudentNotFoundException : NotFoundException
{
    public StudentNotFoundException()
        : base("No active student on the roster matches this national ID.")
    {
    }

    public StudentNotFoundException(StudentId id)
        : base($"Student {id.Value} was not found.")
    {
    }
}
