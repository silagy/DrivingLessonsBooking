# US-02–04: Teachers Module — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Per-task files with full code live in [us-02-04-teachers-plan/](us-02-04-teachers-plan/README.md).

**Goal:** Implement GitHub issues [#3 (US-02)](https://github.com/silagy/DrivingLessonsBooking/issues/3) admin creates a teacher, [#4 (US-03)](https://github.com/silagy/DrivingLessonsBooking/issues/4) admin adds cars with transmission, and [#5 (US-04)](https://github.com/silagy/DrivingLessonsBooking/issues/5) admin edits teacher and car details — the first real DDD aggregate slice, which also scaffolds the domain building blocks, the test project, rules-compliant infrastructure, and the client feature architecture every later module copies.

**Architecture:** Modular monolith, DDD, Onion. `Teacher` is an `AggregateRoot` owning `Car` child entities; all mutations go through the root with guards and domain events. CQRS: command interactors via `ITeacherRepository` + `IUnitOfWork`; queries project to DTOs via `ITeacherQueries` + static `Selector`. Client: first `domain/data/state/ui` feature with a signal store, `resource()` reads, DynamicDialog forms, per the mockup in `Driving Lesson Mockup\mock\admin.jsx` (`AdminTeachers`).

**Tech Stack:** .NET 10 / EF Core 10 + Npgsql / MSTest + Shouldly (new test project) · Angular 21 + PrimeNG 21 + Transloco 8 (existing workspace).

**Branch:** `3-us-02-04-teachers-module` (off `main`, after US-01 merge PR #54).

**Out of scope:** US-49 roster CSV (#51) despite its `module:teachers` label · car/teacher deletion — and with it the "at least one car" rule, which binds as *"the last car can never be removed"* and only activates when car removal is built · fleet single/dual-transmission derived logic — the "participates in fleet logic" clause in US-03 is obsolete (transmission comes from the roster, students are never asked); storing `Transmission` per car is the full obligation.

## Context

US-01 (auth) deliberately shipped **pre-aggregate** infrastructure: `Domain` holds only `DomainException`; there are no base types, no `IUnitOfWork`, no repositories/EntityConfigurations/converters, no test project, and a bare sealed `AppDbContext` in `Infrastructure\Persistence\`. The client shipped a thin skeleton: no feature layering, no signal store or data service, no ToastService/ProblemDetails handling, no AppRoutes constants, no DialogService usage, no shell navigation. This slice turns the `.claude/rules/*.md` templates into code, making Teachers the reference implementation for WeekSchedule, Publication, Student, and Submission.

### Acceptance criteria

- **US-02:** signed-in admin creates a teacher (name + contact email) → appears in the teachers list. ("Selectable in weekly preparation" is enabled by `GET api/teachers/find`; verified for real in the week-schedules slice.)
- **US-03:** admin adds a car (display name, vehicle type, transmission Automatic/Manual) to a teacher → car appears under the teacher. Issue #4's "fewer than two cars" precondition is **obsolete — product owner corrected the rule: no maximum**; the real invariant is "at least one car", binding only on future car removal.
- **US-04:** admin edits a teacher's name/contact email or a car's name, type, or transmission → updated values shown after reload; future consumers (Excel, roster) read live data by construction.

### Locked decisions

| Decision | Choice |
|---|---|
| Cars rule | **No maximum** cars per teacher (requirements' "1–2" was outdated); minimum one = "last car can never be removed" — dormant until car removal exists; a teacher may be created carless; `docs/requirements.md` §5.1/§5.2 and `CLAUDE.md` amended in this slice |
| Tests | **Domain tests only** — new `tests\DrivingLessons.Domain.Test` (MSTest + Shouldly + Faker + FakeBuilders), TDD |
| Edit shape | Field-group updates: `PUT api/teachers/{id}/details`, `PUT api/teachers/{id}/cars/{carId}` |
| No-op edits | Accepted silently — mutate + raise event unconditionally (the non-idempotency rule targets state transitions; Teacher has no state machine) |
| DbContext | Rename `AppDbContext` → `DrivingLessonsDbContext`, move `Persistence\` → `EntityFramework\`, `ApplyConfigurationsFromAssembly`, fix existing migration namespaces — **migration IDs and class names unchanged** |
| `IUnitOfWork` | Defined in `Application\Common\`; repositories do **not** expose a `UnitOfWork` property — interactors inject `IUnitOfWork` as a separate dependency (resolves the ddd-architecture.md self-contradiction; rules doc gets a follow-up correction) |
| Domain events | `CommitAsync()` = `SaveChangesAsync()` only — no dispatcher/clearing until the first real handler (Publication slice) |
| AddCar reply | `POST api/teachers/{id}/cars` → **201 + `AddCarResponse`** with the new car id |
| Client plumbing | Built this slice: ToastService, ProblemDetails + `apiError`, `shared\config\app-routes.ts`, first signal store + data service, DynamicDialog pattern, shell topbar nav |
| i18n | Lowercase Transloco namespaces (`teachers`, `general`, `formErrors`) in `client\public\i18n\{en,he}.json`, keys always mirrored |
| OpenAPI | `[EndpointSummary]`/`[Tags]`/`[ProducesResponseType]` — native AddOpenApi attributes, **no Swashbuckle, no `[SwaggerOperation]`** |
| UX source | `Driving Lesson Mockup\mock\admin.jsx` → `AdminTeachers` + `AdminShell` |

## Domain Design

```
Domain\Entities\Teacher.cs        AggregateRoot<TeacherId>
    TeacherName Name
    Email ContactEmail
    IReadOnlyCollection<Car> Cars   (private List<Car> cars, unbounded)

    static Create(TeacherName, Email)                      → TeacherCreated
    AddCar(CarName, CarType, Transmission) : Car           → CarAdded (no count guard)
    ChangeDetails(TeacherName, Email)                      → TeacherDetailsChanged
    ChangeCarDetails(Car, CarName, CarType, Transmission)  → MustOwnCar → CarDetailsChanged
                                                             (delegates to internal Car.ChangeDetails)

Domain\Entities\Car.cs            Entity<CarId> — no events; internal Create factory + internal ChangeDetails mutator
    CarName Name, CarType Type, Transmission Transmission
```

Interactor/domain split for car edits: the **interactor** resolves `carId` in `teacher.Cars` and throws `CarNotFoundException` (404, "does it exist"); the **domain** method guards `CarNotInTeacherException` (409, "is it allowed").

| Kind | Types |
|---|---|
| Typed IDs | `TeacherId`, `CarId` |
| Value objects | `TeacherName`, `CarName`, `CarType` (trim, non-empty), `Email` (trim + lowercase + `MailAddress.TryCreate`) |
| Enum | `Transmission { Automatic = 10, Manual = 20 }` |
| Events | `TeacherCreated` · `CarAdded` · `TeacherDetailsChanged` · `CarDetailsChanged` |
| Domain exceptions | `TeacherNameMustNotBeEmptyException`, `EmailMustBeValidException`, `CarNameMustNotBeEmptyException`, `CarTypeMustNotBeEmptyException`, `CarNotInTeacherException` |
| App exceptions | `TeacherNotFoundException`, `CarNotFoundException` : `NotFoundException` |

## API Surface

| Endpoint | Interactor | Returns |
|---|---|---|
| `POST api/teachers` | `CreateTeacherInteractor` | 201 `CreateTeacherResponse` |
| `POST api/teachers/{id:guid}/cars` | `AddCarInteractor` | 201 `AddCarResponse` |
| `PUT api/teachers/{id:guid}/details` | `ChangeTeacherDetailsInteractor` | 204 |
| `PUT api/teachers/{id:guid}/cars/{carId:guid}` | `ChangeCarDetailsInteractor` | 204 |
| `GET api/teachers/find` | `FindTeachersInteractor` | 200 `ItemForFindTeachersResponse[]` |
| `GET api/teachers/{id:guid}` | `GetTeacherInteractor` | 200 `GetTeacherResponse` / 404 |

`TeacherCommandController` + `TeacherQueryController` in `Presentation.Web\Controllers\Teacher\`, route `api/teachers`, `[FromServices]` per-action injection, no constructors, `CreatedAtAction(null, result)`. Errors flow through the existing `ApiExceptionFilter`. `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` added to JSON options — first enum crossing the API.

## Task Breakdown

See [us-02-04-teachers-plan/README.md](us-02-04-teachers-plan/README.md) for the execution index. Summary:

| # | Task | Commit |
|---|------|--------|
| 01 | Domain building blocks + test project | ✅ own |
| 02 | Typed IDs + value objects (TDD) | ✅ own |
| 03 | Teacher aggregate + Car + events (TDD) | ✅ own |
| 04 | Application layer (interactors, queries, DTOs, exceptions) | ✅ own |
| 05 | Infrastructure restructure (DbContext rename/move only) | ✅ own |
| 06 | Teachers EF mapping + repository + queries + migration | ✅ own |
| 07 | Controllers + enum serialization | ✅ own |
| 08 | Client plumbing (Toast, ProblemDetails, AppRoutes) | ✅ own |
| 09 | Teachers feature: domain + data + state | ⏳ with 10 |
| 10 | Teachers feature: UI + routing + i18n | ✅ 09–10 |
| 11 | Hebrew/RTL audit | ✅ if changes |
| 12 | Manual verification | — |

Ordering rationale: inside-out backend (tests before/with domain → domain before application → **restructure isolated in 05 before new EF work in 06** so the mechanical rename diff is reviewable and drift-checkable via `dotnet ef migrations has-pending-model-changes` → controllers last); client plumbing (08) before the feature that consumes it; 09+10 commit together (a store with no consumer is dead code).

## Risks / Gotchas

1. **DbContext rename:** the snapshot's `[DbContext]` attribute is the critical one — miss it and EF re-scaffolds everything in task 06's migration. Verify with `has-pending-model-changes` before proceeding.
2. **OwnsMany defaults:** without explicit `HasKey(x => x.Id)` EF keys owned collections as (ownerFK, synthetic id); set key + converter explicitly; shadow FK configured by name `"teacher_id"`; never add a `TeacherId` CLR property to `Car`.
3. **Value-converter rehydration** runs `Of()` on reads — tightening validation later can make reads throw; accepted.
4. **`resource()` reload flash:** render from stale `value()` during reload; gate the spinner on first load / `isEmpty`.
5. **Stale rules docs:** Transloco + lowercase namespaces (not ngx-translate/PascalCase), `[EndpointSummary]` (not `[SwaggerOperation]`), and no `UnitOfWork` property on repositories are deliberate — do not "fix" them back to the rules docs.

## Verification

- `dotnet test` — all domain tests green.
- `dotnet build` + `dotnet ef migrations has-pending-model-changes` after task 05 (must report none).
- **Scalar (task 07):** authorize via bearer; create teacher → 201; add several cars → 201 each; edit details/car → 204; edit a car of another teacher → 404/409; unauthenticated → 401; `GET find` shows nested cars with camelCase transmission strings.
- **E2E (task 12):** fresh DB via docker-compose (proves the renamed-context migration chain from zero) and an upgrade run against a DB that already has the US-01 migration; browser: sign in → Teachers nav → create teacher (US-02) → add car (US-03) → edit email + car transmission, reload, values persist (US-04) → repeat key screens in Hebrew (RTL mirror check).

## Follow-ups (not this slice)

- Correct `.claude/rules/ddd-architecture.md` (IUnitOfWork location + repository template, `[SwaggerOperation]` → `[EndpointSummary]`) and `client-i18n.md` (Transloco) — record deviations in `docs/modules/teachers/us-02-04-teachers-changes.md` when the slice lands.
- Car deletion / teacher deactivation when a story calls for it (activates the min-one-car rule).
- Event dispatch plumbing with the first real handler (Publication slice).
