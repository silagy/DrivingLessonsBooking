using DrivingLessons.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class OutboxDeliveryConfiguration : IEntityTypeConfiguration<OutboxDelivery>
{
    private const int ConsumerMaxLength = 200;
    private const int StateMaxLength = 20;

    public void Configure(EntityTypeBuilder<OutboxDelivery> builder)
    {
        builder.ToTable("outbox_deliveries");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.MessageId)
            .HasColumnName("message_id");

        builder
            .HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.Consumer)
            .HasColumnName("consumer")
            .HasMaxLength(ConsumerMaxLength);

        builder
            .Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(StateMaxLength);

        builder
            .Property(x => x.Attempts)
            .HasColumnName("attempts");

        builder
            .Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.LockedUntilUtc)
            .HasColumnName("locked_until_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(OutboxDelivery.LastErrorMaxLength);

        builder
            .Property(x => x.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .HasColumnType("timestamptz");

        builder
            .HasIndex(x => new { x.MessageId, x.Consumer })
            .IsUnique();

        builder.HasIndex(x => new { x.State, x.NextAttemptAtUtc });
    }
}
