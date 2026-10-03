using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public sealed record AdminAccount(Guid Id, string Email, PasswordHash PasswordHash);

public interface IAdminAccountGateway
{
    Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
