using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class AdminAccountGateway(DrivingLessonsDbContext dbContext) : IAdminAccountGateway
{
    public async Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Email == normalizedEmail, cancellationToken);

        if (admin is null)
        {
            return null;
        }

        var passwordHash = PasswordHash.Of(admin.PasswordHash);

        return new AdminAccount(admin.Id, admin.Email, passwordHash);
    }
}
