namespace DrivingLessons.Application.Auth;

public sealed record AdminAccount(Guid Id, string Email, string PasswordHash);

public interface IAdminAccountGateway
{
    Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
