---
paths:
  - "**/*.Test/**/*.cs"
  - "**/*.Test.csproj"
---

# Domain Testing Guide

Domain tests are in-memory unit tests of business rules through aggregate roots. They need no mocks, no database, no infrastructure — `Create()` the aggregate, call a method, assert. If a domain test needs a mock, the design is wrong.

## Frameworks

- **Test framework**: MSTest
- **Assertions**: Shouldly — never mix assertion libraries in one project
- **Mocking** (application-layer tests only): FakeItEasy
- **Test data**: a small `Faker` helper (`Faker.FakeString()`, `Faker.FakeInt()`) — never hardcoded literals like `"test-string"`

## Test File Organization

Test hierarchy mirrors the domain structure; tests are organized **by entity, never by feature**:

```
Domain\Entities\Publication.cs      → Domain.Test\Entities\PublicationTest.cs
Domain\Entities\Submission.cs       → Domain.Test\Entities\SubmissionTest.cs
Domain\Entities\SlotRequest.cs      → Domain.Test\Entities\SlotRequestTest.cs
Domain\Values\SubmissionWindow.cs   → Domain.Test\Values\SubmissionWindowTest.cs
Domain.Test\Entities\Fake\{Entity}FakeBuilder.cs
```

- Aggregate root tests go in the aggregate's test file; child entity tests in their own file — but **all operations still go through the root**
- Always search for an existing test file before creating one

## Test Naming

Write test names as **plain English statements of fact**, words separated by underscores, Each Word Capitalized:

- State facts, not wishes: "is", never "should be"
- No rigid `[Method]_[Scenario]_[Result]` patterns; no `Should_` prefixes
- Business-rule language: `Must` for validations
- For complex entities, prefix with the method name + `__` separator

```csharp
public void Publish()                       // happy path
public void Publish__Add_Event()            // domain event verification
public void Publish__Must_Be_Draft()        // state guard (DataRow over rejected states)
public void Close__Must_Be_Open()           // state guard
public void Target_Count_Must_Be_Positive() // value object validation
public void New_Publication_Is_Draft()      // initial state
public void Add_Slot_Request__Ranks_In_Selection_Order()
```

## Test Structure

Section markers instead of Arrange/Act/Assert — these are the **only comments allowed in test code**:

- `//given` + `//when` + `//then` — action performed and result verified
- `//given` + `//expected` — initial-state assertion only, no action

```csharp
[TestMethod]
public void Publish()
{
    //given
    var publication = PublicationFakeBuilder.BuildDraft();

    //when
    publication.Publish();

    //then
    publication.IsPublished.ShouldBeTrue();
}

[TestMethod]
public void New_Publication_Is_Draft()
{
    //given
    var publication = PublicationFakeBuilder.BuildDraft();

    //expected
    publication.IsDraft.ShouldBeTrue();
}
```

### Exception Tests

Always `//when` + `var act` + `//then` + `Should.Throw<>(act)` — never inline lambdas, never `//expected`:

```csharp
[TestMethod]
public void Close__Must_Be_Open()
{
    //given
    var publication = PublicationFakeBuilder.BuildDraft();
    var closedAtUtc = Faker.FakeUtcDate();

    //when
    var act = () => publication.Close(closedAtUtc);

    //then
    Should.Throw<PublicationMustBeOpenException>(act);
}
```

## Test Through Aggregate Roots Only

- **The aggregate root is the only entry point** — never instantiate a child entity directly in a test
- Child entities are internal implementation details; exercise them via the root's public methods
- Verify internal state through public properties or domain events — never expose state for testing

```csharp
// WRONG — instantiating the child directly
var request = SlotRequest.Create(slotId, SessionType.Single, null, 1);

// CORRECT — through the aggregate root
var submission = SubmissionFakeBuilder.BuildWithTarget(2);
var request = submission.AddSlotRequest(slot, SessionType.Single, null);
```

## Domain Event Assertions

Verify events via `UncommittedEvents` on the root. Name these tests `{Operation}__Add_Event`:

```csharp
[TestMethod]
public void Close__Add_Event()
{
    //given
    var publication = PublicationFakeBuilder.BuildOpen();
    var closedAtUtc = Faker.FakeUtcDate();

    //when
    publication.Close(closedAtUtc);

    //then
    publication
        .UncommittedEvents
        .OfType<PublicationClosed>()
        .Where(x => x.ClosedAtUtc == closedAtUtc)
        .ShouldHaveSingleItem();
}
```

## FakeBuilder Pattern

Each aggregate gets a static `{Entity}FakeBuilder` in `Domain.Test\Entities\Fake\` that creates the aggregate in every reachable state.

```csharp
public static class PublicationFakeBuilder
{
    public static readonly Dictionary<PublicationState, Func<Publication>> StateBuilders = new()
    {
        { PublicationState.Draft, BuildDraft },
        { PublicationState.Published, BuildPublished },
        { PublicationState.Open, BuildOpen },
        { PublicationState.Closed, BuildClosed }
    };

