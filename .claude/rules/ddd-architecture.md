---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---

# DDD Architecture Guide

This application is a **modular monolith** following Domain-Driven Design and Onion Architecture. The single goal is **maintainability**: each module has one reason to change, dependencies point inward, and business rules are centralized in the domain.

## Solution Layout

```
src\
├── DrivingLessons.Domain\            entities, value objects, domain events, repository interfaces, domain exceptions
├── DrivingLessons.Application\       interactors (commands/queries), request/response DTOs, query interfaces
├── DrivingLessons.Infrastructure\    EF Core DbContext, repositories, query implementations, entity configurations
└── DrivingLessons.Presentation.Web\  controllers, DI registration, startup
tests\
└── DrivingLessons.Domain.Test\       domain unit tests
```

### Layer Dependencies

```
Domain            → ∅  (no dependencies — no frameworks, no EF, no HTTP)
Application       → Domain
Infrastructure    → Application, Domain, EF Core, Npgsql
Presentation.Web  → Application
Domain.Test       → Domain
```

**The cardinal rule: no layer depends on a layer above it. Domain depends on nothing. Everything depends on Domain.**

### Where to Put Code?

| Code Type | Location | Class Name |
|-----------|----------|------------|
| Base types (EntityId, Entity, AggregateRoot, IDomainEvent, DomainException) | `Domain\Common\{BaseType}.cs` | see domain-building-blocks.md |
| Business rule | `Domain\Entities\{Entity}.cs` | `{Entity}` |
| Typed ID | `Domain\Values\{Entity}Id.cs` | `{Entity}Id` |
| Value object | `Domain\Values\{Value}.cs` | `{Value}` |
| Domain event | `Domain\Events\{Entity}{Action}.cs` | `{Entity}{Action}` |
| Domain exception | `Domain\Exceptions\{Entity}{Rule}Exception.cs` | `{Entity}{Rule}Exception` |
| Not-found exception | `Application\Common\Exceptions\{Entity}NotFoundException.cs` | `{Entity}NotFoundException` |
| Repository interface | `Domain\Repositories\I{Entity}Repository.cs` | `I{Entity}Repository` |
| Command + handler | `Application\Commands\{Op}{Entity}\{Op}{Entity}Interactor.cs` | `{Op}{Entity}Interactor` |
| Command request DTO | `Application\Commands\{Op}{Entity}\{Op}{Entity}Request.cs` | `{Op}{Entity}Request` |
| Query + handler | `Application\Queries\{Op}{Entity}\{Op}{Entity}Interactor.cs` | `{Op}{Entity}Interactor` |
| Query response DTO | `Application\Queries\{Op}{Entity}\{Op}{Entity}Response.cs` | `{Op}{Entity}Response` |
| Query interface | `Application\Queries\I{Entity}Queries.cs` | `I{Entity}Queries` |
| DbContext | `Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` | `DrivingLessonsDbContext` |
| Repository impl | `Infrastructure\EntityFramework\Repositories\{Entity}Repository.cs` | `{Entity}Repository` |
| Query impl | `Infrastructure\EntityFramework\Queries\{Entity}Queries.cs` | `{Entity}Queries` |
| EF configuration | `Infrastructure\EntityFramework\EntityConfigurations\{Entity}Configuration.cs` | `{Entity}Configuration` |
| HTTP endpoint | `Presentation.Web\Controllers\{Entity}\` | see api-guidelines.md |

## Critical Rules

1. **Operations are NOT idempotent** — throw a domain exception if the entity is already in the target state
2. **Domain methods accept resolved entities/value objects, never raw IDs or primitives**
3. **All state changes go through aggregate methods** — no public setters anywhere in the domain
4. **The application layer resolves, the domain decides** — "does it exist?" is an application question; "is it allowed?" is a domain question
5. **Always search for existing code** before creating new files

## Domain Layer

### Building Blocks

The domain defines its own small base types in `Domain\Common\` — no framework package: `EntityId` (strongly-typed identifiers), `Entity<TId>` (identity equality), `AggregateRoot<TId>` (domain event raising via `AddEvent`/`UncommittedEvents`/`CommitEvents`), and the `IDomainEvent` marker. **Full reference implementations live in `domain-building-blocks.md` — copy them exactly.**

Every entity gets its own ID type (`PublicationId`, `SubmissionId`) deriving from `EntityId`; raw `Guid` never appears in domain signatures.

### Aggregate Template

Aggregates own their invariants. Private constructors, a static `Create` factory that generates the ID and raises the created event, and explicit state guards in every method.

```csharp
public class Publication : AggregateRoot<PublicationId>
{
    public WeekScheduleId WeekScheduleId { get; private set; }
    public PublicationState State { get; private set; }
    public ShareableLinkToken LinkToken { get; private set; }
    public SubmissionWindow Window { get; private set; }
    public int ExcelVersion { get; private set; }

