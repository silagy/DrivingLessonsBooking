# Task 3 of 12: Teacher aggregate + Car child + events (TDD)

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–2 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** The `Teacher` aggregate root owning `Car` child entities, four domain events, the ownership guard, and `ITeacherRepository`. TDD through the aggregate root only — never instantiate `Car` directly in tests.

**Locked rules for this aggregate:** NO maximum-cars guard (product owner corrected the old "1–2 cars" requirement); minimum-one-car binds only on future car removal, so nothing enforces it here. No-op edits are accepted silently — `ChangeDetails` always mutates and raises its event. `ITeacherRepository` does **not** expose a `UnitOfWork` property.

---

**Files:**
- Create: `src/DrivingLessons.Domain/Entities/Teacher.cs`, `Car.cs`
- Create: `src/DrivingLessons.Domain/Events/TeacherCreated.cs`, `CarAdded.cs`, `TeacherDetailsChanged.cs`, `CarDetailsChanged.cs`
- Create: `src/DrivingLessons.Domain/Exceptions/CarNotInTeacherException.cs`
- Create: `src/DrivingLessons.Domain/Repositories/ITeacherRepository.cs`
- Test: `tests/DrivingLessons.Domain.Test/Entities/TeacherTest.cs`, `tests/DrivingLessons.Domain.Test/Entities/Fake/TeacherFakeBuilder.cs`

- [ ] **Step 1: Write the FakeBuilder — `tests/DrivingLessons.Domain.Test/Entities/Fake/TeacherFakeBuilder.cs`**

Deviation from the FakeBuilder template noted: `Teacher` has no state enum, so there is no `StateBuilders` dictionary.

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class TeacherFakeBuilder
{
    public static Teacher Build()
    {
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        return Teacher.Create(name, contactEmail);
    }

    public static (Teacher Teacher, Car Car) AddFakeCar(this Teacher teacher)
    {
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());
        var car = teacher.AddCar(name, type, Transmission.Automatic);

        return (teacher, car);
    }
}
```

- [ ] **Step 2: Write the failing tests — `tests/DrivingLessons.Domain.Test/Entities/TeacherTest.cs`**

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class TeacherTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
        teacher.Cars.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherCreated>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == name && x.ContactEmail == contactEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Add_Car()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = teacher.AddCar(name, type, Transmission.Manual);

        //then
        teacher.Cars.ShouldContain(x => x.Id == car.Id);
        car.Name.ShouldBe(name);
        car.Type.ShouldBe(type);
        car.Transmission.ShouldBe(Transmission.Manual);
    }

    [TestMethod]
    public void Add_Car_Has_No_Maximum()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        teacher.AddFakeCar();
        teacher.AddFakeCar();

        //when
        var (_, third) = teacher.AddFakeCar();

        //then
        teacher.Cars.ShouldContain(x => x.Id == third.Id);
    }

    [TestMethod]
    public void Add_Car__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = teacher.AddCar(name, type, Transmission.Automatic);

        //then
        teacher
            .UncommittedEvents
            .OfType<CarAdded>()
            .Where(x => x.TeacherId == teacher.Id && x.CarId == car.Id && x.Name == name)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher.Name.ShouldBe(newName);
        teacher.ContactEmail.ShouldBe(newEmail);
    }

    [TestMethod]
    public void Change_Details__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherDetailsChanged>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == newName && x.ContactEmail == newEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details__Accepts_Unchanged_Values()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());
        var teacher = Teacher.Create(name, contactEmail);

        //when
        teacher.ChangeDetails(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
    }

    [TestMethod]
    public void Change_Car_Details()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        teacher.ChangeCarDetails(car, newName, newType, Transmission.Manual);

        //then
        car.Name.ShouldBe(newName);
        car.Type.ShouldBe(newType);
        car.Transmission.ShouldBe(Transmission.Manual);
    }

    [TestMethod]
    public void Change_Car_Details__Add_Event()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        teacher.ChangeCarDetails(car, newName, newType, Transmission.Manual);

        //then
        teacher
            .UncommittedEvents
            .OfType<CarDetailsChanged>()
            .Where(x => x.TeacherId == teacher.Id && x.CarId == car.Id && x.Name == newName)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Car_Details__Car_Must_Be_In_Teacher()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var (_, foreignCar) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        var act = () => teacher.ChangeCarDetails(foreignCar, newName, newType, Transmission.Automatic);

        //then
        Should.Throw<CarNotInTeacherException>(act);
    }
}
```

