using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class TeacherAndCarLocks : ITeacherAndCarLocks
{
    private readonly DrivingLessonsDbContext context;

    public TeacherAndCarLocks(DrivingLessonsDbContext context)
    {
        this.context = context;
    }

    public async Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds)
    {
        await BeginTransactionIfNoneAsync();

        var teacherKeys = teacherIds.Select(x => x.Value).ToArray();
        var carKeys = carIds.Select(x => x.Value).ToArray();

        await context.Database.ExecuteSqlAsync(
            $"SELECT id FROM teachers WHERE id = ANY({teacherKeys}) ORDER BY id FOR UPDATE");
        await context.Database.ExecuteSqlAsync(
            $"SELECT id FROM cars WHERE id = ANY({carKeys}) ORDER BY id FOR UPDATE");
    }

    public async Task LockAllAsync()
    {
        await BeginTransactionIfNoneAsync();

        await context.Database.ExecuteSqlAsync($"SELECT id FROM teachers ORDER BY id FOR UPDATE");
        await context.Database.ExecuteSqlAsync($"SELECT id FROM cars ORDER BY id FOR UPDATE");
    }

    private async Task BeginTransactionIfNoneAsync()
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return;
        }

        await context.Database.BeginTransactionAsync();
    }
}
