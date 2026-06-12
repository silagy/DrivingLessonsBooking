# CLAUDE.md

Weekly demand-collection system for a driving school (see [README.md](README.md) and [docs/requirements.md](docs/requirements.md)).

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
8. **Use requirements terminology** — entity, state, and flow names come from `docs/requirements.md` (Publication, Week Schedule, Slot, Submission, Slot Request). Never invent alternative names
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
    ├── core\                         singletons: auth, http interceptors, layout, language/date/toast services
    ├── features\                     lazy bounded contexts: teachers, week-schedules, publications, student-form
    │   └── {feature}\                domain\ · data\ · state\ · ui\ (see client-architecture.md)
    └── shared\                       feature-agnostic components, pipes, config (AppRoutes)
tests\
└── DrivingLessons.Domain.Test\       domain unit tests (MSTest + Shouldly)
docs\
└── decisions\                        ADRs, one file per decision
```

### File Location Quick Reference

| Code Type | Path Pattern | Class Name |
|-----------|--------------|------------|
| Base types (EntityId, Entity, AggregateRoot, IDomainEvent, DomainException) | `Domain\Common\{BaseType}.cs` | see domain-building-blocks.md |
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
| Response DTO | `Application\Queries\{Op}{Entity}\{Op}{Entity}Response.cs` | `{Op}{Entity}Response` |
| Query interface | `Application\Queries\I{Entity}Queries.cs` | `I{Entity}Queries` |
| Repository impl | `Infrastructure\EntityFramework\Repositories\{Entity}Repository.cs` | `{Entity}Repository` |
| Query impl | `Infrastructure\EntityFramework\Queries\{Entity}Queries.cs` | `{Entity}Queries` |
| EF configuration | `Infrastructure\EntityFramework\EntityConfigurations\{Entity}Configuration.cs` | `{Entity}Configuration` |
| Command controller | `Presentation.Web\Controllers\{Entity}\{Entity}CommandController.cs` | `{Entity}CommandController` |
| Query controller | `Presentation.Web\Controllers\{Entity}\{Entity}QueryController.cs` | `{Entity}QueryController` |
| Test | `Domain.Test\Entities\{Entity}Test.cs` | `{Entity}Test` |
| FakeBuilder | `Domain.Test\Entities\Fake\{Entity}FakeBuilder.cs` | `{Entity}FakeBuilder` |

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
```

## Domain Snapshot

Core aggregates (full model in `docs/requirements.md` §5):

- **Teacher** — owns 1–2 Cars (transmission per car); scheduling is per teacher, not per car
- **WeekSchedule** — per teacher per week; grid of Slots (`Open`/`Unavailable`), Sunday–Friday, short Friday
- **Publication** — per week schedule; state machine `Draft → Published → Open → Closed`, reopenable; carries the unguessable link token and submission window; every close increments the Excel version
- **Student** — identified by email; remembers profile and default teacher
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
