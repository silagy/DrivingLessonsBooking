using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public TeacherRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Teacher?> GetAsync(TeacherId id)
    {
        return await dbContext.Teachers.FindAsync(id);
    }

    public async Task<IReadOnlyCollection<Teacher>> FindActiveAsync()
    {
        return await dbContext.Teachers.ToListAsync();
    }

    public void Add(Teacher teacher)
    {
        dbContext.Teachers.Add(teacher);
    }
}
