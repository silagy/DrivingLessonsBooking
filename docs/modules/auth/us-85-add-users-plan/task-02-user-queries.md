# Task 2 of 7: List Users and get one User (TDD)

> Part of [#85: Add Users from a New Users Screen](README.md). Requires task 1 committed. Work on branch `85-add-users-screen`.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\IUserQueries.cs` (adds `FindAsync`, `GetAsync`)
- Create: `src\DrivingLessons.Application\Queries\FindUsers\ItemForFindUsersResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\FindUsers\FindUsersInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\GetUser\GetUserResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\GetUser\GetUserInteractor.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\UserNotFoundException.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs` (implements the two new methods)
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (register both interactors)
- Test: `tests\DrivingLessons.Application.Test\Queries\GetUserInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (one case)

**Interfaces:**
- Consumes: from task 1, `IUserQueries` (`ExistsWithSignInEmailAsync`, `ExistsLinkedToTeacherAsync`) and `UserQueries`. Existing `UserId.Of(Guid)`, `NotFoundException(string)`, `DrivingLessonsDbContext.Users` / `.Teachers`, `Role`.
- Produces:
  - `IUserQueries.FindAsync() : Task<IReadOnlyCollection<ItemForFindUsersResponse>>`, active Users first, then by name
  - `IUserQueries.GetAsync(Guid id) : Task<GetUserResponse?>`
  - `ItemForFindUsersResponse` and `GetUserResponse`, both with `Guid Id`, `string Name`, `string SignInEmail`, `Role Role`, `Guid? TeacherId`, `string? TeacherName`, `bool IsDeleted`. JSON: `id`, `name`, `signInEmail`, `role` (`"administrator"` / `"teacher"`), `teacherId` (null when unlinked), `teacherName` (null when unlinked), `isDeleted`. Task 5's client DTO mirrors this.
  - `FindUsersInteractor.ExecuteAsync() : Task<IReadOnlyCollection<ItemForFindUsersResponse>>`, `GetUserInteractor.ExecuteAsync(Guid id) : Task<GetUserResponse>`, both registered scoped. Task 3's controller calls them.
  - `UserNotFoundException(UserId id)` (code `userNotFound`)

**Why:** #85 acceptance criteria 1 (list shows name, email, Role, linked Teacher, deleted) and 8 (list / get endpoints). README decisions 7, 8 and 9.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetUserInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"
```

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Queries\GetUserInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetUserInteractorTest
{
    private IUserQueries queries = null!;
    private GetUserInteractor interactor = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IUserQueries>();
        interactor = new GetUserInteractor(queries);
    }

    [TestMethod]
    public async Task Returns_The_User()
    {
        //given
        var id = Guid.NewGuid();
        var user = new GetUserResponse
        {
            Id = id,
            Name = "Dana Levi",
            SignInEmail = "dana@school.example",
            Role = Role.Administrator,
            TeacherId = null,
            TeacherName = null,
            IsDeleted = false
        };
        A.CallTo(() => queries.GetAsync(id)).Returns(user);

        //when
        var result = await interactor.ExecuteAsync(id);

        //then
        result.ShouldBe(user);
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var id = Guid.NewGuid();
        A.CallTo(() => queries.GetAsync(id)).Returns((GetUserResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(id);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
    }
}
```

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add after the tests task 1 added:

```csharp
    [TestMethod]
    public void Missing_User_Is_Not_Found_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserNotFoundException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "userNotFound");
    }
```

- [ ] **Step 2: Run them and watch them fail**

Run the command above. Expected: build error, `GetUserInteractor`, `GetUserResponse`, `IUserQueries.GetAsync` and `UserNotFoundException` don't exist.

- [ ] **Step 3: Add the responses**

Create `src\DrivingLessons.Application\Queries\FindUsers\ItemForFindUsersResponse.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindUsers;

public class ItemForFindUsersResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SignInEmail { get; init; } = string.Empty;
    public Role Role { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public bool IsDeleted { get; init; }
}
```

Create `src\DrivingLessons.Application\Queries\GetUser\GetUserResponse.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetUser;

