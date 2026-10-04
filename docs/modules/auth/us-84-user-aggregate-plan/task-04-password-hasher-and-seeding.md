# Task 4 of 6: Password hasher port and first-Administrator seeding (TDD)

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Requires tasks 1 to 3 committed. Work on branch `84-user-aggregate-sign-in`.

**Files:**
- Rename + modify: `src\DrivingLessons.Application\Auth\IPasswordVerifier.cs` → `src\DrivingLessons.Application\Auth\IPasswordHasher.cs`
- Rename + modify: `src\DrivingLessons.Infrastructure\Auth\PasswordVerifier.cs` → `src\DrivingLessons.Infrastructure\Auth\IdentityPasswordHasher.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (the `IPasswordVerifier` registration)
- Modify: `src\DrivingLessons.Application\Auth\IAdminAccountGateway.cs` (`AdminAccount.PasswordHash` becomes a `PasswordHash`)
- Modify: `src\DrivingLessons.Infrastructure\Auth\AdminAccountGateway.cs` (builds the `PasswordHash`)
- Modify: `src\DrivingLessons.Application\Auth\LoginInteractor.cs` (constructor parameter type only)
- Create: `src\DrivingLessons.Application\Commands\SeedFirstAdministrator\SeedFirstAdministratorRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\SeedFirstAdministrator\SeedFirstAdministratorInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (register the interactor)
- Test: `tests\DrivingLessons.Application.Test\Auth\IdentityPasswordHasherTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\SeedFirstAdministratorInteractorTest.cs`

**Interfaces:**
- Consumes: from task 2, `User.Create(UserName, Email, PasswordHash, Role, Teacher?)`, `User.Name`, `SignInEmail`, `PasswordHash`, `Role`, `TeacherId`, and `IUserRepository.AnyExistAsync()` / `Add(User)`; from task 1, `UserName`, `PasswordHash`, `Role`. Existing `IUnitOfWork.CommitAsync()`, `Email.Of`.
- Produces:
  - `DrivingLessons.Application.Auth.IPasswordHasher` with `PasswordHash Hash(string password)` and `bool Verify(PasswordHash passwordHash, string providedPassword)`
  - `DrivingLessons.Infrastructure.Auth.IdentityPasswordHasher : IPasswordHasher`, registered as a singleton
  - `record SeedFirstAdministratorRequest(string Name, string Email, string Password)`
  - `SeedFirstAdministratorInteractor` with `Task ExecuteAsync(SeedFirstAdministratorRequest request)`, registered scoped. Task 5 calls it from `Program.cs`.

**Why:** #84 acceptance criterion 6, and README decision 3. The domain gets a `PasswordHash` from the port and never sees the plaintext. The interactor isn't called from `Program.cs` yet. `AdminSeeder` keeps owning start-up until task 5 removes `admin_users`. Wiring both now would put a config-hashed Administrator into `users` before task 5's data migration copies the real row.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IdentityPasswordHasherTest|FullyQualifiedName~SeedFirstAdministratorInteractorTest"
```

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Auth\IdentityPasswordHasherTest.cs`:

```csharp
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class IdentityPasswordHasherTest
{
    private const string Password = "Correct#Horse2026";

    private readonly IdentityPasswordHasher hasher = new();

    [TestMethod]
    public void Verifies_The_Password_It_Hashed()
    {
        //given
        var hash = hasher.Hash(Password);

        //when
        var verified = hasher.Verify(hash, Password);

        //then
        verified.ShouldBeTrue();
    }

    [TestMethod]
    public void Rejects_A_Wrong_Password()
    {
        //given
        var hash = hasher.Hash(Password);

        //when
        var verified = hasher.Verify(hash, "Wrong#Horse2026");

        //then
        verified.ShouldBeFalse();
    }

    [TestMethod]
    public void Hash_Is_Not_The_Plain_Password()
    {
        //when
        var hash = hasher.Hash(Password);

        //then
        hash.Value.ShouldNotContain(Password);
    }

    [TestMethod]
    public void Verifies_A_Hash_Written_By_The_Old_Admin_Seeder()
    {
        //given
        var oldSeederHasher = new PasswordHasher<LegacyAdmin>();
        var oldAdmin = new LegacyAdmin();
        var storedHash = oldSeederHasher.HashPassword(oldAdmin, Password);
        var passwordHash = PasswordHash.Of(storedHash);

        //when
        var verified = hasher.Verify(passwordHash, Password);

        //then
        verified.ShouldBeTrue();
    }

    private sealed class LegacyAdmin;
}
```

`LegacyAdmin` stands in for the removed `AdminUser`: the old `AdminSeeder` hashed with `PasswordHasher<AdminUser>`, a different type argument from the one `IdentityPasswordHasher` uses. The test proves the stored hash doesn't depend on it (README Review Focus 4).

