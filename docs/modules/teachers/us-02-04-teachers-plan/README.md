# US-02–04: Teachers Module — Task Index

Per-task breakdown of [../us-02-04-teachers-plan.md](../us-02-04-teachers-plan.md) (the source of truth). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** Implement GitHub issues #3 (US-02 create teacher), #4 (US-03 add cars with transmission), #5 (US-04 edit teacher/car details) — the first real DDD aggregate slice, scaffolding the domain building blocks, test project, rules-compliant infrastructure, and client feature architecture.

**User decisions (locked):** no maximum cars (min-one binds on future removal) · domain tests only (TDD) · field-group edits, no-op edits accepted · `AppDbContext` → `DrivingLessonsDbContext` in `EntityFramework\` · `IUnitOfWork` in `Application\Common\`, no `UnitOfWork` on repositories · `CommitAsync` = `SaveChangesAsync` only · `[EndpointSummary]` not `[SwaggerOperation]` · lowercase Transloco namespaces · UX from `Driving Lesson Mockup\mock\admin.jsx`.

**Branch:** `3-us-02-04-teachers-module`

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-building-blocks-and-tests.md](task-01-building-blocks-and-tests.md) | Domain base types + Domain.Test project | ✅ own commit |
| 2 | [task-02-values.md](task-02-values.md) | Typed IDs + value objects + exceptions (TDD) | ✅ own commit |
| 3 | [task-03-teacher-aggregate.md](task-03-teacher-aggregate.md) | Teacher aggregate + Car + events (TDD) | ✅ own commit |
| 4 | [task-04-application-layer.md](task-04-application-layer.md) | Interactors, queries, DTOs, exceptions, DI | ✅ own commit |
| 5 | [task-05-infrastructure-restructure.md](task-05-infrastructure-restructure.md) | DbContext rename/move, IUnitOfWork, config scanning | ✅ own commit |
| 6 | [task-06-teachers-persistence.md](task-06-teachers-persistence.md) | Converters, TeacherConfiguration, repo, queries, migration | ✅ own commit |
| 7 | [task-07-controllers.md](task-07-controllers.md) | Command/query controllers + enum JSON | ✅ own commit |
| 8 | [task-08-client-plumbing.md](task-08-client-plumbing.md) | ToastService, ProblemDetails, AppRoutes | ✅ own commit |
| 9 | [task-09-teachers-feature-state.md](task-09-teachers-feature-state.md) | Client models, API service, signal store | ⏳ commits with task 10 |
| 10 | [task-10-teachers-feature-ui.md](task-10-teachers-feature-ui.md) | Page, cards, dialogs, routing, nav, i18n | ✅ commits tasks 9–10 |
| 11 | [task-11-rtl-audit.md](task-11-rtl-audit.md) | Hebrew/RTL pass | ✅ own commit if changes |
| 12 | [task-12-manual-verification.md](task-12-manual-verification.md) | Acceptance walkthrough | — |

## How to Run a Task

1. Confirm you are on branch `3-us-02-04-teachers-module` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\
├── Common\        EntityId, Entity, AggregateRoot, IDomainEvent   (+ existing DomainException)
├── Entities\      Teacher, Car
├── Events\        TeacherCreated, CarAdded, TeacherDetailsChanged, CarDetailsChanged
├── Exceptions\    5 domain exceptions
├── Repositories\  ITeacherRepository
└── Values\        TeacherId, CarId, TeacherName, Email, CarName, CarType, Transmission
src\DrivingLessons.Application\
├── Common\        IUnitOfWork  (+ existing Exceptions\ + TeacherNotFoundException, CarNotFoundException)
├── Commands\      CreateTeacher, AddCar, ChangeTeacherDetails, ChangeCarDetails
└── Queries\       ITeacherQueries, GetTeacher, FindTeachers
src\DrivingLessons.Infrastructure\EntityFramework\          (renamed from Persistence\)
├── DrivingLessonsDbContext.cs                              (renamed from AppDbContext)
├── EntityConfigurations\  AdminUserConfiguration, TeacherConfiguration, Converters\
├── Migrations\            existing InitialCreate (ns fixed) + new AddTeachers
├── Queries\               TeacherQueries
└── Repositories\          TeacherRepository
src\DrivingLessons.Presentation.Web\Controllers\Teacher\    TeacherCommandController, TeacherQueryController
tests\DrivingLessons.Domain.Test\                           Common\Faker, Entities\ + Fake\, Values\
client\src\app\
├── core\services\toast.service.ts
├── shared\{models\problem-details.ts, config\app-routes.ts}
└── features\teachers\{domain, data, state, ui}\
```
