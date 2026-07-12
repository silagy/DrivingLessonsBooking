# Task 1 of 12: Domain building blocks + test project

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md), issues #3 #4 #5). Work on branch `3-us-02-04-teachers-module`, commands run from the repo root.

## Shared Context

**Goal:** Scaffold the reusable domain base types (`EntityId`, `Entity<TId>`, `AggregateRoot<TId>`, `IDomainEvent`) and the `DrivingLessons.Domain.Test` project — the foundation for the Teacher aggregate and every future aggregate.

**Rules:** base types are copied **verbatim** from `.claude/rules/domain-building-blocks.md`. Tests use MSTest + Shouldly. No comments anywhere except `//given //when //then` in tests.

---

**Files:**
- Create: `src/DrivingLessons.Domain/Common/EntityId.cs`, `Entity.cs`, `AggregateRoot.cs`, `IDomainEvent.cs`
- Create: `tests/DrivingLessons.Domain.Test/` project + `Common/Faker.cs`
- Modify: `DrivingLessons.sln`

- [x] **Step 1: `src/DrivingLessons.Domain/Common/EntityId.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

public abstract record EntityId
{
    public Guid Value { get; }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id must not be empty.", nameof(value));
        }

        Value = value;
    }
}
```

- [x] **Step 2: `src/DrivingLessons.Domain/Common/Entity.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

public abstract class Entity<TId>
    where TId : EntityId
{
    public TId Id { get; protected init; }

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
```

Note: `Id` is non-nullable and assigned only in the id-taking constructor; the parameterless constructor exists for EF materialization, which sets `Id` afterwards. If the build treats the resulting CS8618 as an error, add `= null!;` to the property initializer — do not make `Id` nullable.

- [x] **Step 3: `src/DrivingLessons.Domain/Common/AggregateRoot.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : EntityId
{
    private readonly List<IDomainEvent> uncommittedEvents = [];

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected void AddEvent(IDomainEvent domainEvent)
    {
        uncommittedEvents.Add(domainEvent);
    }

    public void CommitEvents()
    {
        uncommittedEvents.Clear();
    }
}
```

- [x] **Step 4: `src/DrivingLessons.Domain/Common/IDomainEvent.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

public interface IDomainEvent;
```

- [x] **Step 5: Scaffold the test project**

```
dotnet new mstest -n DrivingLessons.Domain.Test -o tests/DrivingLessons.Domain.Test -f net10.0
dotnet sln DrivingLessons.sln add tests/DrivingLessons.Domain.Test
dotnet add tests/DrivingLessons.Domain.Test reference src/DrivingLessons.Domain
dotnet add tests/DrivingLessons.Domain.Test package Shouldly
```

Delete the template's sample test file (`Test1.cs` or `UnitTest1.cs` — whatever `dotnet new mstest` generated).

- [x] **Step 6: `tests/DrivingLessons.Domain.Test/Common/Faker.cs`**

```csharp
namespace DrivingLessons.Domain.Test.Common;

public static class Faker
{
    public static string FakeString()
    {
        return $"fake-{Guid.NewGuid():N}";
    }

    public static string FakeEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }
}
```

- [x] **Step 7: Verify + commit**

Run: `dotnet build` — expected: success.
Run: `dotnet test` — expected: success (zero tests is fine at this point).

```bash
git add src tests DrivingLessons.sln
git commit -m "feat(domain): aggregate building blocks and test project scaffolding"
```

---

**Next:** [task-02-values.md](task-02-values.md)
