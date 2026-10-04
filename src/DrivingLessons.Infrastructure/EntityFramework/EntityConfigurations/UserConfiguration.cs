using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const int EmailMaxLength = 320;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<UserIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<UserNameConverter>();

        builder
            .Property(x => x.SignInEmail)
            .HasColumnName("email")
            .HasMaxLength(EmailMaxLength)
            .HasConversion<EmailConverter>();

        builder
            .HasIndex(x => x.SignInEmail)
            .IsUnique();

        builder
            .Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion<PasswordHashConverter>();

        builder
            .Property(x => x.Role)
            .HasColumnName("role");

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasConversion<SecurityStampConverter>();

        builder
            .Property(x => x.IsDeleted)
            .HasColumnName("is_deleted");
    }
}
