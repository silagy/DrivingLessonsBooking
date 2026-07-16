using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class RosterImportConfiguration : IEntityTypeConfiguration<RosterImport>
{
    public void Configure(EntityTypeBuilder<RosterImport> builder)
    {
        builder.ToTable("roster_imports");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<RosterImportIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasConversion<RosterFileNameConverter>();

        builder
            .Property(x => x.ImportedAtUtc)
            .HasColumnName("imported_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.AddedCount)
            .HasColumnName("added_count");

        builder
            .Property(x => x.UpdatedCount)
            .HasColumnName("updated_count");

        builder
            .Property(x => x.DeactivatedCount)
            .HasColumnName("deactivated_count");

        builder
            .Property(x => x.FailedCount)
            .HasColumnName("failed_count");

        builder.OwnsMany(
            x => x.Entries,
            entries =>
            {
                entries.ToTable("roster_import_entries");

                entries.Property<int>("id");

                entries.HasKey("id");

                entries
                    .Property<RosterImportId>("roster_import_id")
                    .HasConversion<RosterImportIdConverter>()
                    .IsRequired();

                entries
                    .WithOwner()
                    .HasForeignKey("roster_import_id");

                entries
                    .Property(e => e.NationalId)
                    .HasColumnName("national_id")
                    .HasConversion<NationalIdConverter>();

                entries
                    .Property(e => e.Outcome)
                    .HasColumnName("outcome");
            });

        builder.OwnsMany(
            x => x.Failures,
            failures =>
            {
                failures.ToTable("roster_import_failures");

                failures.Property<int>("id");

                failures.HasKey("id");

                failures
                    .Property<RosterImportId>("roster_import_id")
                    .HasConversion<RosterImportIdConverter>()
                    .IsRequired();

                failures
                    .WithOwner()
                    .HasForeignKey("roster_import_id");

                failures
                    .Property(f => f.RowNumber)
                    .HasColumnName("row_number");

                failures
                    .Property(f => f.StudentName)
                    .HasColumnName("student_name");

                failures
                    .Property(f => f.Reason)
                    .HasColumnName("reason");
            });

        builder
            .Navigation(x => x.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .Navigation(x => x.Failures)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
