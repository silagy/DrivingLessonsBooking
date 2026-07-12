using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

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

    public void Add(Teacher teacher)
    {
        dbContext.Teachers.Add(teacher);
    }
}
