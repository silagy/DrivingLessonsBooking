using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
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

    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync()
    {
        var query = from user in dbContext.Users
                    join teacher in dbContext.Teachers.IgnoreQueryFilters()
                        on user.TeacherId equals teacher.Id into linkedTeachers
                    from linkedTeacher in linkedTeachers.DefaultIfEmpty()
                    orderby user.IsDeleted, user.Name
                    select new ItemForFindUsersResponse
                    {
                        Id = user.Id.Value,
                        Name = user.Name.Value,
                        SignInEmail = user.SignInEmail.Value,
                        Role = user.Role,
                        TeacherId = user.TeacherId == null
                            ? null
                            : (Guid?)user.TeacherId.Value,
                        TeacherName = linkedTeacher == null
                            ? null
                            : linkedTeacher.Name.Value,
                        IsDeleted = user.IsDeleted
                    };

        return await query.ToListAsync();
    }

    public async Task<GetUserResponse?> GetAsync(Guid id)
    {
        var userId = UserId.Of(id);

        var query = from user in dbContext.Users
                    join teacher in dbContext.Teachers.IgnoreQueryFilters()
                        on user.TeacherId equals teacher.Id into linkedTeachers
                    from linkedTeacher in linkedTeachers.DefaultIfEmpty()
                    where user.Id == userId
                    select new GetUserResponse
                    {
                        Id = user.Id.Value,
                        Name = user.Name.Value,
                        SignInEmail = user.SignInEmail.Value,
                        Role = user.Role,
                        TeacherId = user.TeacherId == null
                            ? null
                            : (Guid?)user.TeacherId.Value,
                        TeacherName = linkedTeacher == null
                            ? null
                            : linkedTeacher.Name.Value,
                        IsDeleted = user.IsDeleted
                    };

        return await query.FirstOrDefaultAsync();
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