Create `tests\DrivingLessons.Application.Test\Commands\SeedFirstAdministratorInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.SeedFirstAdministrator;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class SeedFirstAdministratorInteractorTest
{
    private const string ConfiguredName = "School Owner";
    private const string ConfiguredEmail = "owner@school.example";
    private const string ConfiguredPassword = "Configured#2026";

    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private SeedFirstAdministratorInteractor interactor = null!;
    private SeedFirstAdministratorRequest request = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new SeedFirstAdministratorInteractor(repository, passwordHasher, unitOfWork);
        request = new SeedFirstAdministratorRequest(ConfiguredName, ConfiguredEmail, ConfiguredPassword);
    }

    [TestMethod]
    public async Task Creates_The_First_Administrator_When_No_Users_Exist()
    {
        //given
        var hashed = PasswordHash.Of("hashed-configured-password");
        User? added = null;

        A.CallTo(() => repository.AnyExistAsync()).Returns(false);
        A.CallTo(() => passwordHasher.Hash(ConfiguredPassword)).Returns(hashed);
        A.CallTo(() => repository.Add(A<User>._)).Invokes((User user) => added = user);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Name.ShouldBe(UserName.Of(ConfiguredName));
        added.SignInEmail.ShouldBe(Email.Of(ConfiguredEmail));
        added.PasswordHash.ShouldBe(hashed);
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBeNull();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Leaves_Existing_Users_Untouched()
    {
        //given
        A.CallTo(() => repository.AnyExistAsync()).Returns(true);

        //when
        await interactor.ExecuteAsync(request);

        //then
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

`Leaves_Existing_Users_Untouched` is the "restart with a different configured password" case. When any User exists, the configured values are never hashed or written (README Review Focus 5).

- [ ] **Step 2: Run the tests to verify they fail**

Run the test command above.
Expected: build FAILS with `CS0246` for `IdentityPasswordHasher`, `IPasswordHasher`, `SeedFirstAdministratorInteractor` and `SeedFirstAdministratorRequest`.

- [ ] **Step 3: Rename and extend the hasher port**

```bash
git mv src/DrivingLessons.Application/Auth/IPasswordVerifier.cs src/DrivingLessons.Application/Auth/IPasswordHasher.cs
git mv src/DrivingLessons.Infrastructure/Auth/PasswordVerifier.cs src/DrivingLessons.Infrastructure/Auth/IdentityPasswordHasher.cs
```

Replace the content of `src\DrivingLessons.Application\Auth\IPasswordHasher.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);

    bool Verify(PasswordHash passwordHash, string providedPassword);
}
```

Replace the content of `src\DrivingLessons.Infrastructure\Auth\IdentityPasswordHasher.cs`:

```csharp
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
```

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, replace

```csharp
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
```

with

```csharp
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
```

- [ ] **Step 4: Move the remaining login code onto the port**

`src\DrivingLessons.Application\Auth\IAdminAccountGateway.cs` becomes:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public sealed record AdminAccount(Guid Id, string Email, PasswordHash PasswordHash);

public interface IAdminAccountGateway
{
    Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
```

In `src\DrivingLessons.Infrastructure\Auth\AdminAccountGateway.cs`, add `using DrivingLessons.Domain.Values;` and replace the `return admin is null ? ... ;` statement with:

```csharp
        if (admin is null)
        {
            return null;
        }

        var passwordHash = PasswordHash.Of(admin.PasswordHash);

        return new AdminAccount(admin.Id, admin.Email, passwordHash);
```

In `src\DrivingLessons.Application\Auth\LoginInteractor.cs`, change the constructor parameter `IPasswordVerifier passwords` to `IPasswordHasher passwords`. Nothing else changes: `passwords.Verify(account.PasswordHash, ...)` now receives a `PasswordHash`. Task 5 deletes the gateway and rewrites this interactor. This step only keeps the build green until then.

Run: `dotnet build`
Expected: build succeeds. `grep -rn "IPasswordVerifier\|PasswordVerifier" src tests` prints nothing.

- [ ] **Step 5: Write the seeding interactor**

`src\DrivingLessons.Application\Commands\SeedFirstAdministrator\SeedFirstAdministratorRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.SeedFirstAdministrator;

public record SeedFirstAdministratorRequest(string Name, string Email, string Password);
```

`src\DrivingLessons.Application\Commands\SeedFirstAdministrator\SeedFirstAdministratorInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.SeedFirstAdministrator;

public class SeedFirstAdministratorInteractor
{
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public SeedFirstAdministratorInteractor(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(SeedFirstAdministratorRequest request)
    {
        var usersExist = await repository.AnyExistAsync();

        if (usersExist)
        {
            return;
        }

        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.Email);
        var passwordHash = passwordHasher.Hash(request.Password);
        var administrator = User.Create(name, signInEmail, passwordHash, Role.Administrator, teacher: null);

        repository.Add(administrator);

        await unitOfWork.CommitAsync();
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.Commands.SeedFirstAdministrator;` in alphabetical position among the `Commands` usings (after `ReviseSubmission`), and add after `services.AddScoped<LoginInteractor>();`:

```csharp
        services.AddScoped<SeedFirstAdministratorInteractor>();
```

- [ ] **Step 6: Run the tests to verify they pass**

Run the test command above.
Expected: PASS, 6 tests.

Then the full build and both suites:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build succeeds with no new warnings, every test PASS.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Auth src/DrivingLessons.Infrastructure/Auth src/DrivingLessons.Infrastructure/DependencyInjection.cs src/DrivingLessons.Application/Commands/SeedFirstAdministrator src/DrivingLessons.Application/DependencyInjection.cs tests/DrivingLessons.Application.Test/Auth/IdentityPasswordHasherTest.cs tests/DrivingLessons.Application.Test/Commands/SeedFirstAdministratorInteractorTest.cs
git commit -m "feat(application): hash through IPasswordHasher and seed the first Administrator only when no Users exist (#84)"
```

End the commit message with the attribution trailer from the session's instructions.
