using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Auth;

public static class AdminSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, AdminOptions options, CancellationToken cancellationToken = default)
    {
        var hasher = new PasswordHasher<AdminUser>();
        var normalizedEmail = options.Email.Trim().ToLowerInvariant();

        var admin = await dbContext.AdminUsers.FirstOrDefaultAsync(cancellationToken);
        if (admin is null)
        {
            admin = new AdminUser { Id = Guid.NewGuid(), Email = normalizedEmail, PasswordHash = string.Empty };
            dbContext.AdminUsers.Add(admin);
        }

        admin.Email = normalizedEmail;
        admin.PasswordHash = hasher.HashPassword(admin, options.Password);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