    public static Publication BuildDraft()
    {
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var window = SubmissionWindowFakeBuilder.BuildFutureWindow();

        return Publication.Create(weekSchedule, window);
    }

    public static Publication BuildPublished()
    {
        var publication = BuildDraft();
        publication.Publish();

        return publication;
    }

    public static Publication BuildOpen()
    {
        var publication = BuildPublished();
        publication.Open();

        return publication;
    }

    public static Publication BuildClosed()
    {
        var publication = BuildOpen();
        var closedAtUtc = Faker.FakeUtcDate();
        publication.Close(closedAtUtc);

        return publication;
    }
}
```

Key rules:
- `StateBuilders` must cover **every** value of the state enum
- Build methods chain along the real state progression — `BuildClosed()` calls `BuildOpen()`; never construct a state artificially
- All data inside builders comes from `Faker`
- When tests need both the aggregate and a child it owns, add an extension method returning a tuple:
  ```csharp
  public static (Submission Submission, SlotRequest Request) AddFakeSlotRequest(this Submission submission)
  ```

## Parameterized Tests with [DataRow]

### State guards rejecting 2+ states

One test with a `[DataRow]` per rejected state — never separate methods per state when the exception is the same:

```csharp
[TestMethod]
[DataRow(PublicationState.Published)]
[DataRow(PublicationState.Open)]
[DataRow(PublicationState.Closed)]
public void Publish__Must_Be_Draft(PublicationState state)
{
    //given
    var publication = PublicationFakeBuilder.StateBuilders[state]();

    //when
    var act = () => publication.Publish();

    //then
    Should.Throw<PublicationMustBeDraftException>(act);
}
```

When a guard rejects only one state, write a standalone test named `{Operation}__Must_Not_Be_{State}`.

### Happy paths allowed in 2+ states

When a method behaves identically in multiple states, one `[DataRow]` test named `{Operation}` — never a separate `{Operation}__Allowed_When_{State}` method.

When parameterizing across states, adjust assertions for differing starting conditions (e.g., `ShouldHaveSingleItem()` may need to become a specific-item check if one state's setup already adds items).

## Assertion Rules

**Assert values, not existence.** Values set up in `//given` must be compared exactly — `ShouldNotBeNull()` proves population, not correctness:

```csharp
// WRONG
submission.TargetCount.ShouldNotBeNull();

// RIGHT
var targetCount = TargetSessionCount.Of(3);
var submission = Submission.Create(publication, student, targetCount);
submission.TargetCount.ShouldBe(targetCount);
```

Existence checks are acceptable only for system-generated values (ids, timestamps) and null-guards in initial-state tests.

**Don't hide asserted values in builders.** Use the FakeBuilder for state setup, but create values being verified inline in the test so the assertion compares against visible input.

**Assert specific collection items, not counts:**

```csharp
// WRONG — counter arithmetic / existence
submission.SlotRequests.Count.ShouldBe(countBefore - 1);
submission.SlotRequests.ShouldNotBeEmpty();

// RIGHT — the specific item, by id
submission.SlotRequests.ShouldContain(x => x.Id == request.Id);
submission.SlotRequests.ShouldNotContain(x => x.Id == removedId);
```

## What to Test

**Test** (anything with a branch):
- State transitions and their guards (every `MustBe*` path)
- Domain events raised by every state change
- Value object validation rules (`TargetSessionCount.Of(0)` throws)
- Collection rules (ranking order, duplicate-slot rejection)
- Conditional business rules (e.g., transmission stamped silently when the teacher's fleet has one type)

**Don't test**:
- DTOs and requests/responses with only property assignments
- Factory methods that only construct without logic
- Logging, framework behavior, external libraries
- Private methods directly — they're covered through the public API
- Simple getters/constructor assignment

Cover 100% of logic branches in the domain layer.

## General Rules

- No comments in test code except section markers
- No underscore-prefixed private fields
- Never use default parameters in test helpers — pass all arguments explicitly
- Keep only necessary variables in private fields; prefer locals
- **Typed IDs in tests**: create identifiers via `{Entity}Id.New()` — never pass raw `Guid.NewGuid()` into a domain call. Raw `Guid` appears only where the test exercises the application boundary (an interactor's `ExecuteAsync(Guid id)`)
- Application-layer (interactor) tests use FakeItEasy for repository/query fakes:
  ```csharp
  [TestInitialize]
  public void Init()
  {
      repository = A.Fake<IPublicationRepository>();
      interactor = new ClosePublicationInteractor(repository, TimeProvider.System);
  }

  [TestMethod]
  public async Task Publication_Must_Exist()
  {
      //given
      var id = Guid.NewGuid();
      var publicationId = PublicationId.Of(id);

      A.CallTo(() => repository.GetAsync(publicationId))
          .Returns((Publication?)null);

      //when
      var act = () => interactor.ExecuteAsync(id);

      //then
      await Should.ThrowAsync<PublicationNotFoundException>(act);
  }
  ```
  Typed IDs are records, so FakeItEasy matches `repository.GetAsync(publicationId)` by value — the interactor's internally-converted `PublicationId.Of(id)` is equal to the test's.
