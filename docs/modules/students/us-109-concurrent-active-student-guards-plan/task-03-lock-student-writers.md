# Task 3 of 6: Add Student, Change Teacher, Change Car, Reactivate and the Roster import lock before they read

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires task 1 committed (task 2 is independent of this one).

**Files:**
- Modify: `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ChangeStudentTeacher\ChangeStudentTeacherInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ChangeStudentCar\ChangeStudentCarInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ReactivateStudent\ReactivateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\CreateStudentInteractorTest.cs`, `ChangeStudentTeacherInteractorTest.cs`, `ChangeStudentCarInteractorTest.cs`, `ReactivateStudentInteractorTest.cs`, `ImportRosterInteractorTest.cs`

**Interfaces:**
- Consumes (task 1): `ITeacherAndCarLocks` (`DrivingLessons.Application.Common`), with `LockAsync(IReadOnlyCollection<TeacherId>, IReadOnlyCollection<CarId>)` and `LockAllAsync()`.
- Produces: the five interactors' constructors gain `ITeacherAndCarLocks locks` immediately before `IUnitOfWork unitOfWork`. In `ImportRosterInteractor`, `unitOfWork` is followed by `TimeProvider`; `locks` still goes right before `unitOfWork`.

**Why:** These commands put an active Student on a Teacher and Car, after checking that the Teacher and Car aren't deleted and that the Car is the Teacher's (#95, #108, #92). Locking the same rows that the removals in task 2 lock makes the two sides take turns. Change Car and Reactivate first read the Student to learn its Teacher and Car. That read happens before the lock, and a parallel change to the same Student is caught by the token in task 4 (README decision 5).

- [ ] **Step 1: Wire the fake into the five test files**

In each file: add the field `private ITeacherAndCarLocks locks = null!;` after the other fakes, create it in `Init()` with `locks = A.Fake<ITeacherAndCarLocks>();` before the interactor is built, and pass `locks` immediately before `unitOfWork`:

- `new CreateStudentInteractor(repository, teacherRepository, carRepository, locks, unitOfWork)`
- `new ChangeStudentTeacherInteractor(repository, teacherRepository, carRepository, locks, unitOfWork)`
- `new ChangeStudentCarInteractor(repository, teacherRepository, carRepository, locks, unitOfWork)`
- `new ReactivateStudentInteractor(repository, teacherRepository, carRepository, locks, unitOfWork)`
- `ImportRosterInteractor`:

```csharp
        interactor = new ImportRosterInteractor(
            parser,
            studentRepository,
            rosterImportRepository,
            teacherRepository,
            carRepository,
            locks,
            unitOfWork,
            TimeProvider.System);
```

- [ ] **Step 2: Write the failing ordering tests**

`CreateStudentInteractorTest.cs` (it has the helper `RequestFor(string nationalId, Guid teacherId, Guid carId)` and the constant `NationalIdValue`):

```csharp
    [TestMethod]
    public async Task Locks_The_Teacher_And_Car_Before_Reading_Them()
    {
        //given
        var request = RequestFor(NationalIdValue, teacher.Id.Value, car.Id.Value);

        //when
        await interactor.ExecuteAsync(request);

        //then
        A.CallTo(() => locks.LockAsync(
                A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { teacher.Id }),
                A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { car.Id })))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.GetAsync(car.Id)).MustHaveHappened());
    }
```

`ChangeStudentTeacherInteractorTest.cs` (Noa is on Ronit and the Corolla; Yael teaches on the i20):

```csharp
    [TestMethod]
    public async Task Locks_The_New_Teacher_And_Car_Before_Reading_Them()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        A.CallTo(() => locks.LockAsync(
                A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { yael.Id }),
                A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { i20.Id })))
            .MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => teacherRepository.GetAsync(yael.Id)).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.GetAsync(i20.Id)).MustHaveHappened());
    }
```

`ChangeStudentCarInteractorTest.cs` (Noa is on Ronit and the Corolla; the i20 is also Ronit's):

```csharp
    [TestMethod]
    public async Task Locks_The_Students_Teacher_And_The_New_Car_Before_Reading_Them()
    {
        //given
        var request = new ChangeStudentCarRequest(i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        A.CallTo(() => repository.GetAsync(noa.Id)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => locks.LockAsync(
                    A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { ronit.Id }),
                    A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { i20.Id })))
                .MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => teacherRepository.GetAsync(ronit.Id)).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.GetAsync(i20.Id)).MustHaveHappened());
    }
```

`ReactivateStudentInteractorTest.cs` (Dana is an Inactive Student on Ronit and the i20):

```csharp
    [TestMethod]
    public async Task Locks_The_Students_Teacher_And_Car_Before_Reading_Them()
    {
        //when
        await interactor.ExecuteAsync(dana.Id.Value);

        //then
        A.CallTo(() => repository.GetAsync(dana.Id)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => locks.LockAsync(
                    A<IReadOnlyCollection<TeacherId>>.That.IsSameSequenceAs(new[] { ronit.Id }),
                    A<IReadOnlyCollection<CarId>>.That.IsSameSequenceAs(new[] { i20.Id })))
                .MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => teacherRepository.GetIncludingDeletedAsync(ronit.Id)).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.GetIncludingDeletedAsync(i20.Id)).MustHaveHappened());
    }
```

`ImportRosterInteractorTest.cs` (it has the helpers `RowsAre(...)`, `Row(rowNumber, fullName, nationalId)` and `Request()`):

```csharp
    [TestMethod]
    public async Task Locks_Every_Teacher_And_Car_Before_Reading_Them()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        A.CallTo(() => locks.LockAllAsync()).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => teacherRepository.FindActiveAsync()).MustHaveHappened())
            .Then(A.CallTo(() => carRepository.FindActiveAsync()).MustHaveHappened())
            .Then(A.CallTo(() => studentRepository.FindAllAsync()).MustHaveHappened());
    }
```

`A.CallTo(...).MustHaveHappenedOnceExactly()` returns an ordered assertion that `.Then(...)` chains, so each test fails if a read comes before the lock.

- [ ] **Step 3: Run them and see them fail**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateStudentInteractorTest|FullyQualifiedName~ChangeStudentTeacherInteractorTest|FullyQualifiedName~ChangeStudentCarInteractorTest|FullyQualifiedName~ReactivateStudentInteractorTest|FullyQualifiedName~ImportRosterInteractorTest"
```

Expected: build error `CS1729` (the constructors don't take the extra argument).

- [ ] **Step 4: Add the dependency to the five interactors**

In each interactor, add `private readonly ITeacherAndCarLocks locks;` right before the `unitOfWork` field, and add the constructor parameter `ITeacherAndCarLocks locks` right before `IUnitOfWork unitOfWork`, assigned with `this.locks = locks;`. Each file already imports `DrivingLessons.Application.Common` for `IUnitOfWork`.

- [ ] **Step 5: Lock first in each `ExecuteAsync`**

`CreateStudentInteractor.ExecuteAsync`: replace the block from `var teacherId = TeacherId.Of(request.TeacherId);` to the `car` lookup with:

```csharp
        var teacherId = TeacherId.Of(request.TeacherId);
        var carId = CarId.Of(request.CarId);

        await locks.LockAsync([teacherId], [carId]);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);
```

(The national-ID check, `await NationalIdMustBeFreeAsync(nationalId);`, stays before it, so a duplicate ID is refused without taking locks.)

`ChangeStudentTeacherInteractor.ExecuteAsync` becomes:

```csharp
    public async Task ExecuteAsync(Guid id, ChangeStudentTeacherRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var teacherId = TeacherId.Of(request.TeacherId);
        var carId = CarId.Of(request.CarId);

        await locks.LockAsync([teacherId], [carId]);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeTeacher(teacher, car);

        await unitOfWork.CommitAsync();
    }
```

`ChangeStudentCarInteractor.ExecuteAsync` becomes:

```csharp
    public async Task ExecuteAsync(Guid id, ChangeStudentCarRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var teacherId = student.TeacherId;
        var carId = CarId.Of(request.CarId);

        await locks.LockAsync([teacherId], [carId]);

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            throw new TeacherNotFoundException(teacherId);
        }

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeCar(car);

        await unitOfWork.CommitAsync();
    }
