# CLAUDE.md

**DriveCal** (דרייבקל) - weekly demand-collection system for a driving school (see [README.md](README.md) and [docs/requirements.md](docs/requirements.md)). The logo is Option E, Wheel and calendar, from the Claude Design file `DriveCal Logo Options v2.html` (a copy is in `Driving Lesson Mockup\`); the client draws it in `client\src\app\shared\brand-logo\`, and the favicon is its steering wheel alone.

**Stack**: latest .NET, ASP.NET Core Web API, EF Core, PostgreSQL. Frontend: latest Angular + PrimeNG, lives in this repo under `client\`.
**Architecture**: modular monolith, DDD, Onion Architecture. Single deployable — Angular is served from `Presentation.Web\wwwroot`.
**Hosting**: AWS Lightsail, Docker Compose (app + postgres + caddy). See [docs/tech-stack.md](docs/tech-stack.md).

## Reference Documentation

**MANDATORY**: Read the relevant guide before implementing or reviewing code.

| Task Type | Required Reading |
|-----------|------------------|
| Any C# code | `.claude/rules/code-style.md` (auto-loaded) |
| Domain, application, or infrastructure code | `.claude/rules/ddd-architecture.md` (auto-loaded) |
| Base classes, typed IDs, value objects, entities, events | `.claude/rules/domain-building-blocks.md` (auto-loaded) |
| Controllers / endpoints | `.claude/rules/api-guidelines.md` (auto-loaded) |
| Writing or modifying tests | `.claude/rules/domain-testing.md` (auto-loaded) |
| Any Angular code in `client\` | `.claude/rules/client-architecture.md` + `client-state.md` (auto-loaded) |
| Client templates / styles / PrimeNG | `.claude/rules/client-primeng.md` (auto-loaded) |
| User-visible strings, dates, RTL | `.claude/rules/client-i18n.md` (auto-loaded) |
| Product behavior questions | `docs/requirements.md` |
| Domain terms and words to avoid | `CONTEXT.md` |
| Stack, repo layout, deployment | `docs/tech-stack.md` + `docs/decisions/` |

Backend rules govern `src\` and `tests\`; client rules govern `client\`. The client consumes the API contract defined by `api-guidelines.md` — it never redefines it.

## Critical Rules

1. **Operations are NOT idempotent** — throw a domain exception if already in the target state
2. **Domain methods accept resolved entities, typed IDs, and value objects — never raw Guids or primitives** (raw `Guid` exists only at controller/response boundaries)
3. **No comments** — code must be self-documenting (test section markers `//given //when //then` are the only exception)
4. **Always search for existing code** before creating new files
5. **Test through aggregate roots only** — never instantiate or test child entities directly
6. **Repositories return null; interactors throw `{Entity}NotFoundException`**
7. **Controllers contain zero logic** — delegate to an interactor injected via `[FromServices]`
8. **Use requirements terminology** — entity, state, and flow names come from `docs/requirements.md` and the `CONTEXT.md` glossary (Publication, Week Schedule, Slot, Submission, Slot Request, User, Role, Change Teacher, Change Car, Inactive Student). Never invent alternative names
9. **Prefer new domain events over modifying existing ones**
10. **Client state is signals only** — no NgRx, no Subject-held state; stores expose readonly signals, components never subscribe
11. **No hardcoded user-visible strings; Hebrew is RTL-first** — every string is a translation key, every layout mirror-correct
12. **Business rules live in the backend** — the client renders state and displays 409 rule violations; it never re-implements transitions

## Project Structure

```
src\
├── DrivingLessons.Domain\            entities, value objects, events, repository interfaces, exceptions
├── DrivingLessons.Application\       interactors, request/response DTOs, query interfaces
├── DrivingLessons.Infrastructure\    EF Core DbContext, repositories, query implementations, configurations
└── DrivingLessons.Presentation.Web\  controllers, exception filter, DI, startup; serves the Angular build
client\                               Angular workspace (latest Angular, PrimeNG, signals, standalone)
└── src\app\
    ├── core\                         singletons: auth, role guards, language, toast/clipboard/download services, signed-in user, PWA (service worker, new version check)
    ├── features\                     lazy bounded contexts: admin-shell, auth, teachers, week-schedules, publications, users, students, roster, student-form
    │   └── {feature}\                domain\ · data\ · state\ · ui\ (see client-architecture.md)
    ├── shared\                       feature-agnostic components, dialogs, models, dates, pipes, config (AppRoutes)
    └── theme\                        PrimeNG theme preset
tests\
├── DrivingLessons.Domain.Test\       domain unit tests (MSTest + Shouldly)
└── DrivingLessons.Application.Test\  interactor, auth, filter and infrastructure tests (MSTest + Shouldly + FakeItEasy)
docs\
└── decisions\                        ADRs, one file per decision
```

### File Location Quick Reference

| Code Type | Path Pattern | Class Name |
|-----------|--------------|------------|
| Base types (EntityId, Entity, AggregateRoot, IDomainEvent, IHasDomainEvents, DomainException) | `Domain\Common\{BaseType}.cs` | see domain-building-blocks.md |
| Entity | `Domain\Entities\{Entity}.cs` | `{Entity}` |
| Typed ID | `Domain\Values\{Entity}Id.cs` | `{Entity}Id` |
| Value object | `Domain\Values\{Value}.cs` | `{Value}` |
| Domain event | `Domain\Events\{Entity}{Action}.cs` | `{Entity}{Action}` |
| Domain exception | `Domain\Exceptions\{Entity}{Rule}Exception.cs` | `{Entity}{Rule}Exception` |
| Not-found exception | `Application\Common\Exceptions\{Entity}NotFoundException.cs` | `{Entity}NotFoundException` |
| Repository interface | `Domain\Repositories\I{Entity}Repository.cs` | `I{Entity}Repository` |
| Command + handler | `Application\Commands\{Op}{Entity}\{Op}{Entity}Interactor.cs` | `{Op}{Entity}Interactor` |
| Request DTO | `Application\Commands\{Op}{Entity}\{Op}{Entity}Request.cs` | `{Op}{Entity}Request` |
| Query + handler | `Application\Queries\{Op}{Entity}\{Op}{Entity}Interactor.cs` | `{Op}{Entity}Interactor` |
| Command response DTO | `Application\Commands\{Op}{Entity}\{Op}{Entity}Response.cs` | `{Op}{Entity}Response` |
| Query response DTO | `Application\Queries\{Op}{Entity}\{Op}{Entity}Response.cs` | `{Op}{Entity}Response` |
| List item response DTO | `Application\Queries\Find{Entities}\ItemForFind{Entities}Response.cs` | `ItemForFind{Entities}Response` |
| Query interface | `Application\Queries\I{Entity}Queries.cs` | `I{Entity}Queries` |
| Repository impl | `Infrastructure\EntityFramework\Repositories\{Entity}Repository.cs` | `{Entity}Repository` |
| Query impl | `Infrastructure\EntityFramework\Queries\{Entity}Queries.cs` | `{Entity}Queries` |
| EF configuration | `Infrastructure\EntityFramework\EntityConfigurations\{Entity}Configuration.cs` | `{Entity}Configuration` |
| Command controller | `Presentation.Web\Controllers\{Entity}\{Entity}CommandController.cs` | `{Entity}CommandController` |
| Query controller | `Presentation.Web\Controllers\{Entity}\{Entity}QueryController.cs` | `{Entity}QueryController` |
| Entity test | `Domain.Test\Entities\{Entity}Test.cs` | `{Entity}Test` |
| Value object test | `Domain.Test\Values\{Value}Test.cs` | `{Value}Test` |
| FakeBuilder | `Domain.Test\Entities\Fake\{Entity}FakeBuilder.cs` | `{Entity}FakeBuilder` |
| Command interactor test | `Application.Test\Commands\{Op}{Entity}InteractorTest.cs` | `{Op}{Entity}InteractorTest` |
| Query interactor test | `Application.Test\Queries\{Op}{Entity}InteractorTest.cs` | `{Op}{Entity}InteractorTest` |

### Where to put code?

```
Business rule        → Domain\Entities\{Entity}.cs
Validation at input  → Domain\Values\{Value}.cs (Of() factory)
Orchestration        → Application\Commands|Queries\{Op}{Entity}\{Op}{Entity}Interactor.cs
Database query       → Infrastructure\EntityFramework\Queries\{Entity}Queries.cs
HTTP endpoint        → Presentation.Web\Controllers\{Entity}\
```

### Layer Dependencies

```
Domain            → ∅
Application       → Domain
Infrastructure    → Application, Domain, EF Core, Npgsql
Presentation.Web  → Application
Domain.Test       → Domain
Application.Test  → Application, Infrastructure, Presentation.Web
```

## Domain Snapshot

Core aggregates (full model in `docs/requirements.md` §5):

- **User** — a person who can sign in; exactly one Role (`Administrator` | `Teacher`); a Teacher-Role User is linked to exactly one Teacher, fixed at creation, and reaches only that Teacher's Week Schedules and Publications; sign-in email unique case-insensitively; soft-deleted (Deleted User) and restorable; a security stamp signs the User out on their next request after delete, Role or password change ([ADR 0006](docs/decisions/0006-user-aggregate-and-roles.md))
- **Teacher** — no longer owns cars (decision #21); scheduling is per teacher, not per car; soft-deleted, refused while an active User or active Students depend on it
- **Car** — shared pool aggregate; assigned to any number of teachers (many-to-many via TeacherAssignment); soft-deleted; transmission per car
- **WeekSchedule** — per teacher per week; grid of Slots (`Open`/`Unavailable`), Sunday–Friday, short Friday
- **Publication** — per week, school-wide (aggregates that week's Week Schedules, one link for every teacher); state machine `Draft → Published → Open → Closed`, reopenable; carries the unguessable link token and submission window; every close increments the Excel version
- **Student** — identified by national ID; added by the Roster or by an Administrator; belongs to one Teacher and learns on one of that Teacher's Cars (decision #23); Change Teacher / Change Car; deactivated (Inactive Student), never deleted
- **Submission** — per student per publication; target session count + ranked SlotRequests, editable until close

Timezone rules (requirements §8.3): instants in UTC, slot windows as local time + Asia/Jerusalem.

## Implementation Checklist

### Before coding
- [ ] Read `docs/requirements.md` for the feature's exact terminology and rules
- [ ] Find 2–3 similar existing files — match namespace, naming, and structure
- [ ] Verify the state machine transitions involved

### During implementation
- [ ] `MustBe*` guards with domain-specific exceptions on every state-changing method
- [ ] Domain event per state change, named `{Entity}{Action}` in past tense
- [ ] Value objects at the application boundary — no primitives into the domain
- [ ] Tests per `domain-testing.md`: happy path, `__Add_Event`, `__Must_Be_*` guards with `[DataRow]`
