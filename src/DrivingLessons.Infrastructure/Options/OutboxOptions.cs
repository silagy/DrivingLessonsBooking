using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(1, 3600)]
    public int PollSeconds { get; init; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 20;

    [Range(1, 50)]
    public int MaxAttempts { get; init; } = 8;

    [Range(30, 3600)]
    public int LeaseSeconds { get; init; } = 300;

    [Range(1, 365)]
    public int RetentionDays { get; init; } = 30;
}
