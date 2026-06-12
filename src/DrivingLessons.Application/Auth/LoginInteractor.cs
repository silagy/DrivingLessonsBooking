using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Auth;

public sealed class LoginInteractor(
    IAdminAccountGateway accounts,
    IPasswordVerifier passwords,
    IJwtTokenGenerator tokens)
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var account = await accounts.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (account is null || !passwords.Verify(account.PasswordHash, command.Password ?? string.Empty))
        {
            throw new AuthenticationFailedException();
        }

        var issued = tokens.Generate(account.Id, account.Email);
        return new LoginResult(issued.AccessToken, issued.ExpiresAtUtc);
    }
}
