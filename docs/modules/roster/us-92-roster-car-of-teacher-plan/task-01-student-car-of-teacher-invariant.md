# Task 1 of 4: A Student's Car must be one of their Teacher's Cars (domain)

> Part of [#92: Roster import stops deactivating Students and enforces that a Student's Car is one of their Teacher's Cars](README.md). Requires the plan commit. Work on branch `92-roster-car-of-teacher`. Read README decisions 3, 4 and 7 first.

**Files:**
- Modify: `src\DrivingLessons.Domain\Entities\Car.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Student.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentCarMustBeAssignedToTeacherException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\CarTest.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Entities\Fake\StudentFakeBuilder.cs`
- Modify: `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`, `ReviseSubmissionInteractorTest.cs`, `ImportRosterInteractorTest.cs` (setup only)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`

**Interfaces:**
- Consumes: existing `Car.AssignTeacher(Teacher)`, `Car.UnassignTeacher(Teacher)`, `Car.TeacherAssignments`, `CarFakeBuilder.Build()`, `CarFakeBuilder.AssignFakeTeacher(this Car car)` → `(Car Car, Teacher Teacher)`, `TeacherFakeBuilder.Build()`, `Faker.*`.
- Produces (tasks 2-3 rely on these):
  - `public bool Car.IsAssignedTo(Teacher teacher)`: true when the Car has a `TeacherAssignment` for `teacher.Id`. A Car may be assigned to many Teachers (shared Car).
  - `DrivingLessons.Domain.Exceptions.StudentCarMustBeAssignedToTeacherException(CarId carId, TeacherId teacherId) : DomainException`, mapped by `ApiExceptionFilter` to 409 with `code` `studentCarMustBeAssignedToTeacher`.
  - `Student.Create(...)` and `Student.UpdateFromRoster(...)` (signatures unchanged) throw it, before any state change or event, when `!car.IsAssignedTo(teacher)`.
  - `StudentFakeBuilder.Build()` / `BuildInactive()` always return a consistent Student: the builder assigns the resolved Car to the resolved Teacher when it isn't already.

**Why:** AC 2. The invariant belongs to the `Student` aggregate so every future path (manual create in slice 5, Change Teacher / Change Car in slice 6) inherits it. Landing it before task 3 is safe because the only production caller, `ImportRosterInteractor`, today passes whatever the file says; task 1 keeps its tests green by making their fixture consistent, and task 3 adds the per-row check so the guard never fires during an import.

- [ ] **Step 1: Write the failing `Car.IsAssignedTo` tests**

Add to `tests\DrivingLessons.Domain.Test\Entities\CarTest.cs`, after `Unassign_Teacher__Unassigned_Teacher_Cannot_Be_Unassigned_Again`:

```csharp
    [TestMethod]
    public void Is_Assigned_To_Assigned_Teacher()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        var isAssigned = car.IsAssignedTo(teacher);

        //then
        isAssigned.ShouldBeTrue();
    }

    [TestMethod]
    public void Is_Assigned_To_Each_Teacher_Of_A_Shared_Car()
    {
        //given
        var (car, first) = CarFakeBuilder.Build().AssignFakeTeacher();
        var second = TeacherFakeBuilder.Build();
        car.AssignTeacher(second);

        //when
        var isAssignedToFirst = car.IsAssignedTo(first);
        var isAssignedToSecond = car.IsAssignedTo(second);

        //then
        isAssignedToFirst.ShouldBeTrue();
        isAssignedToSecond.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Is_Not_Assigned_To_Other_Teacher(bool wasAssignedBefore)
    {
        //given
        var car = CarFakeBuilder.Build();
        var teacher = TeacherFakeBuilder.Build();

        if (wasAssignedBefore)
        {
            car.AssignTeacher(teacher);
            car.UnassignTeacher(teacher);
        }

        //when
        var isAssigned = car.IsAssignedTo(teacher);

        //then
        isAssigned.ShouldBeFalse();
    }
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~CarTest"`
Expected: build error `'Car' does not contain a definition for 'IsAssignedTo'`.

- [ ] **Step 3: Add `IsAssignedTo` to `Car`**

In `src\DrivingLessons.Domain\Entities\Car.cs`, add after `UnassignTeacher`:

```csharp
    public bool IsAssignedTo(Teacher teacher)
    {
        return teacherAssignments.Any(x => x.TeacherId == teacher.Id);
    }
```

and make `MustNotBeAssigned` reuse it:

```csharp
    private void MustNotBeAssigned(Teacher teacher)
    {
        if (IsAssignedTo(teacher))
        {
            throw new TeacherAlreadyAssignedToCarException(Id, teacher.Id);
        }
    }
```

- [ ] **Step 4: Run the Car tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~CarTest"`
Expected: PASS (all `CarTest` tests, including the existing `Assign_Teacher__Teacher_Must_Not_Be_Assigned`).

- [ ] **Step 5: Write the failing Student guard tests and make the existing Student tests consistent**

In `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`:

1. In `Create` and `Create__Add_Event`, replace the two lines

```csharp
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
```

with

```csharp
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
```

2. In `Update_From_Roster` and `Update_From_Roster__Add_Event`, replace

```csharp
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
```

with

```csharp
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
```

3. Add these tests after `Create__Add_Event`:

```csharp
    [TestMethod]
    public void Create_With_A_Shared_Car()
    {
        //given
        var (car, otherTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        var teacher = TeacherFakeBuilder.Build();
        car.AssignTeacher(teacher);
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, null, null, null);

        //then
        student.TeacherId.ShouldBe(teacher.Id);
        student.TeacherId.ShouldNotBe(otherTeacher.Id);
        student.CarId.ShouldBe(car.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Create__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            car.AssignFakeTeacher();
        }

        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var act = () => Student.Create(nationalId, name, phone, teacher, car, null, null, null);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }
```

4. Add these tests after `Update_From_Roster__Add_Event`:

```csharp
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Update_From_Roster__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var act = () => student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, null, null, null);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Update_From_Roster__Rejected_Car_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalName = student.Name;
        var originalPhone = student.Phone;
        var originalTeacherId = student.TeacherId;
        var originalCarId = student.CarId;
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(
            () => student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, null, null, null));

        //then
        student.Name.ShouldBe(originalName);
        student.Phone.ShouldBe(originalPhone);
        student.TeacherId.ShouldBe(originalTeacherId);
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentUpdatedFromRoster>().ShouldBeEmpty();
    }
```

5. In `tests\DrivingLessons.Domain.Test\Entities\Fake\StudentFakeBuilder.cs`, make `Build()` assign the Car to the Teacher. Replace the first two lines of `Build()`:

```csharp
        var resolvedTeacher = teacher ?? TeacherFakeBuilder.Build();
        var resolvedCar = car ?? CarFakeBuilder.Build();
```

with

```csharp
        var resolvedTeacher = teacher ?? TeacherFakeBuilder.Build();
        var resolvedCar = car ?? CarFakeBuilder.Build();

        if (!resolvedCar.IsAssignedTo(resolvedTeacher))
        {
            resolvedCar.AssignTeacher(resolvedTeacher);
        }
```

(`SubmissionFakeBuilder` and `SubmissionTest` call `new StudentFakeBuilder().WithTeacher(teacher)` with the default Car; this keeps them valid.)

- [ ] **Step 6: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~StudentTest"`
Expected: build error `The type or namespace name 'StudentCarMustBeAssignedToTeacherException' could not be found`.

- [ ] **Step 7: Add the exception**

Create `src\DrivingLessons.Domain\Exceptions\StudentCarMustBeAssignedToTeacherException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentCarMustBeAssignedToTeacherException : DomainException
{
    public StudentCarMustBeAssignedToTeacherException(CarId carId, TeacherId teacherId)
        : base($"Car {carId.Value} is not assigned to teacher {teacherId.Value}.")
    {
    }
}
```

- [ ] **Step 8: Run again to see the guard tests fail on behavior**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~StudentTest"`
Expected: FAIL in `Create__Must_Be_Car_Of_Teacher`, `Update_From_Roster__Must_Be_Car_Of_Teacher` and `Update_From_Roster__Rejected_Car_Leaves_Student_Unchanged` ("Should throw StudentCarMustBeAssignedToTeacherException but did not"); the other Student tests pass.

- [ ] **Step 9: Guard `Student.Create` and `Student.UpdateFromRoster`**

In `src\DrivingLessons.Domain\Entities\Student.cs`:

`Create` starts with the guard:

```csharp
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
        MustBeCarOfTeacher(car, teacher);

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
```

`UpdateFromRoster` starts with the guard (rest unchanged):

```csharp
    public void UpdateFromRoster(
        StudentName name,
        PhoneNumber phone,
        Teacher teacher,
        Car car,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        MustBeCarOfTeacher(car, teacher);

        Name = name;
        Phone = phone;
        TeacherId = teacher.Id;
        CarId = car.Id;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentUpdatedFromRoster(Id, teacher.Id, car.Id));
    }
```

Add the private guard after `MustBeInactive`:

```csharp
    private static void MustBeCarOfTeacher(Car car, Teacher teacher)
    {
        if (!car.IsAssignedTo(teacher))
        {
            throw new StudentCarMustBeAssignedToTeacherException(car.Id, teacher.Id);
        }
    }
```

- [ ] **Step 10: Run the whole Domain suite**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS. If a test elsewhere (`SubmissionTest`, `UserTest`, ...) now throws `StudentCarMustBeAssignedToTeacherException`, it builds a Student without the builder; fix its setup by assigning the Car to the Teacher (`car.AssignTeacher(teacher);`), never by weakening the guard.

- [ ] **Step 11: Make the Application test fixtures consistent and add the exception-mapping test**

The Application suite builds Students with `Student.Create` directly. Each fixture assigns its Car to its Teacher right after creating the Car:

- `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs` and `ReviseSubmissionInteractorTest.cs`, in `Init`, after `var car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);` add:

```csharp
        car.AssignTeacher(teacher);
```

- `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`, in `Init`, after `car = Car.Create(CarName.Of("טויוטה 123"), CarType.Of("יאריס"), Transmission.Manual);` add:

```csharp
        car.AssignTeacher(teacher);
```

Add to `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, after `Teacher_Already_Linked_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void Student_Car_Not_Of_Teacher_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentCarMustBeAssignedToTeacherException(CarId.New(), TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentCarMustBeAssignedToTeacher");
    }
```

- [ ] **Step 12: Run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build with 0 warnings introduced, both suites PASS (the mapping test passes with no filter change: every `DomainException` is already a 409 carrying its rule as `code`).

- [ ] **Step 13: Commit**

```bash
git add src/DrivingLessons.Domain/Entities/Car.cs src/DrivingLessons.Domain/Entities/Student.cs src/DrivingLessons.Domain/Exceptions/StudentCarMustBeAssignedToTeacherException.cs tests/DrivingLessons.Domain.Test/Entities/CarTest.cs tests/DrivingLessons.Domain.Test/Entities/StudentTest.cs tests/DrivingLessons.Domain.Test/Entities/Fake/StudentFakeBuilder.cs tests/DrivingLessons.Application.Test/Commands/CreateSubmissionInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ReviseSubmissionInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ImportRosterInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs
git commit -m "feat(domain): a Student's Car must be one of their Teacher's Cars (#92)"
```

Add any other test file Step 10 had to fix to the same commit. End the commit message with the `Co-Authored-By` trailer from the session's attribution rule.
