# Task 1 of 6: The lock port, a transaction-aware commit, and the `concurrentChange` 409

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires the plan commit only.

**Files:**
- Create: `src\DrivingLessons.Application\Common\ITeacherAndCarLocks.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\ConcurrentChangeException.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\TeacherAndCarLocks.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (`CommitAsync`)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (line ~32, next to the `IUnitOfWork` registration)
- Modify: `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` (the `mapping` switch, line ~23)
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (top-level `errors` object, after `"conflict"`)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces (tasks 2 to 4 and 6 rely on these exact names):
  - `DrivingLessons.Application.Common.ITeacherAndCarLocks`, with `Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds)` and `Task LockAllAsync()`. Both open a transaction on the request's `DrivingLessonsDbContext` if none is open, then lock rows with `FOR UPDATE`: Teachers first, then Cars, each ordered by id.
  - `DrivingLessons.Application.Common.Exceptions.ConcurrentChangeException(Exception innerException)`, giving 409 with code `concurrentChange`.
  - `DrivingLessonsDbContext.CommitAsync()` commits the open transaction, if there is one, after saving. It throws `ConcurrentChangeException` when EF throws `DbUpdateConcurrencyException`.
  - The translation key `errors.concurrentChange`.

**Why:** This is the mechanism under #109's AC. Tasks 2 and 3 call the port; task 4 gives Student the token whose failure this task maps.

- [ ] **Step 1: Write the failing filter test**

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add `using DrivingLessons.Application.Common.Exceptions;` if it isn't there, and add this test next to `Teacher_Already_Linked_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void Concurrent_Change_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new ConcurrentChangeException(new InvalidOperationException()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "concurrentChange");
    }
```

- [ ] **Step 2: Run it and see it fail**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest"
```

Expected: build error `CS0246: The type or namespace name 'ConcurrentChangeException' could not be found`.

- [ ] **Step 3: Add the exception and map it**

Create `src\DrivingLessons.Application\Common\Exceptions\ConcurrentChangeException.cs`:

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class ConcurrentChangeException(Exception innerException)
    : Exception("The data changed in another request at the same time.", innerException);
```

In `ApiExceptionFilter.cs`, in the `mapping` switch, add this arm right after the `NotFoundException` arm and before `SubmissionWindowMustBeOpenException`:

```csharp
            ConcurrentChangeException => (StatusCodes.Status409Conflict, "Conflict", null, code),
```

If `ConcurrentChangeException`'s namespace isn't imported yet, add `using DrivingLessons.Application.Common.Exceptions;` (`NotFoundException` lives there too, so it probably is).

- [ ] **Step 4: Run the filter tests**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest"
```

Expected: all pass, including the new test.

- [ ] **Step 5: Add the port**

Create `src\DrivingLessons.Application\Common\ITeacherAndCarLocks.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common;

public interface ITeacherAndCarLocks
{
    Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds);

    Task LockAllAsync();
}
```

- [ ] **Step 6: Implement it in Infrastructure**

Create `src\DrivingLessons.Infrastructure\EntityFramework\TeacherAndCarLocks.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class TeacherAndCarLocks : ITeacherAndCarLocks
{
    private readonly DrivingLessonsDbContext context;

    public TeacherAndCarLocks(DrivingLessonsDbContext context)
    {
        this.context = context;
    }

    public async Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds)
    {
        await BeginTransactionIfNoneAsync();

        var teacherKeys = teacherIds.Select(x => x.Value).ToArray();
        var carKeys = carIds.Select(x => x.Value).ToArray();

        await context.Database.ExecuteSqlAsync(
            $"SELECT id FROM teachers WHERE id = ANY({teacherKeys}) ORDER BY id FOR UPDATE");
        await context.Database.ExecuteSqlAsync(
            $"SELECT id FROM cars WHERE id = ANY({carKeys}) ORDER BY id FOR UPDATE");
    }

    public async Task LockAllAsync()
    {
        await BeginTransactionIfNoneAsync();

        await context.Database.ExecuteSqlAsync($"SELECT id FROM teachers ORDER BY id FOR UPDATE");
        await context.Database.ExecuteSqlAsync($"SELECT id FROM cars ORDER BY id FOR UPDATE");
    }

    private async Task BeginTransactionIfNoneAsync()
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return;
        }

        await context.Database.BeginTransactionAsync();
    }
}
```

