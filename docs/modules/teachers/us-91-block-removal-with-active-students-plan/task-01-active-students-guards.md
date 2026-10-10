# Task 1 of 4: Active Students guards on Teacher and Car

> Part of [#91: Block deleting or unassigning Teachers and Cars while active Students depend on them](README.md). Work on branch `91-block-removal-with-active-students`. Read README decisions 1 to 5 first.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveStudentsException.cs`, `CarMustNotHaveActiveStudentsException.cs`, `TeacherAssignmentMustNotHaveActiveStudentsException.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Teacher.cs` (`Delete`)
- Modify: `src\DrivingLessons.Domain\Entities\Car.cs` (`UnassignTeacher`, `Delete`)
- Modify: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`
- Modify: `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs`, `DeleteCar\DeleteCarInteractor.cs`, `UnassignCarFromTeacher\UnassignCarFromTeacherInteractor.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Entities\TeacherTest.cs`, `CarTest.cs`
- Modify: `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs`, `ImportRosterInteractorTest.cs` (line ~563)
- Create: `tests\DrivingLessons.Application.Test\Commands\DeleteCarInteractorTest.cs`, `UnassignCarFromTeacherInteractorTest.cs`

**Interfaces:**
- Consumes: `Student` (`Name: StudentName`, `TeacherId`, `CarId`, `IsActive`, `Deactivate()`, `Reactivate()`, `Student.Create(NationalId, StudentName, PhoneNumber, Teacher, Car, Address?, LessonsStartDate?, LicenseType?)`), `StudentFakeBuilder` (`WithTeacher`, `WithCar`, `Build`, `BuildInactive`), `TeacherFakeBuilder.Build()`, `CarFakeBuilder.Build()`, `Car.AssignFakeTeacher()`, `IUserQueries.ActiveExistsLinkedToTeacherAsync(TeacherId)`.
- Produces (tasks 2 to 4 rely on these):
  - `Teacher.Delete(IReadOnlyCollection<Student> students)`; `Car.Delete(IReadOnlyCollection<Student> students)`; `Car.UnassignTeacher(Teacher teacher, IReadOnlyCollection<Student> students)`.
  - `TeacherMustNotHaveActiveStudentsException(TeacherId id, IReadOnlyList<StudentName> activeStudentNames)` with `ActiveStudentNames`.
  - `CarMustNotHaveActiveStudentsException(CarId id, IReadOnlyList<StudentName> activeStudentNames)` with `ActiveStudentNames`.
  - `TeacherAssignmentMustNotHaveActiveStudentsException(CarId carId, TeacherId teacherId, TeacherName teacherName, IReadOnlyList<StudentName> activeStudentNames)` with `TeacherName`, `ActiveStudentNames`.
  - `IStudentRepository.FindByTeacherAsync(TeacherId teacherId)` and `FindByCarAsync(CarId carId)`, both `Task<IReadOnlyCollection<Student>>`, active and inactive.
  - `DeleteTeacherInteractor(ITeacherRepository, IUserQueries, IStudentRepository, IUnitOfWork)`, `DeleteCarInteractor(ICarRepository, IStudentRepository, IUnitOfWork)`, `UnassignCarFromTeacherInteractor(ICarRepository, ITeacherRepository, IStudentRepository, IUnitOfWork)`.

- [ ] **Step 1: Write the failing domain tests**

In `tests\DrivingLessons.Domain.Test\Entities\TeacherTest.cs`, change the four existing `teacher.Delete()` calls (lines ~115, 128, 143, 146) to `teacher.Delete([])`, then append inside the class:

```csharp
    [TestMethod]
    public void Delete__Must_Not_Have_Active_Students()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();

        //when
        var act = () => teacher.Delete([student]);

        //then
        Should.Throw<TeacherMustNotHaveActiveStudentsException>(act);
        teacher.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Delete_With_Only_Inactive_Students()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).BuildInactive();

        //when
        teacher.Delete([student]);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete_Ignores_Students_Of_Other_Teachers()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var studentOfAnotherTeacher = new StudentFakeBuilder().Build();

        //when
        teacher.Delete([studentOfAnotherTeacher]);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Refusal_Names_The_Active_Students_In_Order()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var first = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var second = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var inactive = new StudentFakeBuilder().WithTeacher(teacher).BuildInactive();
        var expected = new[] { first.Name, second.Name }.OrderBy(x => x.Value, StringComparer.Ordinal).ToList();

        //when
        var act = () => teacher.Delete([second, inactive, first]);

        //then
        var refusal = Should.Throw<TeacherMustNotHaveActiveStudentsException>(act);
        refusal.ActiveStudentNames.ShouldBe(expected);
    }

    [TestMethod]
    public void Delete__Must_Not_Be_Deleted_Before_Active_Students_Are_Checked()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        teacher.Delete([]);

        //when
        var act = () => teacher.Delete([student]);

        //then
        Should.Throw<TeacherAlreadyDeletedException>(act);
    }