public class GetUserResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SignInEmail { get; init; } = string.Empty;
    public Role Role { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public bool IsDeleted { get; init; }
}
```

Neither has a static `Selector`: the Teacher name comes from a left join, which a `Func<User, T>` expression can't express. The projection lives in `UserQueries` (precedent: `StudentQueries.FindAsync`).

- [ ] **Step 4: Extend the query port**

Replace `src\DrivingLessons.Application\Queries\IUserQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IUserQueries
{
    Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync();

    Task<GetUserResponse?> GetAsync(Guid id);

    Task<bool> ExistsWithSignInEmailAsync(Email signInEmail);

    Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId);
}
```

- [ ] **Step 5: Add the exception and the interactors**

Create `src\DrivingLessons.Application\Common\Exceptions\UserNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(UserId id)
        : base($"User {id.Value} was not found.")
    {
    }
}
```

Create `src\DrivingLessons.Application\Queries\FindUsers\FindUsersInteractor.cs`:

```csharp
namespace DrivingLessons.Application.Queries.FindUsers;

public class FindUsersInteractor
{
    private readonly IUserQueries queries;

    public FindUsersInteractor(IUserQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> ExecuteAsync()
    {
        return await queries.FindAsync();
    }
}
```

Create `src\DrivingLessons.Application\Queries\GetUser\GetUserInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetUser;

public class GetUserInteractor
{
    private readonly IUserQueries queries;

    public GetUserInteractor(IUserQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetUserResponse> ExecuteAsync(Guid id)
    {
        var user = await queries.GetAsync(id);

        if (user is null)
        {
            var userId = UserId.Of(id);
            throw new UserNotFoundException(userId);
        }

        return user;
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.Queries.FindUsers;` and `using DrivingLessons.Application.Queries.GetUser;` in alphabetical order among the `Queries` usings, and register both right after `CreateUserInteractor`:

```csharp
        services.AddScoped<CreateUserInteractor>();
        services.AddScoped<FindUsersInteractor>();
        services.AddScoped<GetUserInteractor>();
```

- [ ] **Step 6: Implement the projections**

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`, add the usings and the two methods (keep the two existence checks from task 1 below them). The join reads Teachers with `IgnoreQueryFilters()` so a deleted Teacher's name still shows (README decision 8). The null checks use `==` because a pattern (`is null`) can't appear in an expression tree (precedent: `IdentifyStudentResponse`):

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class UserQueries : IUserQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public UserQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync()
    {
        var query = from user in dbContext.Users
                    join teacher in dbContext.Teachers.IgnoreQueryFilters()
                        on user.TeacherId equals teacher.Id into linkedTeachers
                    from linkedTeacher in linkedTeachers.DefaultIfEmpty()
                    orderby user.IsDeleted, user.Name
                    select new ItemForFindUsersResponse
                    {
                        Id = user.Id.Value,
                        Name = user.Name.Value,
                        SignInEmail = user.SignInEmail.Value,
                        Role = user.Role,
                        TeacherId = user.TeacherId == null
                            ? null
                            : (Guid?)user.TeacherId.Value,
                        TeacherName = linkedTeacher == null
                            ? null
                            : linkedTeacher.Name.Value,
                        IsDeleted = user.IsDeleted
                    };

        return await query.ToListAsync();
    }

    public async Task<GetUserResponse?> GetAsync(Guid id)
    {
        var userId = UserId.Of(id);

        var query = from user in dbContext.Users
                    join teacher in dbContext.Teachers.IgnoreQueryFilters()
                        on user.TeacherId equals teacher.Id into linkedTeachers
                    from linkedTeacher in linkedTeachers.DefaultIfEmpty()
                    where user.Id == userId
                    select new GetUserResponse
                    {
                        Id = user.Id.Value,
                        Name = user.Name.Value,
                        SignInEmail = user.SignInEmail.Value,
                        Role = user.Role,
                        TeacherId = user.TeacherId == null
                            ? null
                            : (Guid?)user.TeacherId.Value,
                        TeacherName = linkedTeacher == null
                            ? null
                            : linkedTeacher.Name.Value,
                        IsDeleted = user.IsDeleted
                    };

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsWithSignInEmailAsync(DrivingLessons.Domain.Values.Email signInEmail)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.SignInEmail == signInEmail);
    }

    public async Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.TeacherId == teacherId);
    }
}
```

These projections are exercised against Postgres in task 3's smoke steps: no unit test reaches EF translation in this codebase.

- [ ] **Step 7: Run the tests and watch them pass**

Run the command at the top of this task. Expected: `GetUserInteractorTest` 2 PASS, `ApiExceptionFilterTest` all PASS. Then:

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build clean, every test PASS.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Application/Queries/IUserQueries.cs src/DrivingLessons.Application/Queries/FindUsers src/DrivingLessons.Application/Queries/GetUser src/DrivingLessons.Application/Common/Exceptions/UserNotFoundException.cs src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/UserQueries.cs tests/DrivingLessons.Application.Test/Queries/GetUserInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs
git commit -m "feat(application): list Users with their linked Teacher and get one User (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