Notes for the implementer:
- `ExecuteSqlAsync(FormattableString)` turns `{teacherKeys}` into a `uuid[]` parameter, so nothing is concatenated into the SQL.
- Running a `SELECT` through `ExecuteSqlAsync` executes it and discards the rows, and the row locks stay held until the transaction ends.
- `teachers.id` and `cars.id` are `uuid` columns (`TeacherIdConverter` and `CarIdConverter` map to `Guid`). If either turns out to be another type, cast the array in SQL (`ANY({teacherKeys}::uuid[])`) and note it.
- Deleted Teachers and Cars keep their rows (soft delete), so they're locked too, which is what Reactivate needs.

- [ ] **Step 7: Commit the transaction in `CommitAsync` and map the concurrency failure**

In `DrivingLessonsDbContext.cs`, add `using DrivingLessons.Application.Common.Exceptions;` and replace the whole `CommitAsync` method with these two methods:

```csharp
    public async Task CommitAsync()
    {
        try
        {
            await SaveAndDispatchAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrentChangeException(exception);
        }

        var transaction = Database.CurrentTransaction;

        if (transaction is null)
        {
            return;
        }

        await transaction.CommitAsync();
    }

    private async Task SaveAndDispatchAsync()
    {
        const int maxCycles = 10;

        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            await SaveChangesAsync();

            var roots = ChangeTracker.Entries<IHasDomainEvents>()
                                     .Where(x => x.Entity.UncommittedEvents.Count > 0)
                                     .Select(x => x.Entity)
                                     .ToList();

            if (roots.Count == 0)
            {
                return;
            }

            var domainEvents = roots.SelectMany(x => x.UncommittedEvents).ToList();
            roots.ForEach(x => x.CommitEvents());

            foreach (var domainEvent in domainEvents)
            {
                await dispatcher.DispatchAsync(domainEvent);
            }
        }

        await SaveChangesAsync();
    }
```

The loop body is the existing one, unchanged; only its early `return` now leaves `SaveAndDispatchAsync`, so the commit after it always runs. A command that throws before `CommitAsync` never commits. Its transaction rolls back when the request's scoped `DbContext` is disposed, which releases the locks.

- [ ] **Step 8: Register the port**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, right after

```csharp
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DrivingLessonsDbContext>());
```

add

```csharp
        services.AddScoped<ITeacherAndCarLocks, TeacherAndCarLocks>();
```

Both resolve the same scoped `DrivingLessonsDbContext`, so the transaction the locks open is the one `CommitAsync` commits. Add any `using` the file needs (`DrivingLessons.Application.Common` is probably already there for `IUnitOfWork`).

- [ ] **Step 9: Translations**

In the **top-level** `"errors"` object of each file (the one that holds `"conflict"` and `"notFound"`, around line 628, not the nested student-form `errors`), add right after the `"conflict"` line:

`client\public\i18n\he.json`:

```json
    "concurrentChange": "מישהו אחר שינה את הפרטים האלה באותו רגע. רעננו את הדף ונסו שוב.",
```

`client\public\i18n\en.json`:

```json
    "concurrentChange": "Someone else changed this at the same moment. Refresh the page and try again.",
```

- [ ] **Step 10: Build and run the suites**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/translations.spec.ts
```

Expected: build with 0 warnings, all tests pass, and the translations spec passes, which confirms both files have the key and no long dash.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Application/Common/ITeacherAndCarLocks.cs src/DrivingLessons.Application/Common/Exceptions/ConcurrentChangeException.cs src/DrivingLessons.Infrastructure/EntityFramework/TeacherAndCarLocks.cs src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs src/DrivingLessons.Infrastructure/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(students): lock Teachers and Cars in a transaction and answer a concurrent change with 409 (#109)"
```

End the commit message with the attribution trailer from the session's instructions.
