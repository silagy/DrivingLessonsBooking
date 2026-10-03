# Task 5 of 6: Sign in through Users, retire the admin record (TDD)

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Requires tasks 1 to 4 committed. Work on branch `84-user-aggregate-sign-in`.

**Files:**
- Modify: `src\DrivingLessons.Application\Auth\IJwtTokenGenerator.cs`
- Modify: `src\DrivingLessons.Application\Auth\LoginInteractor.cs`
- Delete: `src\DrivingLessons.Application\Auth\IAdminAccountGateway.cs`
- Create: `src\DrivingLessons.Infrastructure\Auth\AuthClaims.cs`
- Modify: `src\DrivingLessons.Infrastructure\Auth\JwtTokenGenerator.cs`
- Delete: `src\DrivingLessons.Infrastructure\Auth\AdminAccountGateway.cs`, `AdminSeeder.cs`, `AdminUser.cs`
- Delete: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\AdminUserConfiguration.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (drop `AdminUsers` and the `Infrastructure.Auth` using)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (drop the gateway registration)
- Modify: `src\DrivingLessons.Infrastructure\Options\AdminOptions.cs` (add `Name`)
- Modify: `src\DrivingLessons.Presentation.Web\Program.cs` (seed through the interactor)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\AuthController.cs` (call `ExecuteAsync`)
- Create (generated, then edited): `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_MoveAdminToUsers.cs` + `.Designer.cs`; Modify (generated): `DrivingLessonsDbContextModelSnapshot.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\LoginInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\JwtTokenGeneratorTest.cs`

**Interfaces:**
- Consumes: from task 2, `User` (`Id`, `SignInEmail`, `PasswordHash`, `Role`, `TeacherId`, `SecurityStamp`, `IsDeleted`, `Delete()`) and `IUserRepository.GetByEmailAsync(Email)`; from task 3, the `users` columns `id, name, email, password_hash, role, teacher_id, security_stamp, is_deleted`; from task 4, `IPasswordHasher.Verify(PasswordHash, string)`, `SeedFirstAdministratorInteractor.ExecuteAsync(SeedFirstAdministratorRequest)`, `SeedFirstAdministratorRequest(string Name, string Email, string Password)`.
- Produces:
  - `IJwtTokenGenerator.Generate(User user)` returning `IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc)`
  - `LoginInteractor.ExecuteAsync(LoginCommand command)` returning `LoginResult` (was `Handle(command, cancellationToken)`)
  - `DrivingLessons.Infrastructure.Auth.AuthClaims` constants: `Role = "role"`, `TeacherId = "teacher_id"`, `SecurityStamp = "security_stamp"`, `AdministratorRole = "administrator"`, `TeacherRole = "teacher"`. The Roles and policies slice of #82 reads these.
  - Token claims: `sub` (User id), `email` (sign-in email, still read by the client's `AuthService.email`), `role`, `security_stamp`, and `teacher_id` only when the User has a linked Teacher. No `role=admin`.
  - `AdminOptions.Name` (default `"Administrator"`)

**Why:** #84 acceptance criteria 3, 4, 5 and the start-up half of 6. The data migration copies the admin row in the same migration that drops `admin_users` (README decision 5). A malformed email is the same 401 as a wrong password (README decision 4).

**Don't run the API or `dotnet ef database update` in this task.** Task 6 records the admin row before `MoveAdminToUsers` is applied, so it can prove the hash moved unchanged.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~LoginInteractorTest|FullyQualifiedName~JwtTokenGeneratorTest"
```

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Auth\LoginInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class LoginInteractorTest
{
    private const string SignInEmail = "owner@school.example";
    private const string CorrectPassword = "Correct#Horse2026";
    private const string IssuedAccessToken = "issued-access-token";

    private IUserRepository users = null!;
    private IPasswordHasher passwords = null!;
    private IJwtTokenGenerator tokens = null!;
    private LoginInteractor interactor = null!;
    private User user = null!;
    private DateTimeOffset expiresAtUtc;

    [TestInitialize]
    public void Init()
    {
        users = A.Fake<IUserRepository>();
        passwords = A.Fake<IPasswordHasher>();
        tokens = A.Fake<IJwtTokenGenerator>();
        interactor = new LoginInteractor(users, passwords, tokens);
        user = User.Create(
            UserName.Of("School Owner"),
            Email.Of(SignInEmail),
            PasswordHash.Of("stored-hash"),
            Role.Administrator,
            null);
        expiresAtUtc = new DateTimeOffset(2026, 10, 3, 22, 0, 0, TimeSpan.Zero);

        A.CallTo(() => users.GetByEmailAsync(A<Email>._)).Returns((User?)null);
        A.CallTo(() => users.GetByEmailAsync(Email.Of(SignInEmail))).Returns(user);
        A.CallTo(() => passwords.Verify(user.PasswordHash, CorrectPassword)).Returns(true);
        A.CallTo(() => tokens.Generate(user)).Returns(new IssuedToken(IssuedAccessToken, expiresAtUtc));
    }

    [TestMethod]
    public async Task Signs_In_A_User_With_The_Right_Password()
    {
        //given
        var command = new LoginCommand(SignInEmail, CorrectPassword);

        //when
        var result = await interactor.ExecuteAsync(command);

        //then
        result.AccessToken.ShouldBe(IssuedAccessToken);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
    }

    [TestMethod]
    public async Task Sign_In_Email_Ignores_Case_And_Surrounding_Spaces()
    {
        //given
        var command = new LoginCommand("  Owner@School.Example ", CorrectPassword);

        //when
        var result = await interactor.ExecuteAsync(command);

        //then
        result.AccessToken.ShouldBe(IssuedAccessToken);
    }

    [TestMethod]
    public async Task Wrong_Password_Is_Rejected()
    {
        //given
        var command = new LoginCommand(SignInEmail, "Wrong#Horse2026");

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Missing_Password_Is_Rejected()
    {
        //given
        var command = new LoginCommand(SignInEmail, null!);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Unknown_Email_Is_Rejected()
    {
        //given
        var command = new LoginCommand("stranger@school.example", CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_Like_A_Wrong_Password()
    {
        //given
        user.Delete();
        var command = new LoginCommand(SignInEmail, CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
        A.CallTo(() => tokens.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("not-an-email")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Malformed_Email_Is_Rejected_Like_A_Wrong_Password(string? email)
    {
        //given
        var command = new LoginCommand(email!, CorrectPassword);

        //when
        var act = () => interactor.ExecuteAsync(command);

        //then
        await Should.ThrowAsync<AuthenticationFailedException>(act);
        A.CallTo(() => users.GetByEmailAsync(A<Email>._)).MustNotHaveHappened();
    }
}
```

FakeItEasy uses the most recently configured matching rule, so the specific `GetByEmailAsync(Email.Of(SignInEmail))` rule wins over the catch-all `null` rule above it. `Should.ThrowAsync<AuthenticationFailedException>` fails if the interactor lets `EmailMustBeValidException` escape, which is the 409 leak from README Review Focus 1.

Create `tests\DrivingLessons.Application.Test\Auth\JwtTokenGeneratorTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class JwtTokenGeneratorTest
{
    private const string SignInEmail = "owner@school.example";

    private readonly JwtTokenGenerator generator = new(Microsoft.Extensions.Options.Options.Create(new JwtOptions
    {
        Issuer = "DrivingLessons",
        Audience = "DrivingLessons",
        SigningKey = "test-only-signing-key-at-least-32-characters-long!",
        ExpiryHours = 12
    }));

    [TestMethod]
    public void Token_Carries_The_User_Id_Email_Role_And_Security_Stamp()
    {
        //given
        var user = Administrator();

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.Value.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(SignInEmail);
        token.GetClaim("role").Value.ShouldBe("administrator");
        token.GetClaim("security_stamp").Value.ShouldBe(user.SecurityStamp.Value);
    }

    [TestMethod]
    public void Administrator_Without_A_Teacher_Has_No_Teacher_Claim()
    {
        //given
        var user = Administrator();

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.TryGetClaim("teacher_id", out _).ShouldBeFalse();
    }

    [TestMethod]
    public void Teacher_Token_Carries_The_Teacher_Role_And_The_Linked_Teacher_Id()
    {
        //given
        var teacher = Teacher.Create(TeacherName.Of("Teacher Cohen"), Email.Of("cohen@school.example"));
        var user = User.Create(
            UserName.Of("Teacher Cohen"),
            Email.Of("cohen.login@school.example"),
            PasswordHash.Of("stored-hash"),
            Role.Teacher,
            teacher);

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.GetClaim("role").Value.ShouldBe("teacher");
        token.GetClaim("teacher_id").Value.ShouldBe(teacher.Id.Value.ToString());
    }

    private static User Administrator()
    {
        return User.Create(
            UserName.Of("School Owner"),
            Email.Of(SignInEmail),
            PasswordHash.Of("stored-hash"),
            Role.Administrator,
            null);
    }
}
```

`Options.Create` is fully qualified, as in `SmtpEmailSenderTest`, because `DrivingLessons.Infrastructure.Options` is also imported. The claim names and values are string literals on purpose. They pin the wire contract the client and the next slice depend on, so a typo in `AuthClaims` fails here.

- [ ] **Step 2: Run the tests to verify they fail**

Run the test command above.
Expected: build FAILS. `LoginInteractor` has no `ExecuteAsync` and its constructor takes `IAdminAccountGateway`. `IJwtTokenGenerator.Generate` takes `(Guid, string)`, not a `User`.

- [ ] **Step 3: Change the token port and generator**

`src\DrivingLessons.Application\Auth\IJwtTokenGenerator.cs`:

```csharp
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Application.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    IssuedToken Generate(User user);
}
```

Create `src\DrivingLessons.Infrastructure\Auth\AuthClaims.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Auth;

public static class AuthClaims
{
    public const string Role = "role";
    public const string TeacherId = "teacher_id";
    public const string SecurityStamp = "security_stamp";
    public const string AdministratorRole = "administrator";
    public const string TeacherRole = "teacher";
}
```

`src\DrivingLessons.Infrastructure\Auth\JwtTokenGenerator.cs`:

```csharp
using System.Text;
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public IssuedToken Generate(User user)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(jwt.ExpiryHours);
        var claims = ClaimsOf(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return new IssuedToken(token, expiresAt);
    }

    private static Dictionary<string, object> ClaimsOf(User user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.Value.ToString(),
            [JwtRegisteredClaimNames.Email] = user.SignInEmail.Value,
            [AuthClaims.Role] = RoleClaimOf(user.Role),
            [AuthClaims.SecurityStamp] = user.SecurityStamp.Value
        };

        var teacherId = user.TeacherId;

        if (teacherId is not null)
        {
            claims[AuthClaims.TeacherId] = teacherId.Value.ToString();
        }

        return claims;
    }

    private static string RoleClaimOf(Role role)
    {
        return role switch
        {
            Role.Administrator => AuthClaims.AdministratorRole,
            Role.Teacher => AuthClaims.TeacherRole,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}
```

The descriptor block is unchanged from today except `Claims = claims`.

- [ ] **Step 4: Rewrite the login interactor and its endpoint**

`src\DrivingLessons.Application\Auth\LoginInteractor.cs`:

```csharp
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
```

In `src\DrivingLessons.Presentation.Web\Controllers\AuthController.cs`, replace the action with:

```csharp
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResult> Login([FromBody] LoginCommand command) =>
        login.ExecuteAsync(command);
```

- [ ] **Step 5: Remove the admin record and seed through the interactor**

```bash
git rm src/DrivingLessons.Application/Auth/IAdminAccountGateway.cs src/DrivingLessons.Infrastructure/Auth/AdminAccountGateway.cs src/DrivingLessons.Infrastructure/Auth/AdminSeeder.cs src/DrivingLessons.Infrastructure/Auth/AdminUser.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/AdminUserConfiguration.cs
```

In `DrivingLessonsDbContext.cs`, delete the line `public DbSet<AdminUser> AdminUsers => Set<AdminUser>();` (and the blank line after it) and the `using DrivingLessons.Infrastructure.Auth;` directive.

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, delete `services.AddScoped<IAdminAccountGateway, AdminAccountGateway>();`. Keep `using DrivingLessons.Infrastructure.Auth;`, because `IdentityPasswordHasher` and `JwtTokenGenerator` still live there.

`src\DrivingLessons.Infrastructure\Options\AdminOptions.cs` becomes:

```csharp
using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    private const string DefaultName = "Administrator";

    [Required]
    public string Name { get; init; } = DefaultName;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
```

In `src\DrivingLessons.Presentation.Web\Program.cs`:
- Replace `using DrivingLessons.Infrastructure.Auth;` with `using DrivingLessons.Application.Commands.SeedFirstAdministrator;`, placed after `using DrivingLessons.Application;`.
- Replace the start-up scope block with:

```csharp
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DrivingLessonsDbContext>();
    await db.Database.MigrateAsync();
    var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value;
    var seedFirstAdministrator = scope.ServiceProvider.GetRequiredService<SeedFirstAdministratorInteractor>();
    var seedRequest = new SeedFirstAdministratorRequest(adminOptions.Name, adminOptions.Email, adminOptions.Password);
    await seedFirstAdministrator.ExecuteAsync(seedRequest);
}
```

Run: `dotnet build`
Expected: build succeeds with no new warnings. `grep -rn "AdminUser\|AdminAccount\|AdminSeeder" src tests --include=*.cs | grep -v Migrations` prints nothing. (Old migration designer files mention `AdminUser` inside strings, which is expected.)

- [ ] **Step 6: Run the tests to verify they pass**

Run the test command above.
Expected: PASS, 12 test cases counting each `DataRow` (`LoginInteractorTest` 9, `JwtTokenGeneratorTest` 3).

- [ ] **Step 7: Generate the data migration**

```bash
dotnet ef migrations add MoveAdminToUsers --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web --output-dir EntityFramework\Migrations
```

The generated `Up` is a single `migrationBuilder.DropTable(name: "admin_users");`. The generated `Down` re-creates `admin_users` (`Id`, `Email`, `PasswordHash`) and its unique index `IX_admin_users_Email`. If it contains anything else, stop: the model drifted somewhere other than the admin record.

Edit `{timestamp}_MoveAdminToUsers.cs`. Insert the copy **before** the drop in `Up`:

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO users (id, name, email, password_hash, role, teacher_id, security_stamp, is_deleted)
                SELECT a."Id", 'Administrator', a."Email", a."PasswordHash", 10, NULL,
                       replace(gen_random_uuid()::text, '-', ''), false
                FROM admin_users a
                WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.email = a."Email");
                """);

            migrationBuilder.DropTable(
                name: "admin_users");
        }
```

Then add the copy back at the **end** of `Down`, after the generated `CreateTable` and `CreateIndex`:

```csharp
            migrationBuilder.Sql(
                """
                INSERT INTO admin_users ("Id", "Email", "PasswordHash")
                SELECT id, email, password_hash
                FROM users
                WHERE role = 10 AND NOT is_deleted;
                """);
```

Notes for the reviewer:
- `admin_users` was created with PascalCase columns, so they must be double-quoted. `users` is snake_case.
- `10` is `Role.Administrator`. The admin's id is kept, so a token issued before the deploy still names the same person in `sub`. The generated stamp has the same 32-hex-character format as `SecurityStamp.New()`.
- `WHERE NOT EXISTS` keeps the unique `email` index from failing on a database whose `users` table already holds that address.
- `Down` copies Administrators back and leaves `users` alone. Rolling back `AddUsers` after it drops the table.

Then confirm nothing is pending:

```bash
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 8: Run every test**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build succeeds with no new warnings, every test PASS (including `SourceTextTest` and `ApiExceptionFilterTest`).

- [ ] **Step 9: Commit**

```bash
git add -A src/DrivingLessons.Application/Auth src/DrivingLessons.Infrastructure src/DrivingLessons.Presentation.Web tests/DrivingLessons.Application.Test/Auth
git commit -m "feat(auth): sign in through the User aggregate and move the admin into users (#84)"
```

End the commit message with the attribution trailer from the session's instructions.