    public bool IsDraft => State is PublicationState.Draft;
    public bool IsOpen => State is PublicationState.Open;
    public bool IsClosed => State is PublicationState.Closed;

    private Publication()
    {
    }

    private Publication(
        PublicationId id,
        WeekScheduleId weekScheduleId,
        PublicationState state,
        ShareableLinkToken linkToken,
        SubmissionWindow window,
        int excelVersion)
        : base(id)
    {
        WeekScheduleId = weekScheduleId;
        State = state;
        LinkToken = linkToken;
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
        var linkToken = ShareableLinkToken.New();

        return new Publication(id, weekSchedule.Id, state, linkToken, window, excelVersion);
    }

    public void Publish()
    {
        MustBeDraft();

        State = PublicationState.Published;

        AddEvent(new PublicationPublished(Id));
    }

    public void Close(DateTime closedAtUtc)
    {
        MustBeOpen();

        State = PublicationState.Closed;
        ExcelVersion += 1;

        AddEvent(new PublicationClosed(Id, ExcelVersion, closedAtUtc));
    }

    public void Reopen(SubmissionWindow extendedWindow)
    {
        MustBeClosed();

        State = PublicationState.Open;
        Window = extendedWindow;

        AddEvent(new PublicationReopened(Id, extendedWindow));
    }

    private void MustBeDraft()
    {
        if (!IsDraft)
        {
            throw new PublicationMustBeDraftException(Id);
        }
    }

    private void MustBeOpen()
    {
        if (!IsOpen)
        {
            throw new PublicationMustBeOpenException(Id);
        }
    }

    private void MustBeClosed()
    {
        if (!IsClosed)
        {
            throw new PublicationMustBeClosedException(Id);
        }
    }
}
```

Key rules:
- Parameterless private constructor for EF only
- `Create` defines **all** initial values (including defaults like initial state) as local variables — no hardcoded defaults buried in the constructor
- Method body order, separated by blank lines: **guards → mutations → events**
- State checks use pattern matching exposed as `Is{State}` properties
- Method names are explicit business operations (`Reopen`, `ExtendWindow`) — never a generic `Update(dto)`

### State Machines

Every aggregate with a lifecycle gets a state enum with explicit values:

```csharp
public enum PublicationState
{
    Draft = 10,
    Published = 20,
    Open = 30,
    Closed = 40
}
```

Transitions only through named methods (`Publish`, `Open`, `Close`, `Reopen`), each guarded by `MustBe*` checks. Re-applying a transition throws — operations are not idempotent.

### Validation Methods

- Named `MustBe*` / `MustHave*`, always `private` (or `internal` on owned entities called by the parent)
- Always throw, never return bool
- Always domain-specific exception classes: `PublicationMustBeOpenException`, not a generic `EntityMustBeOpenException` — error messages must identify the entity type

```csharp
public class PublicationMustBeOpenException : DomainException
{
    public PublicationMustBeOpenException(PublicationId id)
        : base($"Publication {id.Value} must be open.")
    {
    }
}
```

`DomainException` (`Domain\Common\`) is the single abstract base for all domain rule violations; `NotFoundException` (`Application\Common\Exceptions\`) is the base for application-level not-found errors. The API layer maps both (see api-guidelines.md).

### Owned (Child) Entities

Child entities live inside the aggregate boundary and are only reachable through the root:

```csharp
public class Submission : AggregateRoot<SubmissionId>
{
    private readonly List<SlotRequest> slotRequests = [];

