using DrivingLessons.Domain.Entities;
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

        builder
            .Property(x => x.IsDeleted)
            .HasColumnName("is_deleted");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
