# Task 1 of 8: Current-user port carries Role and linked Teacher (backend)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Work on branch `90-teacher-data-scoping`; the plan commit comes first. Read README decisions 1-2 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Auth\ICurrentUser.cs`
- Create: `src\DrivingLessons.Application\Auth\CurrentUserExtension.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs` (extended)
- Test: `tests\DrivingLessons.Application.Test\Auth\CurrentUserExtensionTest.cs` (**new**)

`HttpCurrentUser` is the only implementation of `ICurrentUser` (registered scoped in `Program.cs:31`, unchanged). The three tests that fake the port (`ChangeMyPasswordInteractorTest`, `ChangeUserRoleInteractorTest`, `DeleteUserInteractorTest`) use `A.Fake<ICurrentUser>()`, which picks up the new properties on its own: no change there.

**Interfaces:**
- Consumes:
  - `DrivingLessons.Infrastructure.Auth.AuthClaims`: `Role = "role"`, `TeacherId = "teacher_id"`, `AdministratorRole = "administrator"`, `TeacherRole = "teacher"`. `JwtTokenGenerator` writes `role` for every User and `teacher_id` whenever the User is linked, whatever the Role. JWT bearer runs with `MapInboundClaims = false`, so the claim types arrive unchanged.
  - `DrivingLessons.Domain.Values.Role` (`Administrator = 10`, `Teacher = 20`), `TeacherId.Of(Guid)`, `UserId.Of(Guid)`.
- Produces (tasks 2-4 rely on these exact names):
  - `public interface ICurrentUser { UserId Id { get; } Role Role { get; } TeacherId? TeacherId { get; } }` in `DrivingLessons.Application.Auth`.
  - `public static class CurrentUserExtension { public static bool MayReach(this ICurrentUser user, TeacherId teacherId) }` in `DrivingLessons.Application.Auth`: true for an Administrator (linked or not); for a Teacher, true only when `user.TeacherId == teacherId` (false when the Teacher has no linked Teacher).
  - `HttpCurrentUser` throws `InvalidOperationException` for: no `sub` (existing), an unknown or missing `role`, a Teacher-role token without `teacher_id`, and a malformed `teacher_id` (not a Guid, or the empty Guid).
  - Tests fake the port with FakeItEasy: `A.CallTo(() => currentUser.Role).Returns(Role.Teacher); A.CallTo(() => currentUser.TeacherId).Returns(teacher.Id);`. `MayReach` is an extension method, so it is never faked: it runs on the faked properties.

**Why:** AC 1-4 need one trusted answer to "who is asking, and which Teacher is theirs". Review Focus 3 (a Teacher-role token without `teacher_id` reaches nothing, not everything) and 4 (a linked Administrator is not narrowed) are pinned here.

- [ ] **Step 1: Write the failing `HttpCurrentUser` tests**

Replace `tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs` with (the two existing tests are kept as they are):

