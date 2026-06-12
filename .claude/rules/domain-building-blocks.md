---
paths:
  - "**/Domain/**/*.cs"
  - "**/Domain.Test/**/*.cs"
---

# Domain Building Blocks

The reusable base types every aggregate is built from. These are written once in `Domain\Common\` — no framework package, no external dependencies. Copy these implementations exactly when scaffolding the domain.

## File Locations

| Type | Location |
|------|----------|
| `EntityId` (abstract base) | `Domain\Common\EntityId.cs` |
| `Entity<TId>` | `Domain\Common\Entity.cs` |
| `AggregateRoot<TId>` | `Domain\Common\AggregateRoot.cs` |
| `IDomainEvent` | `Domain\Common\IDomainEvent.cs` |
| `DomainException` | `Domain\Common\DomainException.cs` (exists — created in the auth slice) |
| Typed IDs (`PublicationId`, ...) | `Domain\Values\{Entity}Id.cs` |
| Value objects | `Domain\Values\{Value}.cs` |
| Domain rule exceptions | `Domain\Exceptions\{Entity}{Rule}Exception.cs` |
| Not-found exceptions | `Application\Common\Exceptions\{Entity}NotFoundException.cs` |

## EntityId — Strongly-Typed Identifiers

Every entity gets its own ID type. A `SlotId` can never be passed where a `PublicationId` is expected — the compiler enforces it.

### Base record

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

### Typed ID template

```csharp
namespace DrivingLessons.Domain.Values;

public record PublicationId : EntityId
{
    private PublicationId(Guid value)
        : base(value)
    {
    }

    public static PublicationId New()
    {
        return new PublicationId(Guid.NewGuid());
    }

    public static PublicationId Of(Guid value)
    {
        return new PublicationId(value);
    }
}
```

Key rules:
- Private constructor; only `New()` (domain-generated identity) and `Of(Guid)` (rehydration at boundaries) create instances
- `New()` is called inside the aggregate's `Create` factory — the domain owns identity, never the database
- `Of(Guid)` is called at the application boundary to convert incoming route/body Guids
- Records give value equality for free — two `PublicationId` with the same `Value` are equal

## Value Objects

Value objects are plain C# records — no base class needed; records provide structural equality. Every domain concept gets a type: **no primitives (string, int, Guid, bool, DateTime) as parameters in domain model methods.**

### Creation Rules

1. **Private constructor + static factory — always.** The factory is the single entry point; an invalid or non-canonical instance can never exist
2. **Never positional records** — `public record Email(string Value)` generates a public constructor that bypasses validation. Always nominal records with get-only properties and a private constructor
3. **Factory naming is semantic:**

| Factory | Meaning | Examples |
|---------|---------|----------|
| `Of(...)` | Validated construction from primitives — the only entry from outside the domain | `TargetSessionCount.Of(3)`, `SubmissionWindow.Of(start, end)`, `Email.Of(raw)` |
| `New()` | Domain-generated value; generation logic lives inside the type | `ShareableLinkToken.New()`, `PublicationId.New()` |

4. **All construction paths funnel through validation** — derived-value methods (`ExtendTo`) call `Of()` internally so every instance, however produced, is validated
5. **Normalization happens inside `Of()`**, before validation — the caller can never hold a non-canonical instance, and record equality works because equal concepts have equal canonical state
6. **One exception class per broken rule**: `{Value}Must{Rule}Exception : DomainException` in `Domain\Exceptions\` — never `ArgumentException` (reserved for the `EntityId` base only), never a shared `InvalidValueException`

### Single-value template

```csharp
namespace DrivingLessons.Domain.Values;

public record TargetSessionCount
{
    public int Value { get; }

    private TargetSessionCount(int value)
    {
        Value = value;
    }

    public static TargetSessionCount Of(int value)
    {
        if (value < 1)
        {
            throw new TargetSessionCountMustBePositiveException(value);
        }

        return new TargetSessionCount(value);
    }
}
```

### Normalizing template

```csharp
namespace DrivingLessons.Domain.Values;

