using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class TeacherQueries : ITeacherQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public TeacherQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetTeacherResponse?> GetAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        return await dbContext
                         .Teachers
                         .Where(x => x.Id == teacherId)
                         .Select(GetTeacherResponse.Selector)
                         .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync()
    {
        return await dbContext
                         .Teachers
                         .Select(ItemForFindTeachersResponse.Selector)
                         .ToListAsync();
    }
}
