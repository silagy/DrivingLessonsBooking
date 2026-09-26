# Task 2 of 6: Link query — interface, response DTO, interactor, EF implementation

> Part of [US-23: Public Link Gateway](README.md). Requires task 1 complete. Work on branch `24-us-23-public-link-gateway`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Application\Common\Exceptions\PublicationLinkNotFoundException.cs`
- Create: `src\DrivingLessons.Application\Queries\GetPublicationByLink\GetPublicationByLinkResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\GetPublicationByLink\GetPublicationByLinkInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IPublicationQueries.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\GetPublicationByLinkInteractorTest.cs` (new folder `Queries\`)

**Interfaces:**
- Consumes: `WeekStart.WeekNumber` (task 1); existing `ShareableLinkToken.Of(string)`, `PublicationState`.
- Produces (task 3 relies on these exact names):
  - `GetPublicationByLinkInteractor.ExecuteAsync(string linkToken) : Task<GetPublicationByLinkResponse>` — throws `PublicationLinkNotFoundException` (a `NotFoundException` → 404) when no non-draft publication matches.
  - `GetPublicationByLinkResponse { DateOnly WeekStart; int WeekNumber; PublicationState State; DateTimeOffset WindowStartUtc; DateTimeOffset WindowEndUtc; }`
  - `IPublicationQueries.GetByLinkTokenExcludingDraftsAsync(string linkToken) : Task<GetPublicationByLinkResponse?>`

The interface method and its EF implementation land together: `DrivingLessons.Application.Test` references Infrastructure, so the test project does not compile until `PublicationQueries` implements the new member. Precedents to open before coding: `Queries\GetPublication\GetPublicationInteractor.cs` + `GetPublicationResponse.cs` (query interactor and `Selector` shape), `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs` (FakeItEasy setup).

- [x] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Queries\GetPublicationByLinkInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationByLink;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetPublicationByLinkInteractorTest
{
    private IPublicationQueries queries = null!;
    private GetPublicationByLinkInteractor interactor = null!;
    private string linkToken = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IPublicationQueries>();
        interactor = new GetPublicationByLinkInteractor(queries);
        linkToken = ShareableLinkToken.New().Value;
    }

    [TestMethod]
    public async Task Returns_The_Publication_For_The_Link()
    {
        //given
        var publication = new GetPublicationByLinkResponse
        {
            WeekStart = new DateOnly(2026, 6, 14),
            WeekNumber = 25,
            State = PublicationState.Published,
            WindowStartUtc = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero),
            WindowEndUtc = new DateTimeOffset(2026, 6, 12, 11, 0, 0, TimeSpan.Zero)
        };

        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns(publication);

        //when
        var response = await interactor.ExecuteAsync(linkToken);

        //then
        response.ShouldBeSameAs(publication);
    }

    [TestMethod]
    public async Task Publication_Must_Exist_For_The_Link()
    {
        //given
        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(linkToken);

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
    }

    [TestMethod]
    public async Task Not_Found_Message_Does_Not_Reveal_The_Link_Token()
    {
        //given
        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(linkToken);

        //then
        var exception = await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
        exception.Message.ShouldNotContain(linkToken);
    }
}
```

The third test pins README decision 3: the token is a capability, and the exception message becomes the ProblemDetails `detail` returned to the caller.

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetPublicationByLinkInteractorTest"`
Expected: build FAILS — `GetPublicationByLink` namespace, `GetPublicationByLinkResponse`, `PublicationLinkNotFoundException`, and `GetByLinkTokenExcludingDraftsAsync` do not exist.

- [x] **Step 3: Not-found exception**

`src\DrivingLessons.Application\Common\Exceptions\PublicationLinkNotFoundException.cs`:

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class PublicationLinkNotFoundException : NotFoundException
{
    public PublicationLinkNotFoundException()
        : base("No publication is available for this link.")
    {
    }
}
```

A separate class (not a `PublicationNotFoundException` overload) because the only identifier available is the token, and it must not be echoed.

- [x] **Step 4: Response DTO**

`src\DrivingLessons.Application\Queries\GetPublicationByLink\GetPublicationByLinkResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationByLink;

