using DrivingLessons.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Dummy = new();

    public bool Verify(string passwordHash, string providedPassword) =>
        Hasher.VerifyHashedPassword(Dummy, passwordHash, providedPassword)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
