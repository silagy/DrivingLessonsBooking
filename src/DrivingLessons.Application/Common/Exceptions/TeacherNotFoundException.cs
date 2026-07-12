using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class TeacherNotFoundException : NotFoundException
{
    public TeacherNotFoundException(TeacherId id)
        : base($"Teacher {id.Value} was not found.")
    {
    }
}
