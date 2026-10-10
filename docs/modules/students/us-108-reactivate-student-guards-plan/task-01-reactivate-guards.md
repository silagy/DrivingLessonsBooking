# Task 1: Reactivate Guards - Domain, Repositories, Interactors

Part of [#108 plan](README.md). Read the README's Decisions and Global Constraints first.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\StudentTeacherMustNotBeDeletedException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentCarMustNotBeDeletedException.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Student.cs` (`Reactivate`, two private guards)
- Modify: `src\DrivingLessons.Domain\Repositories\ITeacherRepository.cs`, `ICarRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\TeacherRepository.cs`, `CarRepository.cs`
- Modify: `src\DrivingLessons.Application\Commands\ReactivateStudent\ReactivateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs` (~line 190)
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ReactivateStudentInteractorTest.cs`

**Interfaces:**
- Consumes (#91): `Teacher.Delete(IReadOnlyCollection<Student>)`, `Car.Delete(IReadOnlyCollection<Student>)`, `Car.UnassignTeacher(Teacher, IReadOnlyCollection<Student>)`; these refuse only *active* Students, so an Inactive Student in the collection is fine.
- Produces: `Student.Reactivate(Teacher teacher, Car car)`; `StudentTeacherMustNotBeDeletedException(StudentId, TeacherId)`; `StudentCarMustNotBeDeletedException(StudentId, CarId)`; `ITeacherRepository.GetIncludingDeletedAsync(TeacherId) : Task<Teacher?>`; `ICarRepository.GetIncludingDeletedAsync(CarId) : Task<Car?>`; `ReactivateStudentInteractor(IStudentRepository, ITeacherRepository, ICarRepository, IUnitOfWork)`.

- [ ] **Step 1: Write the failing domain tests**

In `StudentTest.cs`, replace the three existing `Reactivate*` tests with the block below and add the helper at the end of the class:

```csharp
    [TestMethod]
    public void Reactivate()
    {
        //given
        var (student, teacher, car) = InactiveStudent();

        //when
        student.Reactivate(teacher, car);

        //then
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Reactivate__Add_Event()
    {
        //given
        var (student, teacher, car) = InactiveStudent();

        //when
        student.Reactivate(teacher, car);

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
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();

        //when
        var act = () => student.Reactivate(teacher, car);

        //then
        Should.Throw<StudentAlreadyActiveException>(act);
    }

    [TestMethod]
    public void Reactivate__Must_Not_Be_Active_Before_Teacher_Check()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();
        teacher.Delete([]);

        //when
        var act = () => student.Reactivate(teacher, car);

        //then
        Should.Throw<StudentAlreadyActiveException>(act);
    }

    [TestMethod]
    public void Reactivate__Teacher_Must_Not_Be_Deleted()
    {
        //given
        var (student, teacher, car) = InactiveStudent();
        teacher.Delete([student]);

        //when
        var act = () => student.Reactivate(teacher, car);

        //then
        Should.Throw<StudentTeacherMustNotBeDeletedException>(act);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Reactivate__Car_Must_Not_Be_Deleted()
    {
        //given
        var (student, teacher, car) = InactiveStudent();
        car.Delete([student]);

        //when
        var act = () => student.Reactivate(teacher, car);

        //then
        Should.Throw<StudentCarMustNotBeDeletedException>(act);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Reactivate__Car_Must_Be_Assigned_To_Teacher()
    {
        //given
        var (student, teacher, car) = InactiveStudent();
        car.UnassignTeacher(teacher, [student]);

        //when
        var act = () => student.Reactivate(teacher, car);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
        student.IsActive.ShouldBeFalse();
    }
```

```csharp
    private static (Student Student, Teacher Teacher, Car Car) InactiveStudent()
    {
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).BuildInactive();

        return (student, teacher, car);
    }
```

If `StudentTest.cs` already has a private helper with that name (from #95), give this one a distinct name such as `InactiveStudentWithTeacherAndCar`. Don't merge the two helpers.

- [ ] **Step 2: Run the domain tests and see them fail to compile**

Run: `dotnet test tests/DrivingLessons.Domain.Test --filter "FullyQualifiedName~StudentTest"`
Expected: build error. `Reactivate` takes no arguments, and `StudentTeacherMustNotBeDeletedException` / `StudentCarMustNotBeDeletedException` don't exist.

- [ ] **Step 3: Add the exceptions and the guarded `Reactivate`**

`src\DrivingLessons.Domain\Exceptions\StudentTeacherMustNotBeDeletedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentTeacherMustNotBeDeletedException : DomainException
{
    public StudentTeacherMustNotBeDeletedException(StudentId studentId, TeacherId teacherId)
        : base($"Teacher {teacherId.Value} of student {studentId.Value} is deleted.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\StudentCarMustNotBeDeletedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentCarMustNotBeDeletedException : DomainException
{
    public StudentCarMustNotBeDeletedException(StudentId studentId, CarId carId)
        : base($"Car {carId.Value} of student {studentId.Value} is deleted.")
    {
    }
}
```

In `Student.cs`, replace `Reactivate()` with:

```csharp
    public void Reactivate(Teacher teacher, Car car)
    {
        MustBeInactive();
        TeacherMustNotBeDeleted(teacher);
        CarMustNotBeDeleted(car);
        MustBeCarOfTeacher(car, teacher);

        IsActive = true;

        AddEvent(new StudentReactivated(Id));
    }
```

Add these next to `MustBeInactive()`:

```csharp
    private void TeacherMustNotBeDeleted(Teacher teacher)
    {
        if (teacher.IsDeleted)
        {
            throw new StudentTeacherMustNotBeDeletedException(Id, teacher.Id);
        }
    }

    private void CarMustNotBeDeleted(Car car)
    {
        if (car.IsDeleted)
        {
            throw new StudentCarMustNotBeDeletedException(Id, car.Id);
        }
    }
```

- [ ] **Step 4: Fix the Roster import call site**

In `ImportRosterInteractor.cs` (~line 190), the row's `teacher` and `car` are already resolved and Car-of-Teacher checked by `UpdateFromRoster`:

```csharp
            if (!student.IsActive)
            {
                student.Reactivate(teacher, car);
            }
```

- [ ] **Step 5: Add `GetIncludingDeletedAsync` to both repositories**

`ITeacherRepository.cs`, after `GetAsync`:

```csharp
    Task<Teacher?> GetIncludingDeletedAsync(TeacherId id);
```

`TeacherRepository.cs`, after `GetAsync`:

```csharp
    public async Task<Teacher?> GetIncludingDeletedAsync(TeacherId id)
    {
        return await dbContext.Teachers
                              .IgnoreQueryFilters()
                              .FirstOrDefaultAsync(x => x.Id == id);
    }
```

`ICarRepository.cs`, after `GetAsync`:

```csharp
    Task<Car?> GetIncludingDeletedAsync(CarId id);
```

`CarRepository.cs`, after `GetAsync` (the `TeacherAssignments` are an owned collection and load with the Car):

```csharp
    public async Task<Car?> GetIncludingDeletedAsync(CarId id)
    {
        return await dbContext.Cars
                              .IgnoreQueryFilters()
                              .FirstOrDefaultAsync(x => x.Id == id);
    }
```

Match the indentation the surrounding repositories use for multi-line LINQ chains (see `StudentRepository.cs`).

- [ ] **Step 6: Write the interactor tests, then change the interactor**

Replace `ReactivateStudentInteractorTest.cs` with:

```csharp
using DrivingLessons.Application.Commands.ReactivateStudent;
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
public class ReactivateStudentInteractorTest
{
    private IStudentRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ReactivateStudentInteractor interactor = null!;
    private Teacher ronit = null!;
    private Car i20 = null!;
    private Student dana = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ReactivateStudentInteractor(repository, teacherRepository, carRepository, unitOfWork);

        ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        i20.AssignTeacher(ronit);
        dana = InactiveStudent(ronit, i20);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(dana.Id)).Returns(dana);
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).Returns(ronit);
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).Returns(i20);
    }

    [TestMethod]
    public async Task Reactivates_An_Inactive_Student()
    {
        //when
        await interactor.ExecuteAsync(dana.Id.Value);

        //then
        dana.IsActive.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Active_Student_Is_Rejected()
    {
        //given
        dana.Reactivate(ronit, i20);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentAlreadyActiveException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_With_A_Deleted_Teacher_Is_Rejected()
    {
        //given
        ronit.Delete([dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentTeacherMustNotBeDeletedException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_With_A_Deleted_Car_Is_Rejected()
    {
        //given
        i20.Delete([dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentCarMustNotBeDeletedException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_Whose_Car_Is_No_Longer_Their_Teachers_Is_Rejected()
    {
        //given
        i20.UnassignTeacher(ronit, [dana]);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        dana.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //given
        A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).Returns((Teacher?)null);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).Returns((Car?)null);

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student InactiveStudent(Teacher teacher, Car car)
    {
        var student = Student.Create(
            NationalId.Of("311078547"),
            StudentName.Of("דנה ששון"),
            PhoneNumber.Of("052-9038816"),
            teacher,
            car,
            null,
            null,
            null);
        student.Deactivate();

        return student;
    }
}
```

The catch-all `A<TeacherId>._` / `A<CarId>._` returning null means the happy path only passes if the interactor asks for **the Student's own** `TeacherId` and `CarId` (Decision 7). Because only `GetIncludingDeletedAsync` is faked, a regression to `GetAsync` would surface as `TeacherNotFoundException` (Review Focus 2).

Replace `ReactivateStudentInteractor.cs` with:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReactivateStudent;

public class ReactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public ReactivateStudentInteractor(
        IStudentRepository repository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var teacher = await teacherRepository.GetIncludingDeletedAsync(student.TeacherId)
                      ?? throw new TeacherNotFoundException(student.TeacherId);

        var car = await carRepository.GetIncludingDeletedAsync(student.CarId)
                  ?? throw new CarNotFoundException(student.CarId);

        student.Reactivate(teacher, car);

        await unitOfWork.CommitAsync();
    }
}
```

Check the `TeacherNotFoundException` / `CarNotFoundException` constructors in `Application\Common\Exceptions\` and match them (both take the typed ID elsewhere). DI needs no change: interactors are resolved by constructor.

- [ ] **Step 7: Run the whole backend test suite**

Run: `dotnet test`
Expected: all green, including `StudentTest`, `ReactivateStudentInteractorTest`, and the existing `ImportRosterInteractorTest` that reactivates an Inactive Student (Review Focus 3). If any other caller of `Reactivate()` fails to compile (`git grep -n "\.Reactivate(" -- src tests`), pass it the Student's resolved Teacher and Car.

- [ ] **Step 8: Commit**

```bash
git add src tests
git commit -m "feat(students): refuse reactivating a Student whose Teacher or Car was removed (#108)"
```
