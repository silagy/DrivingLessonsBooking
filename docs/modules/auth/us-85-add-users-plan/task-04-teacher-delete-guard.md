# Task 4 of 7: Refuse to delete a Teacher who still has an active User (TDD)

> Part of [#85: Add Users from a New Users Screen](README.md). Requires tasks 1 to 3 committed. Work on branch `85-add-users-screen`.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveUserException.cs`
- Modify: `src\DrivingLessons.Application\Queries\IUserQueries.cs` (adds `ActiveExistsLinkedToTeacherAsync`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs` (implements it)
- Modify: `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (`errors.teacherMustNotHaveActiveUser`)
- Test: `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (one case)

**Interfaces:**
- Consumes: from tasks 1 and 2, `IUserQueries` and `UserQueries`. Existing `ITeacherRepository.GetAsync(TeacherId)`, `Teacher.Delete()`, `Teacher.IsDeleted`, `TeacherNotFoundException(TeacherId)`, `IUnitOfWork.CommitAsync()`, `ToastService.apiError` on the client (shows `errors.{code}` when the key exists).
- Produces:
  - `IUserQueries.ActiveExistsLinkedToTeacherAsync(TeacherId teacherId) : Task<bool>`, which ignores Deleted Users (README decision 2)
  - `TeacherMustNotHaveActiveUserException(TeacherId id)` (code `teacherMustNotHaveActiveUser`)
  - `DeleteTeacherInteractor(ITeacherRepository repository, IUserQueries userQueries, IUnitOfWork unitOfWork)`; `ExecuteAsync(Guid id)` is unchanged. #91 adds an active-Students check next to this one.

**Why:** #85 acceptance criterion 7 and the "Teacher delete blocked by linked User" interactor test in criterion 9; #82 story 34. The Cars & teachers screen already sends every delete failure through `ToastService.apiError`, so a translation key is the whole client change.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeleteTeacherInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"
```

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.DeleteTeacher;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class DeleteTeacherInteractorTest
{
    private ITeacherRepository repository = null!;
    private IUserQueries userQueries = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteTeacherInteractor interactor = null!;
    private Teacher teacher = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<ITeacherRepository>();
        userQueries = A.Fake<IUserQueries>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteTeacherInteractor(repository, userQueries, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));

        A.CallTo(() => repository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => repository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(A<TeacherId>._)).Returns(false);
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Without_A_User()
    {
        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Whose_Linked_User_Is_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(false);

        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_User_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveUserException>(act);
        teacher.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add after the tests tasks 1 and 2 added:

```csharp
    [TestMethod]
    public void Teacher_With_An_Active_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherMustNotHaveActiveUserException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherMustNotHaveActiveUser");
    }
```

- [ ] **Step 2: Run them and watch them fail**

Run the command above. Expected: build error, the three-argument `DeleteTeacherInteractor` constructor, `IUserQueries.ActiveExistsLinkedToTeacherAsync` and `TeacherMustNotHaveActiveUserException` don't exist.

- [ ] **Step 3: Add the exception and the query**

Create `src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveUserException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherMustNotHaveActiveUserException : DomainException
{
    public TeacherMustNotHaveActiveUserException(TeacherId id)
        : base($"Teacher {id.Value} still has an active User.")
    {
    }
}
```

In `src\DrivingLessons.Application\Queries\IUserQueries.cs`, add after `ExistsLinkedToTeacherAsync`:

```csharp

    Task<bool> ActiveExistsLinkedToTeacherAsync(TeacherId teacherId);
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`, add after `ExistsLinkedToTeacherAsync`:

```csharp

    public async Task<bool> ActiveExistsLinkedToTeacherAsync(TeacherId teacherId)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.TeacherId == teacherId && !x.IsDeleted);
    }
```

- [ ] **Step 4: Guard the delete**

Replace `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs` with:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteTeacher;

public class DeleteTeacherInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUserQueries userQueries;
    private readonly IUnitOfWork unitOfWork;

    public DeleteTeacherInteractor(ITeacherRepository repository, IUserQueries userQueries, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.userQueries = userQueries;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var hasActiveUser = await userQueries.ActiveExistsLinkedToTeacherAsync(teacherId);

        if (hasActiveUser)
        {
            throw new TeacherMustNotHaveActiveUserException(teacherId);
        }

        teacher.Delete();

        await unitOfWork.CommitAsync();
    }
}
```

`DeleteTeacherInteractor` is resolved from DI and `IUserQueries` is registered since task 1, so no registration changes.

- [ ] **Step 5: Translate the refusal**

In `client\public\i18n\en.json`, inside `"errors"`, add this key in alphabetical order (between `"teacherAlreadyDeleted"` and `"teacherNameMustNotBeEmpty"`):

```json
    "teacherMustNotHaveActiveUser": "This teacher still has an active User, so they can't be deleted.",
```

In `client\public\i18n\he.json`, inside `"errors"`, at the same position:

```json
    "teacherMustNotHaveActiveUser": "למורה הזה יש משתמש פעיל, ולכן אי אפשר למחוק אותו.",
```

Keep the files valid JSON (commas) and indented like their neighbours. Task 6 adds the other new `errors.*` keys; if task 6 already ran, keep its keys in alphabetical order too.

- [ ] **Step 6: Run the tests and watch them pass**

Run the command at the top of this task. Expected: `DeleteTeacherInteractorTest` 4 PASS, `ApiExceptionFilterTest` all PASS. Then:

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

From `client\` in PowerShell (checks the JSON still parses and has no typographic punctuation):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/core/**/*.spec.ts"
```

Expected: build clean, every test PASS.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain/Exceptions/TeacherMustNotHaveActiveUserException.cs src/DrivingLessons.Application/Queries/IUserQueries.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/UserQueries.cs src/DrivingLessons.Application/Commands/DeleteTeacher/DeleteTeacherInteractor.cs client/public/i18n/en.json client/public/i18n/he.json tests/DrivingLessons.Application.Test/Commands/DeleteTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs
git commit -m "feat(teachers): refuse to delete a Teacher who still has an active User (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