public class GetPublicationByLinkResponse
{
    public DateOnly WeekStart { get; init; }
    public int WeekNumber { get; init; }
    public PublicationState State { get; init; }
    public DateTimeOffset WindowStartUtc { get; init; }
    public DateTimeOffset WindowEndUtc { get; init; }

    public static Expression<Func<Publication, GetPublicationByLinkResponse>> Selector =>
        x => new GetPublicationByLinkResponse
        {
            WeekStart = x.WeekStart.Value,
            WeekNumber = x.WeekStart.WeekNumber,
            State = x.State,
            WindowStartUtc = x.Window!.StartUtc,
            WindowEndUtc = x.Window!.EndUtc
        };
}
```

Window fields are non-nullable: every non-draft publication has a window (`Publication.Publish` sets it, and the query excludes drafts). `x.WeekStart.WeekNumber` is evaluated client-side in the top-level projection — EF reads the converted `week_start` column, rehydrates `WeekStart` through `WeekStartConverter`, then calls the getter (same mechanism as the existing `x.WeekStart.Value` projections). Task 3's smoke test asserts `weekNumber` in the real response. No publication id and no token are exposed.

- [x] **Step 5: Query interface member**

In `src\DrivingLessons.Application\Queries\IPublicationQueries.cs`, add the using (keep usings sorted):

```csharp
using DrivingLessons.Application.Queries.GetPublicationByLink;
```

and add this member after `GetByWeekAsync`:

```csharp
    Task<GetPublicationByLinkResponse?> GetByLinkTokenExcludingDraftsAsync(string linkToken);
```

- [x] **Step 6: Interactor**

`src\DrivingLessons.Application\Queries\GetPublicationByLink\GetPublicationByLinkInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetPublicationByLink;

public class GetPublicationByLinkInteractor
{
    private readonly IPublicationQueries queries;

    public GetPublicationByLinkInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetPublicationByLinkResponse> ExecuteAsync(string linkToken)
    {
        var publication = await queries.GetByLinkTokenExcludingDraftsAsync(linkToken)
                          ?? throw new PublicationLinkNotFoundException();

        return publication;
    }
}
```

- [x] **Step 7: EF implementation**

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs`, add the using:

```csharp
using DrivingLessons.Application.Queries.GetPublicationByLink;
```

and add this method directly after `GetByWeekAsync`:

```csharp
    public async Task<GetPublicationByLinkResponse?> GetByLinkTokenExcludingDraftsAsync(string linkToken)
    {
        var resolvedLinkToken = ShareableLinkToken.Of(linkToken);

        return await dbContext
                         .Publications
                         .Where(x => x.LinkToken == resolvedLinkToken && x.State != PublicationState.Draft)
                         .Select(GetPublicationByLinkResponse.Selector)
                         .FirstOrDefaultAsync();
    }
```

`ShareableLinkToken` and `PublicationState` come from the already-imported `DrivingLessons.Domain.Values`. The token comparison reuses `ShareableLinkTokenConverter` exactly like `PublicationRepository.GetByLinkTokenAsync`; `link_token` is already indexed/unique from the publications migration, so no migration is needed.

- [x] **Step 8: Register the interactor**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using next to the other query usings:

```csharp
using DrivingLessons.Application.Queries.GetPublicationByLink;
```

and register it after `services.AddScoped<GetPublicationInteractor>();`:

```csharp
        services.AddScoped<GetPublicationByLinkInteractor>();
```

- [x] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetPublicationByLinkInteractorTest"`
Expected: 3 tests PASS.

Then: `dotnet build` and `dotnet test` — build clean, every test PASS.

- [x] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application src/DrivingLessons.Infrastructure/EntityFramework/Queries/PublicationQueries.cs tests/DrivingLessons.Application.Test/Queries
git commit -m "feat(app): query a non-draft publication by its link token

Student-safe projection (week, week number, state, window) with a
payload-free not-found so the capability token is never echoed."
```

---

**Next:** [task-03-anonymous-endpoint.md](task-03-anonymous-endpoint.md)
