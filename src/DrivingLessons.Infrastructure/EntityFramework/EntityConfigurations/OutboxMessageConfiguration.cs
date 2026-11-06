using DrivingLessons.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private const int EventNameMaxLength = 200;

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(EventNameMaxLength);

        builder
            .Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        builder
            .Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamptz");
    }
}
