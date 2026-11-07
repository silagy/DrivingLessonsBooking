using DrivingLessons.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class SentTeacherExcelEmailConfiguration : IEntityTypeConfiguration<SentTeacherExcelEmail>
{
    public void Configure(EntityTypeBuilder<SentTeacherExcelEmail> builder)
    {
        builder.ToTable("sent_teacher_excel_emails");

        builder
            .Property(x => x.PublicationId)
            .HasColumnName("publication_id");

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id");

        builder
            .Property(x => x.Version)
            .HasColumnName("version");

        builder
            .Property(x => x.SentAtUtc)
            .HasColumnName("sent_at_utc")
            .HasColumnType("timestamptz");

        builder.HasKey(x => new { x.PublicationId, x.TeacherId, x.Version });
    }
}
