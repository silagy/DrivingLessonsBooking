using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<StudentIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.NationalId)
            .HasColumnName("national_id")
            .HasConversion<NationalIdConverter>();

        builder
            .HasIndex(x => x.NationalId)
            .IsUnique();

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<StudentNameConverter>();

        builder
            .Property(x => x.Phone)
            .HasColumnName("phone")
            .HasConversion<PhoneNumberConverter>();

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.CarId)
            .HasColumnName("car_id")
            .HasConversion<CarIdConverter>();

        builder
            .Property(x => x.Address)
            .HasColumnName("address")
            .HasConversion<AddressConverter>();

        builder
            .Property(x => x.StartDate)
            .HasColumnName("start_date")
            .HasConversion<LessonsStartDateConverter>();

        builder
            .Property(x => x.LicenseType)
            .HasColumnName("license_type")
            .HasConversion<LicenseTypeConverter>();

        builder
            .Property(x => x.IsActive)
            .HasColumnName("is_active");
    }
}
