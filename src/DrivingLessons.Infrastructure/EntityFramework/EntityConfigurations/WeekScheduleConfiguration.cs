using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class WeekScheduleConfiguration : IEntityTypeConfiguration<WeekSchedule>
{
    public void Configure(EntityTypeBuilder<WeekSchedule> builder)
    {
        builder.ToTable("week_schedules");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<WeekScheduleIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.WeekStart)
            .HasColumnName("week_start")
            .HasConversion<WeekStartConverter>();

        builder
            .HasIndex(x => new { x.TeacherId, x.WeekStart })
            .IsUnique();

        builder.OwnsMany(
            x => x.Slots,
            slots =>
            {
                slots.ToTable("slots");

                slots
                    .Property(s => s.Id)
                    .HasColumnName("id")
                    .HasConversion<SlotIdConverter>();

                slots.HasKey(s => s.Id);

                slots
                    .Property<WeekScheduleId>("week_schedule_id")
                    .HasConversion<WeekScheduleIdConverter>()
                    .IsRequired();

                slots
                    .WithOwner()
                    .HasForeignKey("week_schedule_id");

                slots
                    .Property(s => s.Day)
                    .HasColumnName("day");

                slots
                    .Property(s => s.Window)
                    .HasColumnName("window");

                slots
                    .Property(s => s.State)
                    .HasColumnName("state");
            });

        builder
            .Navigation(x => x.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