- [ ] **Step 3: Run the tests — expected: compilation failures**

Run: `dotnet test`

- [ ] **Step 4: The four events — one file each in `src/DrivingLessons.Domain/Events/`**

`TeacherCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record TeacherCreated(TeacherId TeacherId, TeacherName Name, Email ContactEmail) : IDomainEvent;
```

`CarAdded.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarAdded(TeacherId TeacherId, CarId CarId, CarName Name, CarType Type, Transmission Transmission) : IDomainEvent;
```

`TeacherDetailsChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record TeacherDetailsChanged(TeacherId TeacherId, TeacherName Name, Email ContactEmail) : IDomainEvent;
```

`CarDetailsChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarDetailsChanged(TeacherId TeacherId, CarId CarId, CarName Name, CarType Type, Transmission Transmission) : IDomainEvent;
```

- [ ] **Step 5: `src/DrivingLessons.Domain/Exceptions/CarNotInTeacherException.cs`**

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class CarNotInTeacherException : DomainException
{
    public CarNotInTeacherException(TeacherId teacherId, CarId carId)
        : base($"Teacher {teacherId.Value} does not own car {carId.Value}.")
    {
    }
}
```

- [ ] **Step 6: `src/DrivingLessons.Domain/Entities/Car.cs`**

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Car : Entity<CarId>
{
    public CarName Name { get; private set; }
    public CarType Type { get; private set; }
    public Transmission Transmission { get; private set; }

    private Car()
    {
    }

    private Car(CarId id, CarName name, CarType type, Transmission transmission)
        : base(id)
    {
        Name = name;
        Type = type;
        Transmission = transmission;
    }

    internal static Car Create(CarName name, CarType type, Transmission transmission)
    {
        var id = CarId.New();
        return new Car(id, name, type, transmission);
    }

    internal void ChangeDetails(CarName name, CarType type, Transmission transmission)
    {
        Name = name;
        Type = type;
        Transmission = transmission;
    }
}
```

- [ ] **Step 7: `src/DrivingLessons.Domain/Entities/Teacher.cs`**

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Teacher : AggregateRoot<TeacherId>
{
    private readonly List<Car> cars = [];

    public TeacherName Name { get; private set; }
    public Email ContactEmail { get; private set; }

    public IReadOnlyCollection<Car> Cars => cars.AsReadOnly();

    private Teacher()
    {
    }

    private Teacher(TeacherId id, TeacherName name, Email contactEmail)
        : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;

        var createdEvent = new TeacherCreated(id, name, contactEmail);
        AddEvent(createdEvent);
    }

    public static Teacher Create(TeacherName name, Email contactEmail)
    {
        var id = TeacherId.New();

        return new Teacher(id, name, contactEmail);
    }

    public Car AddCar(CarName name, CarType type, Transmission transmission)
    {
        var car = Car.Create(name, type, transmission);
        cars.Add(car);

        AddEvent(new CarAdded(Id, car.Id, name, type, transmission));

        return car;
    }

    public void ChangeDetails(TeacherName name, Email contactEmail)
    {
        Name = name;
        ContactEmail = contactEmail;

        AddEvent(new TeacherDetailsChanged(Id, name, contactEmail));
    }

    public void ChangeCarDetails(Car car, CarName name, CarType type, Transmission transmission)
    {
        MustOwnCar(car);

        car.ChangeDetails(name, type, transmission);

        AddEvent(new CarDetailsChanged(Id, car.Id, name, type, transmission));
    }

    private void MustOwnCar(Car car)
    {
        if (!cars.Contains(car))
        {
            throw new CarNotInTeacherException(Id, car.Id);
        }
    }
}
```

- [ ] **Step 8: `src/DrivingLessons.Domain/Repositories/ITeacherRepository.cs`**

No `UnitOfWork` property — locked decision; interactors inject `IUnitOfWork` separately (task 4).

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ITeacherRepository
{
    Task<Teacher?> GetAsync(TeacherId id);

    void Add(Teacher teacher);
}
```

- [ ] **Step 9: Verify + commit**

Run: `dotnet test` — expected: all tests PASS.

```bash
git add src tests
git commit -m "feat(domain): teacher aggregate with owned cars and domain events"
```

---

**Next:** [task-04-application-layer.md](task-04-application-layer.md)
