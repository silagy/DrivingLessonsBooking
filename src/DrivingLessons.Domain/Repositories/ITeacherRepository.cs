using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ITeacherRepository
{
    Task<Teacher?> GetAsync(TeacherId id);

    void Add(Teacher teacher);
}
