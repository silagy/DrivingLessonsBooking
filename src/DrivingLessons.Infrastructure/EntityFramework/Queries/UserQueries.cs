using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class UserQueries : IUserQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public UserQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> ExistsWithSignInEmailAsync(DrivingLessons.Domain.Values.Email signInEmail)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.SignInEmail == signInEmail);
    }

    public async Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.TeacherId == teacherId);
    }
}
