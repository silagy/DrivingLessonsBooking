using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
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

    public async Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId)
    {
        return await dbContext
                         .Students
                         .FirstOrDefaultAsync(x => x.NationalId == nationalId && x.IsActive);
    }

    public async Task<Student?> GetByNationalIdAsync(NationalId nationalId)
    {
        return await dbContext
                         .Students
                         .FirstOrDefaultAsync(x => x.NationalId == nationalId);
    }

    public void Add(Student student)
    {
        dbContext.Students.Add(student);
    }
}
