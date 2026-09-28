using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<SubmissionIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.PublicationId)
            .HasColumnName("publication_id")
            .HasConversion<PublicationIdConverter>();

        builder
            .Property(x => x.StudentId)
            .HasColumnName("student_id")
            .HasConversion<StudentIdConverter>();

        builder
            .HasIndex(x => new { x.PublicationId, x.StudentId })
            .IsUnique();

        builder
            .Property(x => x.WeekScheduleId)
            .HasColumnName("week_schedule_id")
            .HasConversion<WeekScheduleIdConverter>();

        builder.HasIndex(x => x.WeekScheduleId);

        builder
            .Property(x => x.TargetCount)
            .HasColumnName("target_count")
            .HasConversion<TargetSessionCountConverter>();

        builder
            .Property(x => x.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.RevisedAtUtc)
            .HasColumnName("revised_at_utc")
            .HasColumnType("timestamptz");

        builder.OwnsMany(
            x => x.SlotRequests,
            requests =>
            {
                requests.ToTable("slot_requests");

                requests
                    .Property(r => r.Id)
                    .HasColumnName("id")
                    .HasConversion<SlotRequestIdConverter>()
                    .ValueGeneratedNever();

                requests.HasKey(r => r.Id);

                requests
                    .Property<SubmissionId>("submission_id")
                    .HasConversion<SubmissionIdConverter>()
                    .IsRequired();

                requests
                    .WithOwner()
                    .HasForeignKey("submission_id");

                requests
                    .Property(r => r.SlotId)
                    .HasColumnName("slot_id")
                    .HasConversion<SlotIdConverter>();

                requests
                    .Property(r => r.SessionType)
                    .HasColumnName("session_type");

                requests
                    .Property(r => r.Constraint)
                    .HasColumnName("constraint_text")
                    .HasConversion<SlotConstraintConverter>()
                    .HasMaxLength(SlotConstraint.MaxLength);

                requests
                    .Property(r => r.Rank)
                    .HasColumnName("rank")
                    .HasConversion<RankConverter>();
            });

        builder
            .Navigation(x => x.SlotRequests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
