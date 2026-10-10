# Task 1 of 7: `Student.ChangeTeacher` and `Student.ChangeCar` (domain)

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Work on branch `95-change-teacher-and-car`. Read README decisions 2 to 7 first.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\StudentAlreadyWithTeacherException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentAlreadyOnCarException.cs`
- Create: `src\DrivingLessons.Domain\Events\StudentTeacherChanged.cs`
- Create: `src\DrivingLessons.Domain\Events\StudentCarChanged.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Car.cs` (`IsAssignedTo`, line ~72)
- Modify: `src\DrivingLessons.Domain\Entities\Student.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`

**Interfaces:**
- Consumes: `Teacher`, `Car` (`Id`, `AssignTeacher(Teacher)`, `IsAssignedTo(Teacher)`), `StudentCarMustBeAssignedToTeacherException(CarId, TeacherId)`, test helpers `StudentFakeBuilder` (`WithTeacher`, `WithCar`, `Build`, `BuildInactive`), `TeacherFakeBuilder.Build()`, `CarFakeBuilder.Build()`, `car.AssignFakeTeacher()` → `(Car, Teacher)`, `SubmissionFakeBuilder.BuildScenario()` → `SubmissionScenario(Publication, Teacher, Student, WeekSchedule)`, `SubmissionFakeBuilder.Build(SubmissionScenario)` → `Submission`.
- Produces (task 2 relies on these):
  - `Student.ChangeTeacher(Teacher teacher, Car car)`: throws `StudentAlreadyWithTeacherException` when `teacher.Id == TeacherId`, then `StudentCarMustBeAssignedToTeacherException` when `car` is not assigned to `teacher`; sets `TeacherId`, `CarId`; adds `StudentTeacherChanged(StudentId, TeacherId, CarId)`.
  - `Student.ChangeCar(Car car)`: throws `StudentAlreadyOnCarException` when `car.Id == CarId`, then `StudentCarMustBeAssignedToTeacherException` when `car` is not assigned to the current `TeacherId`; sets `CarId`; adds `StudentCarChanged(StudentId, CarId)`.
  - `Car.IsAssignedTo(TeacherId teacherId): bool`.
  - Error codes (derived by `ApiExceptionFilter` from the type names): `studentAlreadyWithTeacher`, `studentAlreadyOnCar`.

- [ ] **Step 1: Write the failing tests**

Add to `StudentTest.cs`, after `Change_Details_Of_An_Inactive_Student` (before `Deactivate`):

```csharp
    [TestMethod]
    public void Change_Teacher()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(newCar.Id);
    }

    [TestMethod]
    public void Change_Teacher__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student
            .UncommittedEvents
            .OfType<StudentTeacherChanged>()
            .Where(x => x.StudentId == student.Id
                        && x.TeacherId == newTeacher.Id
                        && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Teacher_Keeps_A_Car_The_New_Teacher_Also_Teaches_On()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var sharedCar = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(sharedCar).Build();
        var newTeacher = TeacherFakeBuilder.Build();
        sharedCar.AssignTeacher(newTeacher);

        //when
        student.ChangeTeacher(newTeacher, sharedCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(sharedCar.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Teacher__Must_Not_Be_Current_Teacher(bool withAnotherCarOfTheTeacher)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();
        var chosenCar = car;

        if (withAnotherCarOfTheTeacher)
        {
            chosenCar = CarFakeBuilder.Build();
            chosenCar.AssignTeacher(teacher);
        }

        //when
        var act = () => student.ChangeTeacher(teacher, chosenCar);

        //then
        Should.Throw<StudentAlreadyWithTeacherException>(act);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Teacher__Must_Be_Car_Of_New_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        //when
        var act = () => student.ChangeTeacher(newTeacher, newCar);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Change_Teacher__Rejected_Change_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalTeacherId = student.TeacherId;
        var originalCarId = student.CarId;
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(() => student.ChangeTeacher(newTeacher, newCar));

        //then
        student.TeacherId.ShouldBe(originalTeacherId);
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentTeacherChanged>().ShouldBeEmpty();
    }

    [TestMethod]
    public void Change_Teacher_Of_An_Inactive_Student()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Change_Teacher_Leaves_Existing_Submissions_With_Their_Publication()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var publicationId = submission.PublicationId;
        var weekScheduleId = submission.WeekScheduleId;
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        scenario.Student.ChangeTeacher(newTeacher, newCar);

        //then
        submission.StudentId.ShouldBe(scenario.Student.Id);
        submission.PublicationId.ShouldBe(publicationId);
        submission.WeekScheduleId.ShouldBe(weekScheduleId);
    }

    [TestMethod]
    public void Change_Car()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student.CarId.ShouldBe(newCar.Id);
        student.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void Change_Car__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student
            .UncommittedEvents
            .OfType<StudentCarChanged>()
            .Where(x => x.StudentId == student.Id && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Car_Fixes_A_Car_That_Is_Not_The_Teachers()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var oldCar = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(oldCar).Build();
        oldCar.UnassignTeacher(teacher);
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student.CarId.ShouldBe(newCar.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Car__Must_Not_Be_Current_Car(bool inactive)
    {
        //given
        var car = CarFakeBuilder.Build();
        var builder = new StudentFakeBuilder().WithCar(car);
        var student = inactive
            ? builder.BuildInactive()
            : builder.Build();

        //when
        var act = () => student.ChangeCar(car);

        //then
        Should.Throw<StudentAlreadyOnCarException>(act);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Car__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        //when
        var act = () => student.ChangeCar(newCar);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Change_Car__Rejected_Change_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalCarId = student.CarId;
        var newCar = CarFakeBuilder.Build();

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(() => student.ChangeCar(newCar));

        //then
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentCarChanged>().ShouldBeEmpty();
    }
```

If `Car.UnassignTeacher(Teacher)` has a guard that refuses the unassignment in `Change_Car_Fixes_A_Car_That_Is_Not_The_Teachers` (read `Car.cs`, around line 60), keep the test and build the flagged state another way the domain allows; do not add a test-only method to `Car`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~StudentTest"`
Expected: build FAIL with `'Student' does not contain a definition for 'ChangeTeacher'` (and `ChangeCar`, `StudentAlreadyWithTeacherException`, `StudentTeacherChanged`, ...).

- [ ] **Step 3: Add the exceptions and events**

`src\DrivingLessons.Domain\Exceptions\StudentAlreadyWithTeacherException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyWithTeacherException : DomainException
{
    public StudentAlreadyWithTeacherException(StudentId id, TeacherId teacherId)
        : base($"Student {id.Value} is already with teacher {teacherId.Value}.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\StudentAlreadyOnCarException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyOnCarException : DomainException
{
    public StudentAlreadyOnCarException(StudentId id, CarId carId)
        : base($"Student {id.Value} is already on car {carId.Value}.")
    {
    }
}
```

`src\DrivingLessons.Domain\Events\StudentTeacherChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentTeacherChanged(StudentId StudentId, TeacherId TeacherId, CarId CarId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\StudentCarChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentCarChanged(StudentId StudentId, CarId CarId) : IDomainEvent;
```

- [ ] **Step 4: Add `Car.IsAssignedTo(TeacherId)`**

In `Car.cs`, replace the existing `IsAssignedTo(Teacher)` body so it delegates, and add the overload right after it:

```csharp
    public bool IsAssignedTo(Teacher teacher)
    {
        return IsAssignedTo(teacher.Id);
    }

    public bool IsAssignedTo(TeacherId teacherId)
    {
        return teacherAssignments.Any(x => x.TeacherId == teacherId);
    }
```

- [ ] **Step 5: Add `ChangeTeacher` and `ChangeCar` to `Student`**

In `Student.cs`, after `ChangeDetails(...)` and before `Deactivate()`:

```csharp
    public void ChangeTeacher(Teacher teacher, Car car)
    {
        MustNotBeWithTeacher(teacher);
        MustBeCarOfTeacher(car, teacher);

        TeacherId = teacher.Id;
        CarId = car.Id;

        AddEvent(new StudentTeacherChanged(Id, teacher.Id, car.Id));
    }

    public void ChangeCar(Car car)
    {
        MustNotBeOnCar(car);
        MustBeCarOfCurrentTeacher(car);

        CarId = car.Id;

        AddEvent(new StudentCarChanged(Id, car.Id));
    }
```

and the guards, after `MustBeInactive()` and before `MustBeCarOfTeacher(...)`:

```csharp
    private void MustNotBeWithTeacher(Teacher teacher)
    {
        if (teacher.Id == TeacherId)
        {
            throw new StudentAlreadyWithTeacherException(Id, teacher.Id);
        }
    }

    private void MustNotBeOnCar(Car car)
    {
        if (car.Id == CarId)
        {
            throw new StudentAlreadyOnCarException(Id, car.Id);
        }
    }

    private void MustBeCarOfCurrentTeacher(Car car)
    {
        if (!car.IsAssignedTo(TeacherId))
        {
            throw new StudentCarMustBeAssignedToTeacherException(car.Id, TeacherId);
        }
    }
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS, every test (the whole project, so `CarTest` proves the `IsAssignedTo` refactor).

- [ ] **Step 7: Build the solution**

Run: `dotnet build`
Expected: `Build succeeded`, 0 warnings introduced by these files.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Domain/Entities/Student.cs src/DrivingLessons.Domain/Entities/Car.cs src/DrivingLessons.Domain/Exceptions/StudentAlreadyWithTeacherException.cs src/DrivingLessons.Domain/Exceptions/StudentAlreadyOnCarException.cs src/DrivingLessons.Domain/Events/StudentTeacherChanged.cs src/DrivingLessons.Domain/Events/StudentCarChanged.cs tests/DrivingLessons.Domain.Test/Entities/StudentTest.cs
git commit -m "feat(students): Student changes Teacher with one of their Cars, or changes Car within the Teacher's Cars (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