```

(Keep whatever the current method does with `teacher` after the null check; only the lines above `var teacher` change. If `teacher` is otherwise unused, that's the existing code and stays as it is.)

`ReactivateStudentInteractor.ExecuteAsync` becomes:

```csharp
    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        await locks.LockAsync([student.TeacherId], [student.CarId]);

        var teacher = await teacherRepository.GetIncludingDeletedAsync(student.TeacherId)
                      ?? throw new TeacherNotFoundException(student.TeacherId);

        var car = await carRepository.GetIncludingDeletedAsync(student.CarId)
                  ?? throw new CarNotFoundException(student.CarId);

        student.Reactivate(teacher, car);

        await unitOfWork.CommitAsync();
    }
```

`ImportRosterInteractor.ExecuteAsync`: right after `var rows = parser.Parse(request.Content);`, add a blank line and:

```csharp
        await locks.LockAllAsync();
```

so it runs before `teacherRepository.FindActiveAsync()`.

- [ ] **Step 6: Run the tests**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateStudentInteractorTest|FullyQualifiedName~ChangeStudentTeacherInteractorTest|FullyQualifiedName~ChangeStudentCarInteractorTest|FullyQualifiedName~ReactivateStudentInteractorTest|FullyQualifiedName~ImportRosterInteractorTest"
```

Expected: every test in the five classes passes, old and new. Then run the whole suite once, `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`, and expect 0 failures and no warnings.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Commands/CreateStudent/CreateStudentInteractor.cs src/DrivingLessons.Application/Commands/ChangeStudentTeacher/ChangeStudentTeacherInteractor.cs src/DrivingLessons.Application/Commands/ChangeStudentCar/ChangeStudentCarInteractor.cs src/DrivingLessons.Application/Commands/ReactivateStudent/ReactivateStudentInteractor.cs src/DrivingLessons.Application/Commands/ImportRoster/ImportRosterInteractor.cs tests/DrivingLessons.Application.Test/Commands/CreateStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ChangeStudentTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ChangeStudentCarInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ReactivateStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ImportRosterInteractorTest.cs
git commit -m "feat(students): lock the Teacher and Car before placing an active Student on them (#109)"
```

End the commit message with the attribution trailer from the session's instructions.
