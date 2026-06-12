# Task 4 of 11: Application layer — auth ports + login interactor

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–3 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Architecture:** Modular monolith, DDD, Onion. Admin auth is **infrastructure-level** — there is deliberately no Admin domain aggregate. Root namespace: `DrivingLessons`.

**Onion references:** `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.

**User decisions (locked):** JWT bearer · credentials seeded from config · no tests this slice.

---

**Files:**
- Create: `src/DrivingLessons.Application/Auth/LoginCommand.cs`, `LoginResult.cs`, `IAdminAccountGateway.cs`, `IPasswordVerifier.cs`, `IJwtTokenGenerator.cs`, `LoginInteractor.cs`
- Create: `src/DrivingLessons.Application/DependencyInjection.cs`

Note: no value objects here on purpose — VOs belong to aggregates (Teacher/Student slices). Auth is infrastructure-level and pre-aggregate.

- [ ] **Step 1: `LoginCommand.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record LoginCommand(string Email, string Password);
```

- [ ] **Step 2: `LoginResult.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
```

- [ ] **Step 3: `IAdminAccountGateway.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record AdminAccount(Guid Id, string Email, string PasswordHash);

public interface IAdminAccountGateway
{
    Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: `IPasswordVerifier.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public interface IPasswordVerifier
{
    bool Verify(string passwordHash, string providedPassword);
}
```

- [ ] **Step 5: `IJwtTokenGenerator.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    IssuedToken Generate(Guid adminId, string email);
}
```

- [ ] **Step 6: `LoginInteractor.cs`**

```csharp
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

        // Single failure path: never reveal whether the email or the password was wrong.
        if (account is null || !passwords.Verify(account.PasswordHash, command.Password ?? string.Empty))
        {
            throw new AuthenticationFailedException();
        }

        var issued = tokens.Generate(account.Id, account.Email);
        return new LoginResult(issued.AccessToken, issued.ExpiresAtUtc);
    }
}
```

- [ ] **Step 7: `DependencyInjection.cs`**

```csharp
using DrivingLessons.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginInteractor>();
        return services;
    }
}
```

- [ ] **Step 8: Build + commit**

Run: `dotnet build` — expected: success.

```bash
git add src
git commit -m "feat(app): login interactor with auth ports"
```

---

**Next:** [task-05-infrastructure.md](task-05-infrastructure.md)
