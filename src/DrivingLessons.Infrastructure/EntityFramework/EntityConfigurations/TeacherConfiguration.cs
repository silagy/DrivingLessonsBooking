using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<TeacherIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<TeacherNameConverter>();

        builder
            .Property(x => x.ContactEmail)
            .HasColumnName("contact_email")
            .HasConversion<EmailConverter>();

        builder.OwnsMany(
            x => x.Cars,
            cars =>
            {
                cars.ToTable("cars");

                cars
                    .Property(c => c.Id)
                    .HasColumnName("id")
                    .HasConversion<CarIdConverter>();

                cars.HasKey(c => c.Id);

                cars
                    .Property<TeacherId>("teacher_id")
                    .HasConversion<TeacherIdConverter>()
                    .IsRequired();

                cars.WithOwner().HasForeignKey("teacher_id");

                cars
                    .Property(c => c.Name)
                    .HasColumnName("name")
                    .HasConversion<CarNameConverter>();

                cars
                    .Property(c => c.Type)
                    .HasColumnName("type")
                    .HasConversion<CarTypeConverter>();

                cars
                    .Property(c => c.Transmission)
                    .HasColumnName("transmission");
            });

        builder
            .Navigation(x => x.Cars)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