    public PublicationId PublicationId { get; private set; }
    public StudentId StudentId { get; private set; }
    public TargetSessionCount TargetCount { get; private set; }

    public IReadOnlyCollection<SlotRequest> SlotRequests => slotRequests.AsReadOnly();

    public SlotRequest AddSlotRequest(Slot slot, SessionType sessionType, SlotConstraint? constraint)
    {
        SlotMustBeOpen(slot);
        SlotMustNotBeRequestedTwice(slot);

        var rank = slotRequests.Count + 1;
        var request = SlotRequest.Create(slot.Id, sessionType, constraint, rank);
        slotRequests.Add(request);

        AddEvent(new SlotRequestAdded(Id, request.Id, slot.Id, rank));

        return request;
    }

    public void RemoveSlotRequest(SlotRequest request)
    {
        var removed = slotRequests.Remove(request);

        if (!removed)
        {
            throw new SlotRequestNotInSubmissionException(Id, request.Id);
        }

        Rerank();

        AddEvent(new SlotRequestRemoved(Id, request.Id));
    }
}

public class SlotRequest : Entity<SlotRequestId>
{
    public SlotId SlotId { get; private set; }
    public SessionType SessionType { get; private set; }
    public SlotConstraint? Constraint { get; private set; }
    public int Rank { get; private set; }

    private SlotRequest()
    {
    }

    private SlotRequest(SlotRequestId id, SlotId slotId, SessionType sessionType, SlotConstraint? constraint, int rank)
        : base(id)
    {
        SlotId = slotId;
        SessionType = sessionType;
        Constraint = constraint;
        Rank = rank;
    }

    internal static SlotRequest Create(SlotId slotId, SessionType sessionType, SlotConstraint? constraint, int rank)
    {
        var id = SlotRequestId.New();
        return new SlotRequest(id, slotId, sessionType, constraint, rank);
    }

    internal void SetRank(int rank)
    {
        Rank = rank;
    }
}
```

Key rules:
- Child entities extend `Entity<TId>` — they have no `AddEvent`, so they physically cannot raise events
- Child entity methods called by the parent are `internal` — nothing outside the aggregate can mutate a child
- **Only the aggregate root raises domain events** — children don't know what is business-significant
- **Law of Demeter**: the aggregate never reaches through a child to manipulate its internals (`request.Constraint.Change(...)` is forbidden) — it calls one `internal` method on the child and the child handles the rest
- The aggregate resolves children from its own collection; callers pass the resolved child object, not its ID

### Value Objects

Wrap every domain concept — no primitives (string, int, Guid, bool, DateTime) as parameters in domain model methods.

```csharp
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
}
```

Key rules:
- `record` with private constructor + static `Of()` factory that validates — an invalid instance can never exist
- Behavior lives with the data (`window.Contains(now)`, not a helper service)
- **Enum vs value object**: enum when the valid set is fixed at compile time (`SessionType.Single | Double`, `Transmission.Automatic | Manual`); value object when the constraint is a range, format, or structure (`TargetSessionCount`, `Email`)
- Once validated at creation, never re-validate downstream — the type is the proof

### Domain Events

Every meaningful state change raises an event. Events are **simple records created inline** — no event factory classes, no content-class hierarchy:

```csharp
public record PublicationCreated(PublicationId PublicationId, WeekScheduleId WeekScheduleId, SubmissionWindow Window) : IDomainEvent;

public record PublicationClosed(PublicationId PublicationId, int ExcelVersion, DateTime ClosedAtUtc) : IDomainEvent;

