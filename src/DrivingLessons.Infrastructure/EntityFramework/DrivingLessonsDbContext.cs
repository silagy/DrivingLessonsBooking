using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<Car> Cars => Set<Car>();

    public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options)
        : base(options)
    {
    }

    public async Task CommitAsync()
    {
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
