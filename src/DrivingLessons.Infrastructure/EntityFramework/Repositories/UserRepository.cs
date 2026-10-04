using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public UserRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<User?> GetAsync(UserId id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(DrivingLessons.Domain.Values.Email signInEmail)
    {
        return await dbContext
                         .Users
                         .FirstOrDefaultAsync(x => x.SignInEmail == signInEmail);
    }

    public async Task<bool> AnyExistAsync()
    {
        return await dbContext.Users.AnyAsync();
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}
