using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ITeacherRepository
{
    Task<Teacher?> GetAsync(TeacherId id);

    Task<Teacher?> GetIncludingDeletedAsync(TeacherId id);

    Task<IReadOnlyCollection<Teacher>> FindActiveAsync();

    void Add(Teacher teacher);
}