public record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Of(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out _))
        {
            throw new EmailMustBeValidException();
        }

        return new Email(normalized);
    }
}
```

Normalization (trim, casing) is the value object's job — never the interactor's, never the controller's. `Email.Of(" Noa@Example.com ")` and `Email.Of("noa@example.com")` are equal instances. (Exception messages for values that are PII — emails, names — carry no payload; see the logging rule in code-style.md.)

### Multi-value template (with behavior and derived values)

```csharp
namespace DrivingLessons.Domain.Values;

public record SubmissionWindow
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    private SubmissionWindow(DateTime startUtc, DateTime endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public static SubmissionWindow Of(DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new SubmissionWindowEndMustBeAfterStartException(startUtc, endUtc);
        }

        return new SubmissionWindow(startUtc, endUtc);
    }

    public bool Contains(DateTime instantUtc)
    {
        return instantUtc >= StartUtc && instantUtc < EndUtc;
    }

    public SubmissionWindow ExtendTo(DateTime newEndUtc)
    {
        return Of(StartUtc, newEndUtc);
    }
}
```

### Generated-value template

```csharp
namespace DrivingLessons.Domain.Values;

public record ShareableLinkToken
{
    public string Value { get; }

    private ShareableLinkToken(string value)
    {
        Value = value;
    }

    public static ShareableLinkToken New()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                           .Replace('+', '-')
                           .Replace('/', '_')
                           .TrimEnd('=');

        return new ShareableLinkToken(value);
    }

    public static ShareableLinkToken Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ShareableLinkTokenMustNotBeEmptyException();
        }

        return new ShareableLinkToken(value);
    }
}
```

`New()` owns the unguessability requirement (cryptographic randomness — requirements §10); `Of()` exists for rehydration (EF converter, route token). Both are the only doors in.

### Wall-clock values (requirements §8.3)

Slot windows are recurring **local-time** concepts — model them with `TimeOnly`, never `DateTime`:

```csharp
public record SlotWindow
{
    public TimeOnly StartLocal { get; }
    public TimeOnly EndLocal { get; }
}
```

Putting a UTC `DateTime` inside a slot-window value object shifts it across DST transitions — that is a bug, not a simplification. `DateTime` (UTC) belongs only in instant-valued objects like `SubmissionWindow`.

### Optional values

- A value object that may be absent is `null` (`SlotConstraint?`) — never an "empty" instance like `SlotConstraint.None` or `Of(string.Empty)`
- `Of()` still rejects empty/whitespace input — absence is expressed by the caller passing `null`, not by constructing a hollow value

### Usage Rules

- Behavior lives on the type (`window.Contains(now)`, `window.ExtendTo(end)`) — never in helper or "manager" services
- Derived values return new instances — value objects are immutable; no `set;`, no mutation methods
- Once created, never re-validate downstream; the type is the proof
- Compare value objects directly (`a.Window == b.Window`) — records give structural equality; unwrapping `.Value` for comparison defeats the type
- `.Value` is unwrapped only at exit boundaries: response `Selector`s and EF converters
- **Enum vs value object**: enum when the valid set is fixed at compile time (`SessionType.Single | Double`, `Transmission`); value object when the constraint is a range, format, or structure (`TargetSessionCount`, `Email`, `SubmissionWindow`)

### Where Value Objects Are Created

| Layer | Allowed creation |
|-------|------------------|
| Interactor (application boundary) | `Of(request.Primitive)` — converts incoming primitives |
| Aggregate / domain methods | `New()` and derived instances (`ExtendTo`) |
| EF converter (rehydration) | `Of(storedValue)` |
| Controllers, DTOs, queries `Selector`s | never — primitives only |

### Testing Value Objects

Per `domain-testing.md`: one test file per value object at `Domain.Test\Values\{Value}Test.cs`. Cover:

- Happy path: `Of()` returns an instance exposing the (normalized) value
- One `Must_*` test per validation rule, `[DataRow]` over the invalid inputs (`Target_Count_Must_Be_Positive` with `0`, `-1`)
- Normalization facts: `Email_Is_Normalized_To_Lower_Case`
- Behavior facts: `Window_Contains_Instant_Before_End`, `Extended_Window_Keeps_Start`
- Generated values: `New_Tokens_Are_Unique`

## Entity&lt;TId&gt;

Identity-based equality: two entities are the same if they are the same concrete type with the same ID, regardless of property values.

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

Key rules:
- The parameterless protected constructor exists for EF Core only
- `protected init` on `Id` — set in the constructor, never mutated
- Child entities derive from `Entity<TId>` directly; they get **no event capability** (see below)

## AggregateRoot&lt;TId&gt; — With Domain Events

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

```csharp
namespace DrivingLessons.Domain.Common;

