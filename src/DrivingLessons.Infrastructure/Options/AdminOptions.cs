using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
