using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    void Add(Student student);
}
