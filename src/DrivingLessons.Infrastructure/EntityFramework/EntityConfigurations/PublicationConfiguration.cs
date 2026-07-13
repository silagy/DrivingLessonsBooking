using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<PublicationIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.WeekStart)
            .HasColumnName("week_start")
            .HasConversion<WeekStartConverter>();

        builder
            .HasIndex(x => x.WeekStart)
            .IsUnique();

        builder
            .Property(x => x.State)
            .HasColumnName("state");

        builder
            .Property(x => x.LinkToken)
            .HasColumnName("link_token")
            .HasConversion<ShareableLinkTokenConverter>();

        builder
            .HasIndex(x => x.LinkToken)
            .IsUnique();

        builder.OwnsOne(
            x => x.Window,
            window =>
            {
                window
                    .Property(w => w.StartUtc)
                    .HasColumnName("window_start_utc")
                    .HasColumnType("timestamptz");

                window
                    .Property(w => w.EndUtc)
                    .HasColumnName("window_end_utc")
                    .HasColumnType("timestamptz");
            });

        builder.OwnsMany(
            x => x.TeacherVersions,
            versions =>
            {
                versions.ToTable("publication_teacher_versions");

                versions
                    .Property(v => v.Id)
                    .HasColumnName("id")
                    .HasConversion<TeacherExcelVersionIdConverter>();

                versions.HasKey(v => v.Id);

                versions
                    .Property<PublicationId>("publication_id")
                    .HasConversion<PublicationIdConverter>()
                    .IsRequired();

                versions
                    .WithOwner()
                    .HasForeignKey("publication_id");

                versions
                    .Property(v => v.TeacherId)
                    .HasColumnName("teacher_id")
                    .HasConversion<TeacherIdConverter>();

                versions
                    .Property(v => v.Version)
                    .HasColumnName("version");
            });

        builder
            .Navigation(x => x.TeacherVersions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
