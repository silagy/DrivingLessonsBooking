# US-05–07: Week Schedules Module — Task Index

Per-task breakdown of the approved implementation plan (the source of truth). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** Implement the week-schedules module — admin selects a teacher + week and prepares the slot grid by toggling slots Open ↔ Unavailable — covering GitHub issues [#6](https://github.com/silagy/DrivingLessonsBooking/issues/6) (US-05), [#7](https://github.com/silagy/DrivingLessonsBooking/issues/7) (US-06), [#8](https://github.com/silagy/DrivingLessonsBooking/issues/8) (US-07).

**Architecture:** New `WeekSchedule` aggregate (child `Slot` entities, mirroring the Teacher/Car pattern) through all four onion layers, plus a new Angular `week-schedules` feature with a shared dumb `WeekGridComponent`. Client creates the schedule on 404 ("select week → open grid" is one perceived action); toggles are two explicit non-idempotent commands.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular (signals, standalone, zoneless) + PrimeNG + Transloco client.

**Branch:** `6-us-05-07-week-schedules-module`

## User Stories

- **US-05** ([#6](https://github.com/silagy/DrivingLessonsBooking/issues/6)) — admin opens "Weekly prep" for a teacher + week and sees a fully-open grid: Sunday–Friday, Sun–Thu with Morning/Noon/Afternoon/Evening, Friday with Morning + Noon only, no Saturday column.
- **US-06** ([#7](https://github.com/silagy/DrivingLessonsBooking/issues/7)) — admin clicks an Open slot to mark it Unavailable.
- **US-07** ([#8](https://github.com/silagy/DrivingLessonsBooking/issues/8)) — admin clicks an Unavailable slot to mark it Available (Open) again.

## Context

The weekly demand-collection flow starts with the admin preparing each teacher's week: a fixed Sunday–Friday grid (Sun–Thu: Morning/Noon/Afternoon/Evening; Friday: Morning + Noon only; Saturday does not exist), every slot starting Open, individually toggleable to Unavailable and back. This is the prerequisite for the Publications module. The Teachers module (PR #56) established every pattern; the committed mockups (`Driving Lesson Mockup\mock\admin.jsx` → `AdminPrep` + `DeskGrid`, `mock\shared.jsx` → `MK_DAYS`/`MK_SLOTS`/`mkSlotExists`) are the UX source of truth.

## Decisions (resolved via grilling)

| # | Decision |
|---|----------|
| 1 | **Creation**: `GET .../by-teacher-and-week` returns 404 when no schedule exists; the client then POSTs create (all 22 slots Open) and re-fetches. Queries stay side-effect-free. |
| 2 | **Week identity**: `WeekStart` value object wrapping `DateOnly`, must be a Sunday (throws `WeekStartMustBeSundayException`). API format `2026-07-19`. |
| 3 | **Toggle API**: `Slot` child entities with typed `SlotId`s created at schedule creation. Two commands: `POST {id}/slots/{slotId}/mark-unavailable` and `POST {id}/slots/{slotId}/mark-available` (user's naming — symmetric commands; the slot *state* stays `Open` per requirements §5.3). Wrong-state → domain exception → 409. No generic "toggle". |
| 4 | **Week picker**: current week + next 4 Sundays, computed client-side, defaults to next week. Labels are localized date ranges only (no week numbers). |
| 5 | **Slot times**: slots persist `(Day, SlotWindowType, SlotState)` only; times derive from a static domain definition and appear as computed properties on the query response. Client uses response times for row labels. |
| 6 | **Page**: new "Weekly prep" nav tab (mockup `AdminPrep`), route `week-schedules`, teacher + week selectors on the page. |
| 7 | **Out of scope**: PUBLISH WEEK button and Draft status chip (Publication aggregate) — omitted entirely, added by the publications module. |
| 8 | **Toggle UX**: silent success (the cell flipping is the feedback), errors toast via `toast.apiError` + grid reloads to server state. |
| 9 | **Duplicate guard**: `CreateWeekScheduleInteractor` checks `GetByTeacherAndWeekAsync` → `WeekScheduleAlreadyExistsException` (409); unique DB index `(teacher_id, week_start)` as backstop. Client treats a 409 on create as "someone else created it" and re-fetches. |
| 10 | **Cross-feature rule**: `week-schedules` may not import from `features/teachers` — it gets its own `TeacherOptionsApiService` hitting the existing `GET api/teachers/find`. The grid enums (`DayOfWeek`, `SlotWindow`) live in `shared/models` because the shared `WeekGridComponent` needs them. |
| 11 | **Delivery**: branch `6-us-05-07-week-schedules-module`, commit per task, one PR closing #6 #7 #8. Plan docs materialized at `docs/modules/week-schedules/us-05-07-week-schedules-plan/` (teachers-module format). |

## Conventions that OVERRIDE the rules docs (follow the code, per teachers module)

- `ITeacherRepository` has **no `UnitOfWork` property** — interactors inject `IUnitOfWork` separately. Do the same for `IWeekScheduleRepository`.
- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- No comments anywhere except `//given //when //then` test markers.
- `FirstOrDefaultAsync` for predicate lookups, `FindAsync` for PK; never `SingleOrDefaultAsync`.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-domain-values.md](task-01-domain-values.md) | Domain values — IDs, WeekStart, enums, grid definition (TDD) | ✅ own commit |
| 2 | [task-02-week-schedule-aggregate.md](task-02-week-schedule-aggregate.md) | WeekSchedule aggregate + Slot child entity (TDD) | ✅ own commit |
| 3 | [task-03-application-layer.md](task-03-application-layer.md) | Application layer — commands, query, exceptions, DI | ✅ own commit |
| 4 | [task-04-infrastructure.md](task-04-infrastructure.md) | Infrastructure — persistence, queries, migration | ✅ own commit |
| 5 | [task-05-controllers.md](task-05-controllers.md) | Controllers + API smoke test | ✅ own commit |
| 6 | [task-06-client-domain-data.md](task-06-client-domain-data.md) | Client — shared grid enums, feature domain + data layer | ✅ own commit |
| 7 | [task-07-week-grid-component.md](task-07-week-grid-component.md) | Shared WeekGridComponent + grid i18n keys | ✅ own commit |
| 8 | [task-08-signal-store.md](task-08-signal-store.md) | Week-schedules signal store | ✅ own commit |
| 9 | [task-09-weekly-prep-page.md](task-09-weekly-prep-page.md) | Weekly Prep page, routes, nav, feature i18n | ✅ own commit |
| 10 | [task-10-verification.md](task-10-verification.md) | End-to-end verification (both languages) + PR | — |

## How to Run a Task

1. Confirm you are on branch `6-us-05-07-week-schedules-module` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\
├── Values\        WeekScheduleId, SlotId, WeekStart, SlotState, SlotWindowType, SlotWindowTimes, WeekGridDefinition
├── Entities\      WeekSchedule, Slot
├── Events\        WeekScheduleCreated, SlotMarkedUnavailable, SlotMarkedAvailable
├── Exceptions\    WeekStartMustBeSundayException, SlotMustBeOpenException, SlotMustBeUnavailableException, SlotNotInWeekScheduleException, WeekScheduleAlreadyExistsException
└── Repositories\  IWeekScheduleRepository
src\DrivingLessons.Application\
├── Common\Exceptions\  WeekScheduleNotFoundException, SlotNotFoundException
├── Commands\           CreateWeekSchedule, MarkSlotUnavailable, MarkSlotAvailable
└── Queries\            IWeekScheduleQueries, GetWeekSchedule
src\DrivingLessons.Infrastructure\EntityFramework\
├── EntityConfigurations\  WeekScheduleConfiguration, Converters\ (WeekScheduleIdConverter, SlotIdConverter, WeekStartConverter)
├── Migrations\            new AddWeekSchedules
├── Queries\               WeekScheduleQueries
└── Repositories\          WeekScheduleRepository
src\DrivingLessons.Presentation.Web\Controllers\WeekSchedule\    WeekScheduleCommandController, WeekScheduleQueryController
tests\DrivingLessons.Domain.Test\                                Values\, Entities\ + Fake\, Common\Faker (FakeSunday)
client\src\app\
├── shared\models\       day-of-week.enum.ts, slot-window.enum.ts, week-grid-cell.ts
├── shared\components\   week-grid\
└── features\week-schedules\{domain, data, state, ui}\
```