public interface IDomainEvent;
```

Key rules:
- **`AddEvent` is `protected` on the root — only the aggregate can raise events.** Child entities derive from `Entity<TId>`, which has no `AddEvent`; the type system enforces the rule that children never publish
- Every state-changing method raises exactly one event describing what happened
- `UncommittedEvents` is read by the infrastructure after `SaveChanges` succeeds; handlers run, then `CommitEvents()` clears the list
- Domain events are simple records named `{Entity}{Action}` in past tense:

```csharp
namespace DrivingLessons.Domain.Events;

public record PublicationClosed(PublicationId PublicationId, int ExcelVersion, DateTime ClosedAtUtc) : IDomainEvent;
```

## Worked Example

### Aggregate root

```csharp
namespace DrivingLessons.Domain.Entities;

public class Publication : AggregateRoot<PublicationId>
{
    public WeekScheduleId WeekScheduleId { get; private set; }
    public PublicationState State { get; private set; }
    public SubmissionWindow Window { get; private set; }
    public int ExcelVersion { get; private set; }

    public bool IsDraft => State is PublicationState.Draft;
    public bool IsOpen => State is PublicationState.Open;

    private Publication()
    {
    }

    private Publication(
        PublicationId id,
        WeekScheduleId weekScheduleId,
        PublicationState state,
        SubmissionWindow window,
        int excelVersion)
        : base(id)
    {
        WeekScheduleId = weekScheduleId;
        State = state;
        Window = window;
        ExcelVersion = excelVersion;

        var createdEvent = new PublicationCreated(id, weekScheduleId, window);
        AddEvent(createdEvent);
    }

    public static Publication Create(WeekSchedule weekSchedule, SubmissionWindow window)
    {
        const PublicationState state = PublicationState.Draft;
        const int excelVersion = 0;
        var id = PublicationId.New();

        return new Publication(id, weekSchedule.Id, state, window, excelVersion);
    }

    public void Close(DateTime closedAtUtc)
    {
        MustBeOpen();

        State = PublicationState.Closed;
        ExcelVersion += 1;

        AddEvent(new PublicationClosed(Id, ExcelVersion, closedAtUtc));
    }

    private void MustBeOpen()
    {
        if (!IsOpen)
        {
            throw new PublicationMustBeOpenException(Id);
        }
    }
}
```

### Child entity (no events, internal mutators)

```csharp
namespace DrivingLessons.Domain.Entities;

public class SlotRequest : Entity<SlotRequestId>
{
    public SlotId SlotId { get; private set; }
    public SessionType SessionType { get; private set; }
    public int Rank { get; private set; }

    private SlotRequest()
    {
    }

    private SlotRequest(SlotRequestId id, SlotId slotId, SessionType sessionType, int rank)
        : base(id)
    {
        SlotId = slotId;
        SessionType = sessionType;
        Rank = rank;
    }

    internal static SlotRequest Create(SlotId slotId, SessionType sessionType, int rank)
    {
        var id = SlotRequestId.New();
        return new SlotRequest(id, slotId, sessionType, rank);
    }

