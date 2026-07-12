# Task 3 of 10: Application layer — commands, query, exceptions, DI

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–2 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Application\Commands\CreateWeekSchedule\CreateWeekScheduleInteractor.cs`, `CreateWeekScheduleRequest.cs`, `CreateWeekScheduleResponse.cs`
- Create: `src\DrivingLessons.Application\Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\MarkSlotAvailable\MarkSlotAvailableInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\IWeekScheduleQueries.cs`, `Queries\GetWeekSchedule\GetWeekScheduleInteractor.cs`, `GetWeekScheduleResponse.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\WeekScheduleNotFoundException.cs`, `SlotNotFoundException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\WeekScheduleAlreadyExistsException.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`

There is no Application test project — this layer is verified by `dotnet build` and the API smoke test in Task 5. Before coding, open `CreateTeacherInteractor.cs`, `RemoveCarInteractor.cs`, `GetTeacherInteractor.cs`, `GetTeacherResponse.cs` and match their exact style (ctor field order, `??` alignment).

- [ ] **Step 1: Implement commands**

`src\DrivingLessons.Application\Commands\CreateWeekSchedule\CreateWeekScheduleRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateWeekSchedule;

public record CreateWeekScheduleRequest(Guid TeacherId, DateOnly WeekStart);
```

`src\DrivingLessons.Application\Commands\CreateWeekSchedule\CreateWeekScheduleResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateWeekSchedule;

public record CreateWeekScheduleResponse(Guid Id);
```

`src\DrivingLessons.Application\Commands\CreateWeekSchedule\CreateWeekScheduleInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateWeekSchedule;

public class CreateWeekScheduleInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateWeekScheduleInteractor(
        IWeekScheduleRepository repository,
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateWeekScheduleResponse> ExecuteAsync(CreateWeekScheduleRequest request)
    {
        var teacherId = TeacherId.Of(request.TeacherId);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var weekStart = WeekStart.Of(request.WeekStart);

        var existingWeekSchedule = await repository.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (existingWeekSchedule is not null)
        {
            throw new WeekScheduleAlreadyExistsException(teacherId, weekStart);
        }

        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        repository.Add(weekSchedule);
        await unitOfWork.CommitAsync();

        return new CreateWeekScheduleResponse(weekSchedule.Id.Value);
    }
}
```

> `TeacherNotFoundException`'s existing constructor signature wins — check it before use.

`src\DrivingLessons.Domain\Exceptions\WeekScheduleAlreadyExistsException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class WeekScheduleAlreadyExistsException : DomainException
{
    public WeekScheduleAlreadyExistsException(TeacherId teacherId, WeekStart weekStart)
        : base($"Week schedule for teacher {teacherId.Value} and week {weekStart.Value:yyyy-MM-dd} already exists.")
    {
    }
}
```

`src\DrivingLessons.Application\Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotUnavailable;

public class MarkSlotUnavailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public MarkSlotUnavailableInteractor(IWeekScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotUnavailable(slot);

        await unitOfWork.CommitAsync();
    }
}
```

`src\DrivingLessons.Application\Commands\MarkSlotAvailable\MarkSlotAvailableInteractor.cs` — identical shape; the two changed lines:

```csharp
namespace DrivingLessons.Application.Commands.MarkSlotAvailable;

public class MarkSlotAvailableInteractor
```
and the domain call is `weekSchedule.MarkSlotAvailable(slot);` — everything else matches `MarkSlotUnavailableInteractor` verbatim (same usings, fields, constructor, resolve-or-throw lines).

- [ ] **Step 2: Implement not-found exceptions**

`src\DrivingLessons.Application\Common\Exceptions\WeekScheduleNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class WeekScheduleNotFoundException : NotFoundException
{
    public WeekScheduleNotFoundException(WeekScheduleId id)
        : base($"Week schedule {id.Value} was not found.")
    {
    }

    public WeekScheduleNotFoundException(Guid teacherId, DateOnly weekStart)
        : base($"Week schedule for teacher {teacherId} and week {weekStart:yyyy-MM-dd} was not found.")
    {
    }
}
```

`src\DrivingLessons.Application\Common\Exceptions\SlotNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class SlotNotFoundException : NotFoundException
{
    public SlotNotFoundException(WeekScheduleId weekScheduleId, SlotId slotId)
        : base($"Slot {slotId.Value} was not found in week schedule {weekScheduleId.Value}.")
    {
    }
}
```

> Match the exact message phrasing of the existing `TeacherNotFoundException` ("was not found" vs other wording) — the existing file wins.

- [ ] **Step 3: Implement query side**

`src\DrivingLessons.Application\Queries\IWeekScheduleQueries.cs`:

```csharp
using DrivingLessons.Application.Queries.GetWeekSchedule;

namespace DrivingLessons.Application.Queries;

public interface IWeekScheduleQueries
{
    Task<GetWeekScheduleResponse?> GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart);
}
```

`src\DrivingLessons.Application\Queries\GetWeekSchedule\GetWeekScheduleResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleResponse
{
    public Guid Id { get; init; }
    public Guid TeacherId { get; init; }
    public DateOnly WeekStart { get; init; }
    public IReadOnlyCollection<SlotForGetWeekScheduleResponse> Slots { get; init; } = [];

    public static Expression<Func<WeekSchedule, GetWeekScheduleResponse>> Selector =>
        x => new GetWeekScheduleResponse
        {
            Id = x.Id.Value,
            TeacherId = x.TeacherId.Value,
            WeekStart = x.WeekStart.Value,
            Slots = x.Slots
                     .OrderBy(slot => slot.Day)
                     .ThenBy(slot => slot.Window)
                     .Select(slot => new SlotForGetWeekScheduleResponse
                     {
                         Id = slot.Id.Value,
                         Day = slot.Day,
                         Window = slot.Window,
                         State = slot.State
                     })
                     .ToList()
        };
}

public class SlotForGetWeekScheduleResponse
{
    public Guid Id { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public TimeOnly StartLocal => SlotWindowTimes.StartOf(Window);
    public TimeOnly EndLocal => SlotWindowTimes.EndOf(Window);
}
```

> If the teachers module keeps nested response classes in the same file (`GetTeacherResponse.cs` with `CarForGetTeacherResponse`), keep both classes in this one file the same way; if they are separate files, split them.

`src\DrivingLessons.Application\Queries\GetWeekSchedule\GetWeekScheduleInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleInteractor
{
    private readonly IWeekScheduleQueries queries;

    public GetWeekScheduleInteractor(IWeekScheduleQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetWeekScheduleResponse> ExecuteAsync(Guid teacherId, DateOnly weekStart)
    {
        var weekSchedule = await queries.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (weekSchedule is null)
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        return weekSchedule;
    }
}
```

- [ ] **Step 4: Register interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, next to the teacher interactors add:

```csharp
services.AddScoped<CreateWeekScheduleInteractor>();
services.AddScoped<MarkSlotUnavailableInteractor>();
services.AddScoped<MarkSlotAvailableInteractor>();
services.AddScoped<GetWeekScheduleInteractor>();
```

- [ ] **Step 5: Build + run all tests**

Run: `dotnet build` then `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: build succeeds, all tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Domain src/DrivingLessons.Application
git commit -m "feat(application): create week schedule and slot toggle interactors"
```

---

**Next:** [task-04-infrastructure.md](task-04-infrastructure.md)
