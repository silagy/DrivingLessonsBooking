using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);

    bool Verify(PasswordHash passwordHash, string providedPassword);
}
