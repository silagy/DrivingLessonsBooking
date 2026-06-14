namespace DrivingLessons.Infrastructure.Auth;

public sealed class AdminUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
}
