namespace DrivingLessons.Application.Auth;

public interface IPasswordVerifier
{
    bool Verify(string passwordHash, string providedPassword);
}
