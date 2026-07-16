# Task 3 of 10: Student aggregate

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\Student.cs`
- Create: `src\DrivingLessons.Domain\Events\StudentCreated.cs`, `StudentUpdatedFromRoster.cs`, `StudentDeactivated.cs`, `StudentReactivated.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentAlreadyDeactivatedException.cs`, `StudentAlreadyActiveException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`, `tests\DrivingLessons.Domain.Test\Entities\Fake\StudentFakeBuilder.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Common\Faker.cs`

Before coding, open `src\DrivingLessons.Domain\Entities\Teacher.cs`, `src\DrivingLessons.Domain\Exceptions\TeacherAlreadyDeletedException.cs`, `tests\DrivingLessons.Domain.Test\Entities\TeacherTest.cs` and `tests\DrivingLessons.Domain.Test\Entities\Fake\TeacherFakeBuilder.cs` and match their exact shape (parameterless private EF ctor, private full ctor that raises the created event via `AddEvent`, static `Create` factory, private `MustBe*` guards).

Design notes (planning decisions 8–9):

- **`NationalId` is immutable** — no method changes it after creation; it is the upsert key for roster imports.
- **`UpdateFromRoster` is one full-overwrite method** — the roster is the source of truth, so there are no granular field setters and **no same-value guard** (it is a property update, not a state transition; mirror `Teacher.ChangeDetails`).
- **`Deactivate`/`Reactivate` are guarded transitions** — re-applying throws (operations are not idempotent). Reactivation-on-reappearance is orchestrated later by the import interactor.
- **Transmission is NOT stored** — it derives from the assigned `Car` (decision #18).
- The `Guid` in the exception messages is the aggregate id — fine to include (not PII), mirroring `TeacherAlreadyDeletedException`.
- `Student` has a boolean lifecycle (`IsActive`), not a state enum — so the fake builder has no `StateBuilders` dictionary; it is instance-based (unlike the static `TeacherFakeBuilder`) so later application-layer tests can pin the teacher/car via `WithTeacher`/`WithCar`. `BuildInactive()` builds active then calls `Deactivate()` — never construct state artificially.

- [ ] **Step 1: Write failing tests**

Add to `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (inside the existing class):

```csharp
public static string FakePhoneNumber()
{
    var prefixDigit = Random.Shared.Next(0, 10);
    var subscriber = Random.Shared.Next(1000000, 10000000);

    return $"05{prefixDigit}-{subscriber}";
}

public static DateOnly FakeDate()
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    var daysAhead = Random.Shared.Next(1, 365);

    return today.AddDays(daysAhead);
}
```

`tests\DrivingLessons.Domain.Test\Entities\Fake\StudentFakeBuilder.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public class StudentFakeBuilder
{
    private Teacher? teacher;
    private Car? car;

    public StudentFakeBuilder WithTeacher(Teacher teacher)
    {
        this.teacher = teacher;

        return this;
    }

    public StudentFakeBuilder WithCar(Car car)
    {
        this.car = car;

        return this;
    }

    public Student Build()
    {
        var resolvedTeacher = teacher ?? TeacherFakeBuilder.Build();
        var resolvedCar = car ?? CarFakeBuilder.Build();
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        return Student.Create(
            nationalId,
            name,
            phone,
            resolvedTeacher,
            resolvedCar,
            address,
            startDate,
            licenseType);
    }

    public Student BuildInactive()
    {
        var student = Build();
        student.Deactivate();

        return student;
    }
}
```

`tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`:

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
public class StudentTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        //then
        student.NationalId.ShouldBe(nationalId);
        student.Name.ShouldBe(name);
        student.Phone.ShouldBe(phone);
        student.TeacherId.ShouldBe(teacher.Id);
        student.CarId.ShouldBe(car.Id);
        student.Address.ShouldBe(address);
        student.StartDate.ShouldBe(startDate);
        student.LicenseType.ShouldBe(licenseType);
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        //then
        student
            .UncommittedEvents
            .OfType<StudentCreated>()
            .Where(x => x.StudentId == student.Id
                        && x.NationalId == nationalId
                        && x.Name == name
                        && x.TeacherId == teacher.Id
                        && x.CarId == car.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Update_From_Roster()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newAddress = Address.Of(Faker.FakeString());
        var newStartDate = LessonsStartDate.Of(Faker.FakeDate());
        var newLicenseType = LicenseType.Of(Faker.FakeString());

        //when
        student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, newAddress, newStartDate, newLicenseType);

        //then
        student.Name.ShouldBe(newName);
        student.Phone.ShouldBe(newPhone);
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(newCar.Id);
        student.Address.ShouldBe(newAddress);
        student.StartDate.ShouldBe(newStartDate);
        student.LicenseType.ShouldBe(newLicenseType);
    }

    [TestMethod]
    public void Update_From_Roster__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newAddress = Address.Of(Faker.FakeString());
        var newStartDate = LessonsStartDate.Of(Faker.FakeDate());
        var newLicenseType = LicenseType.Of(Faker.FakeString());

        //when
        student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, newAddress, newStartDate, newLicenseType);

        //then
        student
            .UncommittedEvents
            .OfType<StudentUpdatedFromRoster>()
            .Where(x => x.StudentId == student.Id
                        && x.TeacherId == newTeacher.Id
                        && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Deactivate()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        student.Deactivate();

        //then
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Deactivate__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        student.Deactivate();

        //then
        student
            .UncommittedEvents
            .OfType<StudentDeactivated>()
            .Where(x => x.StudentId == student.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Deactivate__Must_Not_Be_Inactive()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        var act = () => student.Deactivate();

        //then
        Should.Throw<StudentAlreadyDeactivatedException>(act);
    }

    [TestMethod]
    public void Reactivate()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        student.Reactivate();

        //then
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Reactivate__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        student.Reactivate();

        //then
        student
            .UncommittedEvents
            .OfType<StudentReactivated>()
            .Where(x => x.StudentId == student.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Reactivate__Must_Not_Be_Active()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        var act = () => student.Reactivate();

        //then
        Should.Throw<StudentAlreadyActiveException>(act);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL (compilation error) — `Student`, its events and its exceptions do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Entities\Student.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Student : AggregateRoot<StudentId>
{
    public NationalId NationalId { get; private set; }
    public StudentName Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public TeacherId TeacherId { get; private set; }
    public CarId CarId { get; private set; }
    public Address? Address { get; private set; }
    public LessonsStartDate? StartDate { get; private set; }
    public LicenseType? LicenseType { get; private set; }
    public bool IsActive { get; private set; }

    private Student()
    {
    }

    private Student(
        StudentId id,
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        TeacherId teacherId,
        CarId carId,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType,
        bool isActive)
        : base(id)
    {
        NationalId = nationalId;
        Name = name;
        Phone = phone;
        TeacherId = teacherId;
        CarId = carId;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;
        IsActive = isActive;

        var createdEvent = new StudentCreated(id, nationalId, name, teacherId, carId);
        AddEvent(createdEvent);
    }

    public static Student Create(
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        Teacher teacher,
        Car car,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        const bool isActive = true;
        var id = StudentId.New();

        return new Student(
            id,
            nationalId,
            name,
            phone,
            teacher.Id,
            car.Id,
            address,
            startDate,
            licenseType,
            isActive);
    }

    public void UpdateFromRoster(
        StudentName name,
        PhoneNumber phone,
        Teacher teacher,
        Car car,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        Name = name;
        Phone = phone;
        TeacherId = teacher.Id;
        CarId = car.Id;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentUpdatedFromRoster(Id, teacher.Id, car.Id));
    }

    public void Deactivate()
    {
        MustBeActive();

        IsActive = false;

        AddEvent(new StudentDeactivated(Id));
    }

    public void Reactivate()
    {
        MustBeInactive();

        IsActive = true;

        AddEvent(new StudentReactivated(Id));
    }

    private void MustBeActive()
    {
        if (!IsActive)
        {
            throw new StudentAlreadyDeactivatedException(Id);
        }
    }

    private void MustBeInactive()
    {
        if (IsActive)
        {
            throw new StudentAlreadyActiveException(Id);
        }
    }
}
```

`src\DrivingLessons.Domain\Events\StudentCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentCreated(
    StudentId StudentId,
    NationalId NationalId,
    StudentName Name,
    TeacherId TeacherId,
    CarId CarId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\StudentUpdatedFromRoster.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentUpdatedFromRoster(StudentId StudentId, TeacherId TeacherId, CarId CarId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\StudentDeactivated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentDeactivated(StudentId StudentId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\StudentReactivated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentReactivated(StudentId StudentId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\StudentAlreadyDeactivatedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyDeactivatedException : DomainException
{
    public StudentAlreadyDeactivatedException(StudentId id)
        : base($"Student {id.Value} is already deactivated.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\StudentAlreadyActiveException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyActiveException : DomainException
{
    public StudentAlreadyActiveException(StudentId id)
        : base($"Student {id.Value} is already active.")
    {
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): add Student aggregate with roster-driven lifecycle"
```

---

**Next:** [task-04-roster-import-aggregate.md](task-04-roster-import-aggregate.md)