```

Add `using DrivingLessons.Domain.Test.Entities.Fake;` if the file doesn't have it (it already uses `TeacherFakeBuilder`, so it does).

In `tests\DrivingLessons.Domain.Test\Entities\CarTest.cs`, change every existing `car.UnassignTeacher(x)` (lines ~181, 195, 213, 224, 227, 275) to `car.UnassignTeacher(x, [])` and every `car.Delete()` (lines ~292, 305, 320, 323) to `car.Delete([])`, then append inside the class:

```csharp
    [TestMethod]
    public void Unassign_Teacher__Must_Not_Have_Active_Students_Of_The_Teacher()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();

        //when
        var act = () => car.UnassignTeacher(teacher, [student]);

        //then
        var refusal = Should.Throw<TeacherAssignmentMustNotHaveActiveStudentsException>(act);
        refusal.TeacherName.ShouldBe(teacher.Name);
        refusal.ActiveStudentNames.ShouldBe([student.Name]);
        car.IsAssignedTo(teacher).ShouldBeTrue();
    }

    [TestMethod]
    public void Unassign_Teacher_With_Only_Inactive_Students()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).BuildInactive();

        //when
        car.UnassignTeacher(teacher, [student]);

        //then
        car.IsAssignedTo(teacher).ShouldBeFalse();
    }

    [TestMethod]
    public void Unassign_Teacher_With_Active_Students_Of_Another_Teacher()
    {
        //given
        var (car, removed) = CarFakeBuilder.Build().AssignFakeTeacher();
        var other = TeacherFakeBuilder.Build();
        car.AssignTeacher(other);
        var studentOfOther = new StudentFakeBuilder().WithTeacher(other).WithCar(car).Build();

        //when
        car.UnassignTeacher(removed, [studentOfOther]);

        //then
        car.IsAssignedTo(removed).ShouldBeFalse();
        car.IsAssignedTo(other).ShouldBeTrue();
    }

    [TestMethod]
    public void Unassign_Teacher__Teacher_Must_Be_Assigned_Before_Active_Students_Are_Checked()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        var flagged = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();
        flagged.Deactivate();
        car.UnassignTeacher(teacher, [flagged]);
        flagged.Reactivate();

        //when
        var act = () => car.UnassignTeacher(teacher, [flagged]);

        //then
        Should.Throw<TeacherNotAssignedToCarException>(act);
    }

    [TestMethod]
    public void Delete__Must_Not_Have_Active_Students()
    {
        //given
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithCar(car).Build();

        //when
        var act = () => car.Delete([student]);

        //then
        Should.Throw<CarMustNotHaveActiveStudentsException>(act);
        car.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Delete_With_Only_Inactive_Students()
    {
        //given
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithCar(car).BuildInactive();

        //when
        car.Delete([student]);

        //then
        car.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete_Ignores_Students_On_Other_Cars()
    {
        //given
        var car = CarFakeBuilder.Build();
        var studentOnAnotherCar = new StudentFakeBuilder().Build();

        //when
        car.Delete([studentOnAnotherCar]);

        //then
        car.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Refusal_Names_The_Active_Students_In_Order()
    {
        //given
        var car = CarFakeBuilder.Build();
        var first = new StudentFakeBuilder().WithCar(car).Build();
        var second = new StudentFakeBuilder().WithCar(car).Build();
        var inactive = new StudentFakeBuilder().WithCar(car).BuildInactive();
        var expected = new[] { first.Name, second.Name }.OrderBy(x => x.Value, StringComparer.Ordinal).ToList();

        //when
        var act = () => car.Delete([second, inactive, first]);

        //then
        var refusal = Should.Throw<CarMustNotHaveActiveStudentsException>(act);
        refusal.ActiveStudentNames.ShouldBe(expected);
    }
```

`StudentFakeBuilder` assigns the Car to the Teacher when they aren't assigned yet, so `WithCar(car)` alone gives a Student on that Car with a fresh Teacher.

- [ ] **Step 2: Run the domain tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: build FAIL: `No overload for method 'Delete' takes 1 arguments`, `'TeacherMustNotHaveActiveStudentsException' could not be found` (and the Car equivalents).

- [ ] **Step 3: Write the exceptions**

`src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveStudentsException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherMustNotHaveActiveStudentsException : DomainException
{
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public TeacherMustNotHaveActiveStudentsException(TeacherId id, IReadOnlyList<StudentName> activeStudentNames)
        : base($"Teacher {id.Value} still has {activeStudentNames.Count} active Students.")
    {
        ActiveStudentNames = activeStudentNames;
    }
}
```

`src\DrivingLessons.Domain\Exceptions\CarMustNotHaveActiveStudentsException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class CarMustNotHaveActiveStudentsException : DomainException
{
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public CarMustNotHaveActiveStudentsException(CarId id, IReadOnlyList<StudentName> activeStudentNames)
        : base($"Car {id.Value} still has {activeStudentNames.Count} active Students.")
    {
        ActiveStudentNames = activeStudentNames;
    }
}
```

`src\DrivingLessons.Domain\Exceptions\TeacherAssignmentMustNotHaveActiveStudentsException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAssignmentMustNotHaveActiveStudentsException : DomainException
{
    public TeacherName TeacherName { get; }
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public TeacherAssignmentMustNotHaveActiveStudentsException(
        CarId carId,
        TeacherId teacherId,
        TeacherName teacherName,
        IReadOnlyList<StudentName> activeStudentNames)
        : base($"Teacher {teacherId.Value} still has {activeStudentNames.Count} active Students on car {carId.Value}.")
    {
        TeacherName = teacherName;
        ActiveStudentNames = activeStudentNames;
    }
}
```

- [ ] **Step 4: Guard the aggregates**

In `Teacher.cs`, replace `Delete()` and add the guard after `MustNotBeDeleted`:

```csharp
    public void Delete(IReadOnlyCollection<Student> students)
    {
        MustNotBeDeleted();
        MustNotHaveActiveStudents(students);

        IsDeleted = true;

        AddEvent(new TeacherDeleted(Id));
    }
```

```csharp
    private void MustNotHaveActiveStudents(IReadOnlyCollection<Student> students)
    {
        var activeStudentNames = students
                                 .Where(x => x.IsActive && x.TeacherId == Id)
                                 .Select(x => x.Name)
                                 .OrderBy(x => x.Value, StringComparer.Ordinal)
                                 .ToList();

        if (activeStudentNames.Count > 0)
        {
            throw new TeacherMustNotHaveActiveStudentsException(Id, activeStudentNames);
        }
    }
```

In `Car.cs`, replace `UnassignTeacher` and `Delete`:

```csharp
    public void UnassignTeacher(Teacher teacher, IReadOnlyCollection<Student> students)
    {
        var assignment = teacherAssignments.FirstOrDefault(x => x.TeacherId == teacher.Id)
                         ?? throw new TeacherNotAssignedToCarException(Id, teacher.Id);

        MustNotHaveActiveStudentsOf(teacher, students);

        teacherAssignments.Remove(assignment);

        AddEvent(new CarUnassignedFromTeacher(Id, teacher.Id));
    }
```

```csharp
    public void Delete(IReadOnlyCollection<Student> students)
    {
        MustNotBeDeleted();
        MustNotHaveActiveStudents(students);

        IsDeleted = true;

        AddEvent(new CarDeleted(Id));
    }
```

and add, after `MustNotBeDeleted`:

```csharp
    private void MustNotHaveActiveStudentsOf(Teacher teacher, IReadOnlyCollection<Student> students)
    {
        var studentsOfTeacher = students.Where(x => x.TeacherId == teacher.Id);
        var activeStudentNames = ActiveStudentNamesOn(studentsOfTeacher);

        if (activeStudentNames.Count > 0)
        {
            throw new TeacherAssignmentMustNotHaveActiveStudentsException(Id, teacher.Id, teacher.Name, activeStudentNames);
        }
    }

    private void MustNotHaveActiveStudents(IReadOnlyCollection<Student> students)
    {
        var activeStudentNames = ActiveStudentNamesOn(students);

        if (activeStudentNames.Count > 0)
        {
            throw new CarMustNotHaveActiveStudentsException(Id, activeStudentNames);
        }
    }

    private List<StudentName> ActiveStudentNamesOn(IEnumerable<Student> students)
    {
        return students
               .Where(x => x.IsActive && x.CarId == Id)
               .Select(x => x.Name)
               .OrderBy(x => x.Value, StringComparer.Ordinal)
               .ToList();
    }
```

- [ ] **Step 5: Run the domain tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS, every test. (`dotnet build` of the whole solution still fails here: the three interactors and `ImportRosterInteractorTest` call the old signatures. Steps 6 to 9 fix them.)

- [ ] **Step 6: Write the failing interactor tests**

Replace `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs` with:

```csharp
using DrivingLessons.Application.Commands.DeleteTeacher;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class DeleteTeacherInteractorTest
{
    private ITeacherRepository repository = null!;
    private IUserQueries userQueries = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteTeacherInteractor interactor = null!;
    private Teacher teacher = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<ITeacherRepository>();
        userQueries = A.Fake<IUserQueries>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteTeacherInteractor(repository, userQueries, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));

        A.CallTo(() => repository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => repository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(A<TeacherId>._)).Returns(false);
        A.CallTo(() => studentRepository.FindByTeacherAsync(A<TeacherId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Without_A_User()
    {
        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Whose_Linked_User_Is_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(false);

        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_User_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveUserException>(act);
        teacher.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_User_Is_Refused_Before_Students_Are_Loaded()
    {
        //given
        A.CallTo(() => userQueries.ActiveExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveUserException>(act);
        A.CallTo(() => studentRepository.FindByTeacherAsync(A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_Student_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherMustNotHaveActiveStudentsException>(act);
        teacher.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByTeacherAsync(teacher.Id)).Returns([StudentOf(teacher, isActive: false)]);

        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student StudentOf(Teacher teacher, bool isActive)
    {
        var car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

        var student = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("Noa Mizrahi"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);

        if (!isActive)
        {
            student.Deactivate();
        }

        return student;
    }
}
```

Create `tests\DrivingLessons.Application.Test\Commands\DeleteCarInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.DeleteCar;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class DeleteCarInteractorTest
{
    private ICarRepository repository = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteCarInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<ICarRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteCarInteractor(repository, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

        A.CallTo(() => repository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => repository.GetAsync(car.Id)).Returns(car);
        A.CallTo(() => studentRepository.FindByCarAsync(A<CarId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Deletes_A_Car_Without_Students()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value);

        //then
        car.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Car_With_An_Active_Student_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value);

        //then
        await Should.ThrowAsync<CarMustNotHaveActiveStudentsException>(act);
        car.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deletes_A_Car_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: false)]);

        //when
        await interactor.ExecuteAsync(car.Id.Value);

        //then
        car.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private Student StudentOn(bool isActive)
    {
        var student = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("Noa Mizrahi"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);

        if (!isActive)
        {
            student.Deactivate();
        }

        return student;
    }
}
```

Create `tests\DrivingLessons.Application.Test\Commands\UnassignCarFromTeacherInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.UnassignCarFromTeacher;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class UnassignCarFromTeacherInteractorTest
{
    private ICarRepository carRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IStudentRepository studentRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private UnassignCarFromTeacherInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;

    [TestInitialize]
    public void Init()
    {
        carRepository = A.Fake<ICarRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new UnassignCarFromTeacherInteractor(carRepository, teacherRepository, studentRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        car.AssignTeacher(teacher);

        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(car.Id)).Returns(car);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => studentRepository.FindByCarAsync(A<CarId>._)).Returns(Array.Empty<Student>());
    }

    [TestMethod]
    public async Task Unassigns_A_Teacher_Without_Students()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        car.IsAssignedTo(teacher).ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Teacher_With_An_Active_Student_On_The_Car_Is_Not_Unassigned()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: true)]);

        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        await Should.ThrowAsync<TeacherAssignmentMustNotHaveActiveStudentsException>(act);
        car.IsAssignedTo(teacher).ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unassigns_A_Teacher_With_Only_Inactive_Students()
    {
        //given
        A.CallTo(() => studentRepository.FindByCarAsync(car.Id)).Returns([StudentOn(isActive: false)]);

        //when
        await interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        car.IsAssignedTo(teacher).ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), teacher.Id.Value);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(car.Id.Value, Guid.NewGuid());

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private Student StudentOn(bool isActive)
    {
        var student = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("Noa Mizrahi"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);

        if (!isActive)
        {
            student.Deactivate();
        }

        return student;
    }
}
```

In `ImportRosterInteractorTest.cs` (line ~563), change `formerCar.UnassignTeacher(teacher);` to `formerCar.UnassignTeacher(teacher, []);`.

If `Student.Create`, the value objects or `Deactivate` take different arguments than shown, copy the construction from `DeactivateStudentInteractorTest`, which compiles today.

- [ ] **Step 7: Run the application tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: build FAIL: `'IStudentRepository' does not contain a definition for 'FindByTeacherAsync'` / `'FindByCarAsync'`, and the interactor constructors take fewer arguments.

- [ ] **Step 8: Add the repository methods**

In `IStudentRepository.cs`, after `FindAllAsync`:

```csharp
    Task<IReadOnlyCollection<Student>> FindByTeacherAsync(TeacherId teacherId);

    Task<IReadOnlyCollection<Student>> FindByCarAsync(CarId carId);
```

In `StudentRepository.cs`, after `FindAllAsync`:

```csharp
    public async Task<IReadOnlyCollection<Student>> FindByTeacherAsync(TeacherId teacherId)
    {
        return await dbContext
                         .Students
                         .Where(x => x.TeacherId == teacherId)
                         .ToListAsync();
    }

    public async Task<IReadOnlyCollection<Student>> FindByCarAsync(CarId carId)
    {
        return await dbContext
                         .Students
                         .Where(x => x.CarId == carId)
                         .ToListAsync();
    }
```

- [ ] **Step 9: Pass the Students from the interactors**

`DeleteTeacherInteractor.cs`: add the field and constructor parameter, and load the Students after the User guard:

```csharp
    private readonly ITeacherRepository repository;
    private readonly IUserQueries userQueries;
    private readonly IStudentRepository studentRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteTeacherInteractor(
        ITeacherRepository repository,
        IUserQueries userQueries,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.userQueries = userQueries;
        this.studentRepository = studentRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var hasActiveUser = await userQueries.ActiveExistsLinkedToTeacherAsync(teacherId);

        if (hasActiveUser)
        {
            throw new TeacherMustNotHaveActiveUserException(teacherId);
        }

        var students = await studentRepository.FindByTeacherAsync(teacherId);

        teacher.Delete(students);

        await unitOfWork.CommitAsync();
    }
```

`DeleteCarInteractor.cs`:

```csharp
    private readonly ICarRepository repository;
    private readonly IStudentRepository studentRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCarInteractor(ICarRepository repository, IStudentRepository studentRepository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.studentRepository = studentRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var carId = CarId.Of(id);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var students = await studentRepository.FindByCarAsync(carId);

        car.Delete(students);

        await unitOfWork.CommitAsync();
    }
```

`UnassignCarFromTeacherInteractor.cs`:

```csharp
    private readonly ICarRepository carRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IStudentRepository studentRepository;
    private readonly IUnitOfWork unitOfWork;

    public UnassignCarFromTeacherInteractor(
        ICarRepository carRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork)
    {
        this.carRepository = carRepository;
        this.teacherRepository = teacherRepository;
        this.studentRepository = studentRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid teacherId)
    {
        var carId = CarId.Of(id);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var resolvedTeacherId = TeacherId.Of(teacherId);

        var teacher = await teacherRepository.GetAsync(resolvedTeacherId)
                      ?? throw new TeacherNotFoundException(resolvedTeacherId);

        var students = await studentRepository.FindByCarAsync(carId);

        car.UnassignTeacher(teacher, students);

        await unitOfWork.CommitAsync();
    }
```

The interactors are already registered in `DependencyInjection.cs`, and `IStudentRepository` is already registered by the Infrastructure layer, so no registration changes.

- [ ] **Step 10: Run every backend test**

Run: `dotnet build`, then `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj` and `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: build succeeds with no warnings added; PASS, every test.

Run: `dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web`
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Domain src/DrivingLessons.Infrastructure/EntityFramework/Repositories/StudentRepository.cs src/DrivingLessons.Application/Commands/DeleteTeacher src/DrivingLessons.Application/Commands/DeleteCar src/DrivingLessons.Application/Commands/UnassignCarFromTeacher tests/DrivingLessons.Domain.Test/Entities/TeacherTest.cs tests/DrivingLessons.Domain.Test/Entities/CarTest.cs tests/DrivingLessons.Application.Test/Commands/DeleteTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/DeleteCarInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/UnassignCarFromTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ImportRosterInteractorTest.cs
git commit -m "feat(teachers): refuse deleting or unassigning a Teacher or Car while active Students depend on it (#91)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
