namespace DrivingLessons.Application.Common.Exceptions;

public class StudentNotFoundException : NotFoundException
{
    public StudentNotFoundException()
        : base("No active student on the roster matches this national ID.")
    {
    }
}