public record SlotRequestAdded(SubmissionId SubmissionId, SlotRequestId SlotRequestId, SlotId SlotId, int Rank) : IDomainEvent;
```

Key rules:
- Named `{Entity}{Action}` in past tense, one file per event in `Domain\Events\`
- Carry enough context for a handler to react without querying back — but only what changed (property-change events carry the new value, lifecycle events carry a snapshot of relevant state)
- **One event class per distinct operation** — never a shared catch-all `PropertyChangedEvent`
- **Prefer new events over modifying existing ones** — preserve existing consumers' behavior
- Events are dispatched after `SaveChanges` succeeds (in-process handlers, e.g. the Excel email on `PublicationClosed`), then `CommitEvents()` clears them

### Repository Interfaces

One per aggregate, defined in Domain:

```csharp
public interface IPublicationRepository
{
    Task<Publication?> GetAsync(PublicationId id);

    Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token);

    void Add(Publication publication);

    IUnitOfWork UnitOfWork { get; }
}
```

- **Repositories never throw on missing entities** — return `null`; the interactor interprets absence
- Speak the domain's language (`GetByLinkTokenAsync`), no generic `Repository<T>`
- `Add` is synchronous (`void`) — persistence happens at commit

## Application Layer

### Interactors — One Class per Operation

No MediatR. Each command and query is its own class with a single `ExecuteAsync` method, injected directly into the controller action.

**Command interactor** — thin orchestration only: resolve → call domain method → commit.

```csharp
public class ClosePublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly TimeProvider timeProvider;

    public ClosePublicationInteractor(IPublicationRepository repository, TimeProvider timeProvider)
    {
        this.repository = repository;
        this.timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        publication.Close(nowUtc);

        await repository
            .UnitOfWork
            .CommitAsync();
    }
}
```

**Create interactor** — maps request primitives to value objects at the boundary, then hands resolved objects to the domain:

```csharp
public class CreatePublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IWeekScheduleRepository weekScheduleRepository;

    public CreatePublicationInteractor(
        IPublicationRepository repository,
        IWeekScheduleRepository weekScheduleRepository)
    {
        this.repository = repository;
        this.weekScheduleRepository = weekScheduleRepository;
    }

    public async Task<CreatePublicationResponse> ExecuteAsync(CreatePublicationRequest request)
    {
        var weekScheduleId = WeekScheduleId.Of(request.WeekScheduleId);

        var weekSchedule = await weekScheduleRepository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        var window = SubmissionWindow.Of(request.WindowStartUtc, request.WindowEndUtc);
        var publication = Publication.Create(weekSchedule, window);

        repository.Add(publication);

        await repository
            .UnitOfWork
            .CommitAsync();

        return new CreatePublicationResponse(publication.Id.Value, publication.LinkToken.Value);
    }
}
```

Key rules:
- **No business rules in interactors** — whether something is allowed belongs to the aggregate; the interactor only loads, calls, and saves
- **Not-found is an application concern** — repository returns `null`, interactor throws `{Entity}NotFoundException`
- One interactor = one reason to change; each declares only the dependencies it actually needs
- Value objects and typed IDs are constructed at the application boundary (`SubmissionWindow.Of(...)`, `PublicationId.Of(id)`) so the domain never sees raw primitives

### Queries

Queries bypass the domain model entirely — they project straight to response DTOs through a query interface:

```csharp
public interface IPublicationQueries
{
    Task<GetPublicationResponse?> GetAsync(Guid id);

    Task<IReadOnlyCollection<ItemForFindPublicationsResponse>> FindByTeacherAsync(Guid teacherId);
}
```

```csharp
public class GetPublicationInteractor
{
    private readonly IPublicationQueries queries;

