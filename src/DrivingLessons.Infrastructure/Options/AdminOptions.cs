using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    private const string DefaultName = "Administrator";

    [Required]
    public string Name { get; init; } = DefaultName;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