    internal void SetRank(int rank)
    {
        Rank = rank;
    }
}
```

The parent aggregate (`Submission`) creates `SlotRequest` via the `internal` factory, mutates it only through `internal` methods, and raises the corresponding events itself (`AddEvent(new SlotRequestAdded(Id, request.Id, slotId, rank))`).

## Exceptions

```csharp
namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeOpenException : DomainException
{
    public PublicationMustBeOpenException(PublicationId id)
        : base($"Publication {id.Value} must be open.")
    {
    }
}
```

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class PublicationNotFoundException : NotFoundException
{
    public PublicationNotFoundException(PublicationId id)
        : base($"Publication {id.Value} was not found.")
    {
    }
}
```

- `{Entity}Must{Condition}Exception : DomainException` in `Domain\Exceptions\` → mapped to 409 by `ApiExceptionFilter`
- `{Entity}NotFoundException : NotFoundException` in `Application\Common\Exceptions\` → mapped to 404 (`NotFoundException` was created in the auth slice)
- Always domain-specific classes — the message must identify the entity type

## EF Core Mapping

### Typed ID converter

One converter per ID type, in `Infrastructure\EntityFramework\EntityConfigurations\Converters\`:

```csharp
namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PublicationIdConverter : ValueConverter<PublicationId, Guid>
{
    public PublicationIdConverter()
        : base(
            id => id.Value,
            value => PublicationId.Of(value))
    {
    }
}
```

### Entity configuration

```csharp
public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<PublicationIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.WeekScheduleId)
            .HasColumnName("week_schedule_id")
            .HasConversion<WeekScheduleIdConverter>();

        builder.OwnsOne(
            x => x.Window,
            window =>
            {
                window.Property(w => w.StartUtc).HasColumnName("window_start_utc");
                window.Property(w => w.EndUtc).HasColumnName("window_end_utc");
            });
    }
}
```

Key rules:
- Every typed ID property gets `.HasConversion<{Id}Converter>()` — including foreign-key-like references (`WeekScheduleId`)
- `FindAsync(typedId)` still works: the converter applies to the primary key, so repositories pass the typed ID directly
- Single-value value objects use a converter; multi-value value objects use `OwnsOne`
- Table and column names stay snake_case

## Boundary Rule — Where Guids Become Typed IDs

Raw `Guid` exists only at the edges; typed IDs everywhere inside:

| Layer | Type used |
|-------|-----------|
| Controller (route/body binding) | `Guid` |
| Interactor (converts at entry) | `var publicationId = PublicationId.Of(id);` |
| Domain, repositories, queries | `PublicationId` |
| Response DTO / Selector (exit) | `Id = x.Id.Value` |

```csharp
public async Task ExecuteAsync(Guid id)
{
    var publicationId = PublicationId.Of(id);

    var publication = await repository.GetAsync(publicationId)
                      ?? throw new PublicationNotFoundException(publicationId);

    ...
}
```

## Anti-Patterns

- Raw `Guid`, `string`, `int`, `DateTime` parameters on domain model methods — wrap in a typed ID or value object
- Public constructors on typed IDs or value objects — only `New()` / `Of()` create instances
- Positional records for value objects (`public record Email(string Value)`) — the generated public constructor bypasses validation
- Normalization in interactors or controllers (`command.Email.Trim().ToLowerInvariant()`) — canonical form is the value object's job, inside `Of()`
- Validation outside `Of()` — re-checking a value object downstream means the type isn't trusted; fix the type
- "Empty" value object instances (`SlotConstraint.None`, `Of(string.Empty)`) — absence is `null`, the type only holds real values
- A shared `InvalidValueException` across value objects — one `{Value}Must{Rule}Exception` per rule
- `DateTime` inside recurring wall-clock concepts (slot windows) — `TimeOnly` local time per requirements §8.3
- Calling `AddEvent` from a child entity — won't compile, and that is intentional; the root raises events for its whole boundary
- Comparing entities by property values — entity equality is identity (`type + Id`); value equality belongs to value objects
- Mutable value objects or `set;` properties on them — derive new instances instead
- A shared `Id` value object across entities (one `GuidId` for everything) — defeats the type-safety purpose of typed IDs
- Exposing `Value` for comparisons inside the domain (`a.Id.Value == b.Id.Value`) — typed IDs are records; compare them directly (`a.Id == b.Id`)
