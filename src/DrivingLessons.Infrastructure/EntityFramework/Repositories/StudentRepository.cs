using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public StudentRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Student>> FindAllAsync()
    {
        return await dbContext.Students.ToListAsync();
    }

    public void Add(Student student)
    {
        dbContext.Students.Add(student);
    }
}
