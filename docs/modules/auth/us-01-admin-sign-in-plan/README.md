# US-01: Admin Signs In — Task Index

Per-task breakdown of [../us-01-admin-sign-in-plan.md](../us-01-admin-sign-in-plan.md) (the source of truth). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** Implement GitHub issue #2 (US-01) — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication — while scaffolding the full greenfield skeleton every later slice builds on.

**Acceptance criteria (issue #2):** Given I open the admin app not signed in and have valid administrator credentials, when I submit my email and password on the login page, then I am signed in and taken to the admin area, which is unreachable without authentication.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice · full skeleton scaffolding.

**Branch:** `2-us-01-admin-signs-in-with-email-and-password`

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-repo-hygiene.md](task-01-repo-hygiene.md) | .gitignore + .editorconfig | ✅ own commit |
| 2 | [task-02-solution-skeleton.md](task-02-solution-skeleton.md) | Solution + Onion projects + packages | ✅ own commit |
| 3 | [task-03-exceptions-and-filter.md](task-03-exceptions-and-filter.md) | Exception base types + global exception filter | ✅ own commit |
| 4 | [task-04-application-auth.md](task-04-application-auth.md) | Auth ports + login interactor | ✅ own commit |
| 5 | [task-05-infrastructure.md](task-05-infrastructure.md) | Entity, DbContext, options, adapters, seeder | ⏳ commits with task 6 |
| 6 | [task-06-presentation-and-migration.md](task-06-presentation-and-migration.md) | Controller, Program.cs, config, first migration | ✅ commits tasks 5–6 |
| 7 | [task-07-angular-workspace.md](task-07-angular-workspace.md) | Angular workspace + PrimeNG + Transloco + RTL | ⏳ commits with task 9 |
| 8 | [task-08-client-auth.md](task-08-client-auth.md) | Client auth service, interceptor, guard, routes | ⏳ commits with task 9 |
| 9 | [task-09-login-and-shell.md](task-09-login-and-shell.md) | Login page + admin shell | ✅ commits tasks 7–9 |
| 10 | [task-10-docker.md](task-10-docker.md) | Dockerfile, docker-compose.yml, .env.example | ✅ own commit |
| 11 | [task-11-manual-verification.md](task-11-manual-verification.md) | Manual verification of acceptance criteria | — |

## How to Run a Task

1. Confirm you are on branch `2-us-01-admin-signs-in-with-email-and-password` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.

## Target Repository Layout

```
DrivingLessonsBooking\
├── DrivingLessons.sln
├── src\
│   ├── DrivingLessons.Domain\            # DomainException now; aggregates in future slices
│   ├── DrivingLessons.Application\       # LoginInteractor, auth ports, app exceptions
│   ├── DrivingLessons.Infrastructure\    # EF Core, JWT, password hashing, seeding, migrations
│   └── DrivingLessons.Presentation.Web\  # AuthController, exception filter, Program.cs, SPA host
├── client\                               # Angular workspace
│   ├── public\i18n\{en,he}.json
│   └── src\app\{core, features\auth, features\admin-shell}
├── Dockerfile  docker-compose.yml  .env.example  .dockerignore  .gitignore  .editorconfig
```

Onion references: `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.