```csharp
using System.Security.Claims;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class HttpCurrentUserTest
{
    [TestMethod]
    public void Reads_The_Signed_In_User_From_The_Subject_Claim()
    {
        //given
        var id = Guid.NewGuid();
        var accessor = AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        //when
        var currentUserId = new HttpCurrentUser(accessor).Id;

        //then
        currentUserId.ShouldBe(UserId.Of(id));
    }

    [TestMethod]
    public void Request_Without_A_Signed_In_User_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith();

        //when
        var act = () => new HttpCurrentUser(accessor).Id;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Reads_An_Administrator_From_The_Role_Claim()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.AdministratorRole));

        //when
        var role = new HttpCurrentUser(accessor).Role;

        //then
        role.ShouldBe(Role.Administrator);
    }

    [TestMethod]
    public void Reads_A_Teacher_And_The_Linked_Teacher()
    {
        //given
        var teacherId = Guid.NewGuid();
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, AuthClaims.TeacherRole),
            new Claim(AuthClaims.TeacherId, teacherId.ToString()));
        var currentUser = new HttpCurrentUser(accessor);

        //when
        var role = currentUser.Role;
        var linkedTeacherId = currentUser.TeacherId;

        //then
        role.ShouldBe(Role.Teacher);
        linkedTeacherId.ShouldBe(TeacherId.Of(teacherId));
    }

    [TestMethod]
    public void Reads_The_Teacher_Linked_To_An_Administrator()
    {
        //given
        var teacherId = Guid.NewGuid();
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, AuthClaims.AdministratorRole),
            new Claim(AuthClaims.TeacherId, teacherId.ToString()));

        //when
        var linkedTeacherId = new HttpCurrentUser(accessor).TeacherId;

        //then
        linkedTeacherId.ShouldBe(TeacherId.Of(teacherId));
    }

    [TestMethod]
    public void Unlinked_Administrator_Has_No_Linked_Teacher()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.AdministratorRole));

        //when
        var linkedTeacherId = new HttpCurrentUser(accessor).TeacherId;

        //then
        linkedTeacherId.ShouldBeNull();
    }

    [TestMethod]
    [DataRow("student")]
    [DataRow("Teacher")]
    public void Unknown_Role_Fails_Loudly(string role)
    {
        //given
        var currentUser = new HttpCurrentUser(AccessorWith(new Claim(AuthClaims.Role, role)));

        //when
        var act = () =>
        {
            _ = currentUser.Role;
        };

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Token_Without_A_Role_Fails_Loudly()
    {
        //given
        var currentUser = new HttpCurrentUser(AccessorWith());

        //when
        var act = () =>
        {
            _ = currentUser.Role;
        };

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Teacher_Without_A_Linked_Teacher_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.TeacherRole));

        //when
        var act = () => new HttpCurrentUser(accessor).TeacherId;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    [DataRow(AuthClaims.TeacherRole, "not-a-guid")]
    [DataRow(AuthClaims.TeacherRole, "00000000-0000-0000-0000-000000000000")]
    [DataRow(AuthClaims.AdministratorRole, "not-a-guid")]
    public void Malformed_Teacher_Id_Fails_Loudly(string role, string teacherId)
    {
        //given
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, role),
            new Claim(AuthClaims.TeacherId, teacherId));

        //when
        var act = () => new HttpCurrentUser(accessor).TeacherId;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    private static HttpContextAccessor AccessorWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
```

`Role` is a value type, so the `Role` cases use a block lambda (`Action`): Shouldly's `Func<object?>` overload doesn't accept a `Func<Role>`. `Id` and `TeacherId` are records, so the expression lambdas work as in the existing test. `"Teacher"` proves the role claim is matched exactly as `JwtTokenGenerator` writes it.

- [ ] **Step 2: Write the failing `CurrentUserExtension` tests**

Create `tests\DrivingLessons.Application.Test\Auth\CurrentUserExtensionTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class CurrentUserExtensionTest
{
    private ICurrentUser currentUser = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        currentUser = A.Fake<ICurrentUser>();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
    }

    [TestMethod]
    public void Administrator_Reaches_Any_Teacher()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Linked_Administrator_Reaches_Any_Teacher()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Teacher_Reaches_Own_Teacher()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);
        var sameTeacherId = TeacherId.Of(ownTeacherId.Value);

        //when
        var mayReach = currentUser.MayReach(sameTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Teacher_Does_Not_Reach_Another_Teacher()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeFalse();
    }

    [TestMethod]
    public void Teacher_Without_Linked_Teacher_Reaches_Nothing()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var mayReach = currentUser.MayReach(ownTeacherId);

        //then
        mayReach.ShouldBeFalse();
    }

    private void SignedInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
```

`Teacher_Reaches_Own_Teacher` passes a fresh `TeacherId` with the same value, so the rule is pinned to value equality (as it is in tasks 2-3, where the id comes from a route or a loaded Week Schedule), not to reference equality.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~HttpCurrentUserTest|FullyQualifiedName~CurrentUserExtensionTest"`

