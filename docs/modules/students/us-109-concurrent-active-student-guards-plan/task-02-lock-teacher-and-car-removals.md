# Task 2 of 6: Delete Teacher, Delete Car and Unassign lock before they read

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires task 1 committed.

**Files:**
- Modify: `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\DeleteCar\DeleteCarInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\UnassignCarFromTeacher\UnassignCarFromTeacherInteractor.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs`, `DeleteCarInteractorTest.cs`, `UnassignCarFromTeacherInteractorTest.cs`

**Interfaces:**
- Consumes (task 1): `ITeacherAndCarLocks` (`DrivingLessons.Application.Common`), with `Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds)`.
- Produces: the three interactors' constructors gain `ITeacherAndCarLocks locks` immediately before `IUnitOfWork unitOfWork`. `Presentation.Web` resolves interactors from DI, so nothing else changes.

**Why:** These three commands check "no active Student depends on this" (#91) and then remove the Teacher, the Car or the assignment. With the lock taken before the check, a parallel Add Student, Change Teacher, Change Car, Reactivate or Roster import on the same Teacher or Car either finishes first, so the check sees its Student, or waits until the removal has committed (README decision 3).

- [ ] **Step 1: Write the failing ordering tests**

In each of the three test files: add the field `private ITeacherAndCarLocks locks = null!;` after the other fakes, create it in `Init()` with `locks = A.Fake<ITeacherAndCarLocks>();` before the interactor is built, and pass `locks` immediately before `unitOfWork` in the `new ...Interactor(` call. The files already import `DrivingLessons.Application.Common` (for `IUnitOfWork`) and `DrivingLessons.Domain.Values`.

`DeleteTeacherInteractorTest.cs`: the constructor call becomes `new DeleteTeacherInteractor(repository, userQueries, studentRepository, locks, unitOfWork)`. Add:

```csharp
    [TestMethod]
    public async Task Locks_The_Teacher_Before_Reading_It_Or_Its_Students()
    {
        //when
        await interactor.ExecuteAsync(teacher.Id.Value);

        //then
        A.CallTo(() => locks.LockAsync(
                A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { teacher.Id }),
                A<IReadOnlyCollection<CarId>>.That.IsEmpty()))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => repository.GetAsync(teacher.Id)).MustHaveHappened())
            .Then(A.CallTo(() => studentRepository.FindActiveByTeacherAsync(teacher.Id)).MustHaveHappened());
    }
```

`DeleteCarInteractorTest.cs`: the constructor call becomes `new DeleteCarInteractor(repository, studentRepository, locks, unitOfWork)`. Add:

```csharp
    [TestMethod]
    public async Task Locks_The_Car_Before_Reading_It_Or_Its_Students()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value);

        //then
        A.CallTo(() => locks.LockAsync(
                A<IReadOnlyCollection<TeacherId>>.That.IsEmpty(),
                A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { car.Id })))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => repository.GetAsync(car.Id)).MustHaveHappened())
            .Then(A.CallTo(() => studentRepository.FindActiveByCarAsync(car.Id)).MustHaveHappened());
    }
```

`UnassignCarFromTeacherInteractorTest.cs`: the constructor call becomes

```csharp
        interactor = new UnassignCarFromTeacherInteractor(
            carRepository,
            teacherRepository,
            studentRepository,
            locks,
            unitOfWork);
```

Add:

```csharp
    [TestMethod]
    public async Task Locks_The_Teacher_And_Car_Before_Reading_Them()
    {
        //when
        await interactor.ExecuteAsync(car.Id.Value, teacher.Id.Value);

        //then
        A.CallTo(() => locks.LockAsync(
                A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { teacher.Id }),
                A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { car.Id })))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => carRepository.GetAsync(car.Id)).MustHaveHappened())
            .Then(A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).MustHaveHappened())
            .Then(A.CallTo(() => studentRepository.FindActiveByCarAsync(car.Id)).MustHaveHappened());
    }
```

(If the existing tests call `ExecuteAsync` with the arguments in another order, match them: the interactor's signature is `ExecuteAsync(Guid id, Guid teacherId)`, with the Car id first.)

- [ ] **Step 2: Run them and see them fail**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeleteTeacherInteractorTest|FullyQualifiedName~DeleteCarInteractorTest|FullyQualifiedName~UnassignCarFromTeacherInteractorTest"
```

Expected: build error `CS1729` (the constructors don't take 5 / 4 / 5 arguments).

- [ ] **Step 3: Lock first in `DeleteTeacherInteractor`**

Add the field and constructor parameter (immediately before `unitOfWork`):

```csharp
    private readonly ITeacherRepository repository;
    private readonly IUserQueries userQueries;
    private readonly IStudentRepository studentRepository;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public DeleteTeacherInteractor(
        ITeacherRepository repository,
        IUserQueries userQueries,
        IStudentRepository studentRepository,
        ITeacherAndCarLocks locks,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.userQueries = userQueries;
        this.studentRepository = studentRepository;
        this.locks = locks;
        this.unitOfWork = unitOfWork;
    }
```

`ExecuteAsync` becomes:

```csharp
    public async Task ExecuteAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        await locks.LockAsync([teacherId], []);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var hasActiveUser = await userQueries.ActiveExistsLinkedToTeacherAsync(teacherId);

        if (hasActiveUser)
        {
            throw new TeacherMustNotHaveActiveUserException(teacherId);
        }

        var students = await studentRepository.FindActiveByTeacherAsync(teacherId);

        teacher.Delete(students);

        await unitOfWork.CommitAsync();
    }
```

- [ ] **Step 4: Lock first in `DeleteCarInteractor`**

Add `private readonly ITeacherAndCarLocks locks;` and the constructor parameter `ITeacherAndCarLocks locks` immediately before `IUnitOfWork unitOfWork`, assigned with `this.locks = locks;`. `ExecuteAsync` becomes:

```csharp
    public async Task ExecuteAsync(Guid id)
    {
        var carId = CarId.Of(id);

        await locks.LockAsync([], [carId]);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var students = await studentRepository.FindActiveByCarAsync(carId);

        car.Delete(students);

        await unitOfWork.CommitAsync();
    }
```

- [ ] **Step 5: Lock first in `UnassignCarFromTeacherInteractor`**

Add `private readonly ITeacherAndCarLocks locks;` and the constructor parameter `ITeacherAndCarLocks locks` immediately before `IUnitOfWork unitOfWork`, assigned with `this.locks = locks;`. `ExecuteAsync` becomes (the Teacher id is now resolved before the Car is read):

```csharp
    public async Task ExecuteAsync(Guid id, Guid teacherId)
    {
        var carId = CarId.Of(id);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        await locks.LockAsync([resolvedTeacherId], [carId]);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var teacher = await teacherRepository.GetAsync(resolvedTeacherId)
                      ?? throw new TeacherNotFoundException(resolvedTeacherId);

        var students = await studentRepository.FindActiveByCarAsync(carId);

        car.UnassignTeacher(teacher, students);

        await unitOfWork.CommitAsync();
    }
```

Each file needs `using DrivingLessons.Application.Common;`, which it already has for `IUnitOfWork`.

- [ ] **Step 6: Run the tests**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeleteTeacherInteractorTest|FullyQualifiedName~DeleteCarInteractorTest|FullyQualifiedName~UnassignCarFromTeacherInteractorTest"
```

Expected: every test in the three classes passes, old and new. Then run the whole Application suite once, `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`, and expect it to pass with no warnings. It also catches any other `new DeleteCarInteractor(` call site, if one exists.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Commands/DeleteTeacher/DeleteTeacherInteractor.cs src/DrivingLessons.Application/Commands/DeleteCar/DeleteCarInteractor.cs src/DrivingLessons.Application/Commands/UnassignCarFromTeacher/UnassignCarFromTeacherInteractor.cs tests/DrivingLessons.Application.Test/Commands/DeleteTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/DeleteCarInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/UnassignCarFromTeacherInteractorTest.cs
git commit -m "feat(teachers): lock the Teacher and Car before checking their active Students on delete and unassign (#109)"
```

End the commit message with the attribution trailer from the session's instructions.