    public GetPublicationInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetPublicationResponse> ExecuteAsync(Guid id)
    {
        var publication = await queries.GetAsync(id)
                          ?? throw new PublicationNotFoundException(id);

        return publication;
    }
}
```

Response DTO naming (Swagger requires globally unique names — embed operation and context):

| Pattern | Template | Example |
|---------|----------|---------|
| Primary response | `{Op}{Entity}Response` | `GetPublicationResponse` |
| Nested component | `{Property}For{Op}{Entity}Response` | `SlotForGetWeekScheduleResponse` |
| Collection item | `ItemForFind{Entities}Response` | `ItemForFindPublicationsResponse` |

Response DTOs use `init` properties and a static `Selector` expression for EF projection:

```csharp
public class GetPublicationResponse
{
    public Guid Id { get; init; }
    public PublicationState State { get; init; }
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }

    public static Expression<Func<Publication, GetPublicationResponse>> Selector =>
        x => new GetPublicationResponse
        {
            Id = x.Id.Value,
            State = x.State,
            WindowStartUtc = x.Window.StartUtc,
            WindowEndUtc = x.Window.EndUtc
        };
}
```

Response DTOs expose raw `Guid`s (`x.Id.Value`) — typed IDs stop at the application boundary.

### Unit of Work

`IUnitOfWork` (defined in Application) exposes `CommitAsync()`. The DbContext implements it. One commit per interactor — a single transaction even when two aggregates change together.

## Infrastructure Layer

### DbContext

One context for the whole monolith:

```csharp
public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Submission> Submissions => Set<Submission>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options)
        : base(options)
    {
    }

    public async Task CommitAsync()
    {
        await SaveChangesAsync();

        await DispatchDomainEventsAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
```

### Repositories

- Implement the domain interface directly — no base class
- Inject the concrete `DrivingLessonsDbContext`, not an abstraction
- PK lookups use `FindAsync(id)` (hits the change tracker first, loads owned collections); lookups with predicates use `FirstOrDefaultAsync` — **never `SingleOrDefaultAsync`**

```csharp
public class PublicationRepository : IPublicationRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public IUnitOfWork UnitOfWork => dbContext;

    public PublicationRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Publication?> GetAsync(PublicationId id)
    {
        return await dbContext.Publications.FindAsync(id);
    }

    public async Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token)
    {
        return await dbContext
                         .Publications
                         .FirstOrDefaultAsync(x => x.LinkToken == token);
    }

    public void Add(Publication publication)
    {
        dbContext.Publications.Add(publication);
    }
}
```

### Query Implementations

- Implement `I{Entity}Queries` from Application
- Always project via the response's static `Selector` — never materialize aggregates for reads
- Use `AsNoTracking` semantics (projection makes this automatic)
- Extract filter values into local variables before the LINQ expression

```csharp
public class PublicationQueries : IPublicationQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public PublicationQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetPublicationResponse?> GetAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        return await dbContext
                         .Publications
                         .Where(x => x.Id == publicationId)
                         .Select(GetPublicationResponse.Selector)
                         .FirstOrDefaultAsync();
    }
}
```

### EF Configurations

One `IEntityTypeConfiguration<T>` per aggregate:

- Table and column names in **snake_case**
- Every typed ID property gets `.HasConversion<{Id}Converter>()` — see domain-building-blocks.md for the converter template
- Value objects map with `OwnsOne` (or a `ValueConverter` for single-value records like `ShareableLinkToken`)
- Child collections map with `OwnsMany` to their own table; the parent FK is a **shadow property** (`owner_id`), never a CLR property on the child
- Enums stored as their numeric value

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
            .Property(x => x.LinkToken)
            .HasColumnName("link_token")
            .HasConversion<ShareableLinkTokenConverter>();

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

## Anti-Patterns

- Business logic in interactors, controllers, or "service" classes — it belongs in the aggregate
- Public setters or `init` properties on domain entities
- Domain methods accepting IDs or primitives instead of resolved entities and value objects
- Silently idempotent operations — re-closing a closed publication must throw
- Repositories throwing not-found exceptions — return `null`, let the interactor decide
- Generic `Repository<T>` or generic CRUD "managers"
- Loading full aggregates to serve a read — queries project to DTOs via `Selector`
- A child entity raising domain events or exposing `public` mutators
- Reaching through a child entity (`submission.SlotRequests.First().SetRank(1)`) instead of calling an aggregate method
- One shared event class for multiple operations
- `SingleOrDefaultAsync` anywhere — `FindAsync` for PKs, `FirstOrDefaultAsync` otherwise
- Generic exceptions (`InvalidOperationException`) for domain rules — every rule gets its own exception type
- Raw `Guid` flowing past the application boundary — convert to typed IDs on entry (`PublicationId.Of(id)`), back to `Guid` on exit (`x.Id.Value`)

## Deliberately Omitted (keep it simple)

These patterns exist in larger systems but are **out of scope here** — do not introduce them without an explicit decision:

- Microservices / schema-per-service — this is one deployable, one schema
- Outbox pattern + message broker integration events — domain events are handled in-process
- Batch-update command envelopes — each operation is its own endpoint and interactor
- A separate ReadModel project — response DTOs live in Application
- ETag / If-Match optimistic locking — add per-aggregate concurrency tokens later if concurrent admin edits become real (concurrent student submissions are protected by the DB transaction)
