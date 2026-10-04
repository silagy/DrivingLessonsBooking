using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using Microsoft.AspNetCore.Identity;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Dummy = new();

    public PasswordHash Hash(string password)
    {
        var hashed = Hasher.HashPassword(Dummy, password);

        return PasswordHash.Of(hashed);
    }

    public bool Verify(PasswordHash passwordHash, string providedPassword)
    {
        var result = Hasher.VerifyHashedPassword(Dummy, passwordHash.Value, providedPassword);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