Expected: build FAILS with `CS1061: 'HttpCurrentUser' does not contain a definition for 'Role'` (and `'TeacherId'`), `CS1061: 'ICurrentUser' does not contain a definition for 'Role'` (and `'TeacherId'`), and `CS1061: 'ICurrentUser' does not contain a definition for 'MayReach'`.

- [ ] **Step 4: Grow the port and add the scoping rule**

Replace `src\DrivingLessons.Application\Auth\ICurrentUser.cs` with:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public interface ICurrentUser
{
    UserId Id { get; }

    Role Role { get; }

    TeacherId? TeacherId { get; }
}
```

Create `src\DrivingLessons.Application\Auth\CurrentUserExtension.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public static class CurrentUserExtension
{
    public static bool MayReach(this ICurrentUser user, TeacherId teacherId)
    {
        if (user.Role == Role.Administrator)
        {
            return true;
        }

        return user.TeacherId == teacherId;
    }
}
```

`TeacherId` is a record, so `==` compares values, and a null linked Teacher never equals the non-null `teacherId`. Any Role other than Administrator falls to the linked-Teacher comparison, so nothing unknown is ever let through.

- [ ] **Step 5: Read the Role and the linked Teacher from the token**

Replace `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs` with:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId Id => SignedInUserId();

    public Role Role => SignedInRole();

    public TeacherId? TeacherId => LinkedTeacherId();

    private UserId SignedInUserId()
    {
        var subject = ClaimValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(subject, out var id))
        {
            throw new InvalidOperationException("The request has no signed-in User.");
        }

        return UserId.Of(id);
    }

    private Role SignedInRole()
    {
        var role = ClaimValue(AuthClaims.Role);

        return role switch
        {
            AuthClaims.AdministratorRole => Role.Administrator,
            AuthClaims.TeacherRole => Role.Teacher,
            _ => throw new InvalidOperationException("The signed-in User has no known Role.")
        };
    }

    private TeacherId? LinkedTeacherId()
    {
        var teacherId = ClaimValue(AuthClaims.TeacherId);

        if (teacherId is null)
        {
            return UnlinkedTeacherId();
        }

        if (!Guid.TryParse(teacherId, out var id) || id == Guid.Empty)
        {
            throw new InvalidOperationException("The signed-in User's linked Teacher is malformed.");
        }

        return TeacherId.Of(id);
    }

    private TeacherId? UnlinkedTeacherId()
    {
        if (Role == Role.Teacher)
        {
            throw new InvalidOperationException("The signed-in Teacher-role User has no linked Teacher.");
        }

        return null;
    }

    private string? ClaimValue(string type)
    {
        var user = httpContextAccessor.HttpContext?.User;

        return user?.FindFirst(type)?.Value;
    }
}
```

`Role Role` and `TeacherId? TeacherId` rely on C#'s Color Color rule: `Role.Teacher` and `TeacherId.Of(id)` resolve to the type's members, `Role == ...` to the property. A missing `teacher_id` is fine only for an Administrator; for a Teacher it throws, so a Teacher can never fall through to "no filter" (decision 1).

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~HttpCurrentUserTest|FullyQualifiedName~CurrentUserExtensionTest"`

Expected: PASS, 18 test cases (13 `HttpCurrentUserTest`, 5 `CurrentUserExtensionTest`).

- [ ] **Step 7: Build and run the whole Application suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build succeeds without new warnings; every Application test PASSES, including `ChangeMyPasswordInteractorTest`, `ChangeUserRoleInteractorTest` and `DeleteUserInteractorTest` (their `A.Fake<ICurrentUser>()` needs no change), `SourceTextTest` and the `ControllerAuthorizationTest` matrix (no endpoint changes in this task).

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Application/Auth/ICurrentUser.cs src/DrivingLessons.Application/Auth/CurrentUserExtension.cs src/DrivingLessons.Presentation.Web/Auth/HttpCurrentUser.cs tests/DrivingLessons.Application.Test/Auth/HttpCurrentUserTest.cs tests/DrivingLessons.Application.Test/Auth/CurrentUserExtensionTest.cs
git commit -m "feat(api): the current-user port carries the Role and linked Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
