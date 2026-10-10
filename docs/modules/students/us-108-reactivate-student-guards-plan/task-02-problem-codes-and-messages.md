# Task 2: Problem Codes and Students Screen Messages

Part of [#108 plan](README.md). Read the README's Decisions and Global Constraints first.

**Files:**
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (`errors` section)
- Test: `client\src\app\features\students\state\students.store.spec.ts`

**Interfaces:**
- Consumes (task 1): `StudentTeacherMustNotBeDeletedException(StudentId, TeacherId)`, `StudentCarMustNotBeDeletedException(StudentId, CarId)`.
- Produces: problem codes `studentTeacherMustNotBeDeleted` and `studentCarMustNotBeDeleted` (409, no `params`); translation keys `errors.studentTeacherMustNotBeDeleted` and `errors.studentCarMustNotBeDeleted`; reworded `errors.studentCarMustBeAssignedToTeacher`.

No production C# or TypeScript changes. `ApiExceptionFilter` derives the code from the class name, and `StudentsStore.reactivate` already toasts `errors.{code}` through `toast.apiError`. These tests pin that behaviour. They pass as soon as they're written, so step 2 confirms green instead of red.

- [ ] **Step 1: Add the filter tests**

In `ApiExceptionFilterTest.cs`, after `Student_Car_Not_Of_Teacher_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void Student_Teacher_Deleted_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentTeacherMustNotBeDeletedException(StudentId.New(), TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentTeacherMustNotBeDeleted");
        problem.Extensions.ShouldNotContainKey("params");
    }

    [TestMethod]
    public void Student_Car_Deleted_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentCarMustNotBeDeletedException(StudentId.New(), CarId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentCarMustNotBeDeleted");
        problem.Extensions.ShouldNotContainKey("params");
    }
```

If the filter's params key constant isn't `"params"`, use the literal the neighbouring `National_Id_In_Use_...` test asserts.

- [ ] **Step 2: Run the filter tests**

Run: `dotnet test tests/DrivingLessons.Application.Test --filter "FullyQualifiedName~ApiExceptionFilterTest"`
Expected: PASS.

- [ ] **Step 3: Add and reword the translations**

`he.json`, in `errors`, keeping the existing alphabetical neighbourhood (next to `studentCarMustBeAssignedToTeacher`):

```json
    "studentCarMustBeAssignedToTeacher": "הרכב לא משויך למורה של התלמיד. אפשר לשייך אותו למורה במסך רכבים ומורים, או לבחור רכב אחר.",
    "studentCarMustNotBeDeleted": "הרכב של התלמיד נמחק. כדי לסמן את התלמיד כפעיל, יש לייבא אותו שוב ברשימת התלמידים עם מורה ורכב קיימים.",
    "studentTeacherMustNotBeDeleted": "המורה של התלמיד נמחק. כדי לסמן את התלמיד כפעיל, יש לייבא אותו שוב ברשימת התלמידים עם מורה ורכב קיימים.",
```

`en.json`, same positions:

```json
    "studentCarMustBeAssignedToTeacher": "The Car is not assigned to the Student's Teacher. Assign it to the Teacher on Cars & teachers, or choose another Car.",
    "studentCarMustNotBeDeleted": "This Student's Car was deleted. To reactivate the Student, import them again in the Roster with a current Teacher and Car.",
    "studentTeacherMustNotBeDeleted": "This Student's Teacher was deleted. To reactivate the Student, import them again in the Roster with a current Teacher and Car.",
```

Place `studentTeacherMustNotBeDeleted` where alphabetical order puts it in each file (the `errors` section is sorted). Use plain hyphens only.

- [ ] **Step 4: Pin the Reactivate refusals in the store spec**

In `students.store.spec.ts`, after `'reports any other Reactivate failure without reloading'`, inside the same `describe`:

```ts
    it.each(['studentTeacherMustNotBeDeleted', 'studentCarMustNotBeDeleted', 'studentCarMustBeAssignedToTeacher'])(
        'explains a %s Reactivate refusal without reloading',
        async (code) => {
            //given
            const failure = problem(HTTP_CONFLICT, code);
            const { store, toast, api } = createStore({ reactivateStudent: () => throwError(() => failure) });
            await stable();

            //when
            await store.reactivate(DANA);
            await stable();

            //then
            expect(toast.apiError).toHaveBeenCalledWith(failure);
            expect(api.findStudents).toHaveBeenCalledTimes(1);
            expect(store.isMutating()).toBe(false);
        },
    );
```

`problem`, `HTTP_CONFLICT`, `DANA`, `stable` and `createStore` are the spec's existing helpers. Use them as the neighbouring Reactivate tests do.

- [ ] **Step 5: Run the client tests**

Run (memory "Client build / npm workaround"), from `client\`: `npx ng test --watch=false`
Expected: all green, including `translations.spec.ts` (no typographic punctuation) and the three new cases.

- [ ] **Step 6: Commit**

```bash
git add tests client/public/i18n client/src/app/features/students/state/students.store.spec.ts
git commit -m "feat(students): explain why a Student can't be reactivated (#108)"
```
