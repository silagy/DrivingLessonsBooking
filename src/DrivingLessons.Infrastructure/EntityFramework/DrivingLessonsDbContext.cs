using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher dispatcher;

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();

    public DbSet<Publication> Publications => Set<Publication>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options, IDomainEventDispatcher dispatcher)
        : base(options)
    {
        this.dispatcher = dispatcher;
    }

    public async Task CommitAsync()
    {
        const int maxCycles = 10;

        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            await SaveChangesAsync();

            var roots = ChangeTracker.Entries<IHasDomainEvents>()
                                     .Where(x => x.Entity.UncommittedEvents.Count > 0)
                                     .Select(x => x.Entity)
                                     .ToList();

            if (roots.Count == 0)
            {
                return;
            }

            var domainEvents = roots.SelectMany(x => x.UncommittedEvents).ToList();
            roots.ForEach(x => x.CommitEvents());

            foreach (var domainEvent in domainEvents)
            {
                await dispatcher.DispatchAsync(domainEvent);
            }
        }

        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
