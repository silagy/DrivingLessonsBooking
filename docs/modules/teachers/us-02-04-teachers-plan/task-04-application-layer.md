# Task 4 of 12: Application layer — interactors, queries, DTOs, exceptions, DI

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–3 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** Four command interactors, two query interactors, `ITeacherQueries`, `IUnitOfWork`, not-found exceptions, DI registrations. One class per operation, `ExecuteAsync`, value objects created at the boundary, raw `Guid` only at entry/exit.

**Locked decisions:** `IUnitOfWork` lives in `Application\Common\` and is injected into interactors as its own dependency (repositories expose no `UnitOfWork` property). No application-layer tests this slice. US-01's `Handle`/primary-ctor style is confined to the auth slice — aggregate interactors follow the rules exactly.

---

**Files:**
- Create: `src/DrivingLessons.Application/Common/IUnitOfWork.cs`
- Create: `src/DrivingLessons.Application/Common/Exceptions/TeacherNotFoundException.cs`, `CarNotFoundException.cs`
- Create: `src/DrivingLessons.Application/Commands/CreateTeacher/` (`CreateTeacherRequest.cs`, `CreateTeacherResponse.cs`, `CreateTeacherInteractor.cs`)
- Create: `src/DrivingLessons.Application/Commands/AddCar/` (`AddCarRequest.cs`, `AddCarResponse.cs`, `AddCarInteractor.cs`)
- Create: `src/DrivingLessons.Application/Commands/ChangeTeacherDetails/` (`ChangeTeacherDetailsRequest.cs`, `ChangeTeacherDetailsInteractor.cs`)
- Create: `src/DrivingLessons.Application/Commands/ChangeCarDetails/` (`ChangeCarDetailsRequest.cs`, `ChangeCarDetailsInteractor.cs`)
- Create: `src/DrivingLessons.Application/Queries/ITeacherQueries.cs`
- Create: `src/DrivingLessons.Application/Queries/GetTeacher/` (`GetTeacherResponse.cs`, `GetTeacherInteractor.cs`)
- Create: `src/DrivingLessons.Application/Queries/FindTeachers/` (`ItemForFindTeachersResponse.cs`, `FindTeachersInteractor.cs`)
- Modify: `src/DrivingLessons.Application/DependencyInjection.cs`

- [ ] **Step 1: `src/DrivingLessons.Application/Common/IUnitOfWork.cs`**

```csharp
namespace DrivingLessons.Application.Common;

public interface IUnitOfWork
{
    Task CommitAsync();
}
```

- [ ] **Step 2: Not-found exceptions**

`src/DrivingLessons.Application/Common/Exceptions/TeacherNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class TeacherNotFoundException : NotFoundException
{
    public TeacherNotFoundException(TeacherId id)
        : base($"Teacher {id.Value} was not found.")
    {
    }
}
```

`src/DrivingLessons.Application/Common/Exceptions/CarNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class CarNotFoundException : NotFoundException
{
    public CarNotFoundException(CarId id)
        : base($"Car {id.Value} was not found.")
    {
    }
}
```

- [ ] **Step 3: CreateTeacher command**

`src/DrivingLessons.Application/Commands/CreateTeacher/CreateTeacherRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateTeacher;

public record CreateTeacherRequest(string Name, string ContactEmail);
```

`src/DrivingLessons.Application/Commands/CreateTeacher/CreateTeacherResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateTeacher;

public record CreateTeacherResponse(Guid Id);
```

`src/DrivingLessons.Application/Commands/CreateTeacher/CreateTeacherInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateTeacher;

public class CreateTeacherInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public CreateTeacherInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateTeacherResponse> ExecuteAsync(CreateTeacherRequest request)
    {
        var name = TeacherName.Of(request.Name);
        var contactEmail = Email.Of(request.ContactEmail);
        var teacher = Teacher.Create(name, contactEmail);

        repository.Add(teacher);

        await unitOfWork.CommitAsync();

        return new CreateTeacherResponse(teacher.Id.Value);
    }
}
```

- [ ] **Step 4: AddCar command**

`src/DrivingLessons.Application/Commands/AddCar/AddCarRequest.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.AddCar;

public record AddCarRequest(string Name, string Type, Transmission Transmission);
```

`src/DrivingLessons.Application/Commands/AddCar/AddCarResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.AddCar;

public record AddCarResponse(Guid Id);
```

`src/DrivingLessons.Application/Commands/AddCar/AddCarInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.AddCar;

public class AddCarInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public AddCarInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<AddCarResponse> ExecuteAsync(Guid id, AddCarRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        var car = teacher.AddCar(name, type, request.Transmission);

        await unitOfWork.CommitAsync();

        return new AddCarResponse(car.Id.Value);
    }
}
```

- [ ] **Step 5: ChangeTeacherDetails command**

`src/DrivingLessons.Application/Commands/ChangeTeacherDetails/ChangeTeacherDetailsRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeTeacherDetails;

public record ChangeTeacherDetailsRequest(string Name, string ContactEmail);
```

`src/DrivingLessons.Application/Commands/ChangeTeacherDetails/ChangeTeacherDetailsInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeTeacherDetails;

public class ChangeTeacherDetailsInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeTeacherDetailsInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeTeacherDetailsRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var name = TeacherName.Of(request.Name);
        var contactEmail = Email.Of(request.ContactEmail);
        teacher.ChangeDetails(name, contactEmail);

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 6: ChangeCarDetails command**

`src/DrivingLessons.Application/Commands/ChangeCarDetails/ChangeCarDetailsRequest.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeCarDetails;

public record ChangeCarDetailsRequest(string Name, string Type, Transmission Transmission);
```

`src/DrivingLessons.Application/Commands/ChangeCarDetails/ChangeCarDetailsInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeCarDetails;

public class ChangeCarDetailsInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeCarDetailsInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid carId, ChangeCarDetailsRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var resolvedCarId = CarId.Of(carId);
        var car = teacher.Cars.FirstOrDefault(x => x.Id == resolvedCarId)
                  ?? throw new CarNotFoundException(resolvedCarId);

        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        teacher.ChangeCarDetails(car, name, type, request.Transmission);

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 7: `src/DrivingLessons.Application/Queries/ITeacherQueries.cs`**

```csharp
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;

namespace DrivingLessons.Application.Queries;

public interface ITeacherQueries
{
    Task<GetTeacherResponse?> GetAsync(Guid id);

    Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync();
}
```

- [ ] **Step 8: GetTeacher query**

`src/DrivingLessons.Application/Queries/GetTeacher/GetTeacherResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetTeacher;

public class GetTeacherResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public IReadOnlyCollection<CarForGetTeacherResponse> Cars { get; init; } = [];

    public static Expression<Func<Teacher, GetTeacherResponse>> Selector =>
        x => new GetTeacherResponse
        {
            Id = x.Id.Value,
            Name = x.Name.Value,
            ContactEmail = x.ContactEmail.Value,
            Cars = x.Cars
                       .Select(car => new CarForGetTeacherResponse
                       {
                           Id = car.Id.Value,
                           Name = car.Name.Value,
                           Type = car.Type.Value,
                           Transmission = car.Transmission
                       })
                       .ToList()
        };
}

public class CarForGetTeacherResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
}
```

`src/DrivingLessons.Application/Queries/GetTeacher/GetTeacherInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetTeacher;

public class GetTeacherInteractor
{
    private readonly ITeacherQueries queries;

    public GetTeacherInteractor(ITeacherQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetTeacherResponse> ExecuteAsync(Guid id)
    {
        var teacher = await queries.GetAsync(id);

        if (teacher is null)
        {
            var teacherId = TeacherId.Of(id);
            throw new TeacherNotFoundException(teacherId);
        }

        return teacher;
    }
}
```

- [ ] **Step 9: FindTeachers query**

`src/DrivingLessons.Application/Queries/FindTeachers/ItemForFindTeachersResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindTeachers;

public class ItemForFindTeachersResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public IReadOnlyCollection<CarForFindTeachersResponse> Cars { get; init; } = [];

    public static Expression<Func<Teacher, ItemForFindTeachersResponse>> Selector =>
        x => new ItemForFindTeachersResponse
        {
            Id = x.Id.Value,
            Name = x.Name.Value,
            ContactEmail = x.ContactEmail.Value,
            Cars = x.Cars
                       .Select(car => new CarForFindTeachersResponse
                       {
                           Id = car.Id.Value,
                           Name = car.Name.Value,
                           Type = car.Type.Value,
                           Transmission = car.Transmission
                       })
                       .ToList()
        };
}

public class CarForFindTeachersResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
}
```

`src/DrivingLessons.Application/Queries/FindTeachers/FindTeachersInteractor.cs`:

```csharp
namespace DrivingLessons.Application.Queries.FindTeachers;

public class FindTeachersInteractor
{
    private readonly ITeacherQueries queries;

    public FindTeachersInteractor(ITeacherQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> ExecuteAsync()
    {
        return await queries.FindAsync();
    }
}
```

- [ ] **Step 10: Register the interactors — `src/DrivingLessons.Application/DependencyInjection.cs`**

Add the six registrations to the existing `AddApplication` method (keep the `LoginInteractor` line):

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.AddCar;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.ChangeTeacherDetails;
using DrivingLessons.Application.Commands.CreateTeacher;
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginInteractor>();
        services.AddScoped<CreateTeacherInteractor>();
        services.AddScoped<AddCarInteractor>();
        services.AddScoped<ChangeTeacherDetailsInteractor>();
        services.AddScoped<ChangeCarDetailsInteractor>();
        services.AddScoped<GetTeacherInteractor>();
        services.AddScoped<FindTeachersInteractor>();
        return services;
    }
}
```

- [ ] **Step 11: Verify + commit**

Run: `dotnet build` — expected: success.
Run: `dotnet test` — expected: all tests still PASS.

```bash
git add src
git commit -m "feat(app): teacher command and query interactors"
```

---

**Next:** [task-05-infrastructure-restructure.md](task-05-infrastructure-restructure.md)
