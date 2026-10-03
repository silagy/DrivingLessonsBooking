using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public sealed class LoginInteractor(
    IUserRepository users,
    IPasswordHasher passwords,
    IJwtTokenGenerator tokens)
{
    public async Task<LoginResult> ExecuteAsync(LoginCommand command)
    {
        var signInEmail = SignInEmailOf(command.Email) ?? throw new AuthenticationFailedException();
        var user = await users.GetByEmailAsync(signInEmail);
        var password = command.Password ?? string.Empty;

        if (user is null
            || user.IsDeleted
            || !passwords.Verify(user.PasswordHash, password))
        {
            throw new AuthenticationFailedException();
        }

        var issued = tokens.Generate(user);

        return new LoginResult(issued.AccessToken, issued.ExpiresAtUtc);
    }

    private static Email? SignInEmailOf(string? rawEmail)
    {
        try
        {
            return Email.Of(rawEmail ?? string.Empty);
        }
        catch (EmailMustBeValidException)
        {
            return null;
        }
    }
}
