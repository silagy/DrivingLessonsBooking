using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    Task<Student?> GetAsync(StudentId id);

    Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId);

    Task<Student?> GetByNationalIdAsync(NationalId nationalId);

    void Add(Student student);
}
