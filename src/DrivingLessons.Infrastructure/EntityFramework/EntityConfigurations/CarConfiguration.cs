using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("cars");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<CarIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<CarNameConverter>();

        builder
            .Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<CarTypeConverter>();

        builder
            .Property(x => x.Transmission)
            .HasColumnName("transmission");

        builder
            .Property(x => x.IsDeleted)
            .HasColumnName("is_deleted");

        builder.OwnsMany(
            x => x.TeacherAssignments,
            assignments =>
            {
                assignments.ToTable("car_teachers");

                assignments
                    .Property(a => a.Id)
                    .HasColumnName("id")
                    .HasConversion<TeacherAssignmentIdConverter>();

                assignments.HasKey(a => a.Id);

                assignments
                    .Property<CarId>("car_id")
                    .HasConversion<CarIdConverter>()
                    .IsRequired();

                assignments.WithOwner().HasForeignKey("car_id");

                assignments
                    .Property(a => a.TeacherId)
                    .HasColumnName("teacher_id")
                    .HasConversion<TeacherIdConverter>();

                assignments
                    .HasIndex("car_id", nameof(TeacherAssignment.TeacherId))
                    .IsUnique();
            });

        builder
            .Navigation(x => x.TeacherAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
