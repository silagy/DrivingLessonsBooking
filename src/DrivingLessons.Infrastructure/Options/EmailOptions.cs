namespace DrivingLessons.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }
}
