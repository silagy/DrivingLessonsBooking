# #89: Teacher-role Users Reach Only Week Schedules and Publications - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#89](https://github.com/silagy/DrivingLessonsBooking/issues/89), the first half of slice (2) "Roles, policies and Teacher scoping" of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #89.

**Goal:** A Teacher-role User signs in, lands on Weekly prep, and sees only Weekly prep, Publications and History. Every other screen is hidden from navigation, refused by the client's route guards, and refused (403) by the server. Week Schedule creation and the Publication lifecycle (publish / extend / reopen) stay Administrator-only, and their controls are hidden for Teacher-role Users. An Administrator still sees and can do everything.

**Architecture:**
- Backend: two authorization policies in `AuthorizationPolicies` (`Administrator`, `TeacherOrAdministrator`). `Administrator` becomes both the **fallback** policy (endpoints with no `[Authorize]`) and the **default** policy (a bare `[Authorize]`), so every existing and future endpoint is Administrator-only unless it opts in. Exactly eight actions opt in to `TeacherOrAdministrator`. A reflection test pins the whole endpoint-to-policy matrix.
- Client: `AuthService` parses `role` and `teacher_id` from the token. Two functional guards in `core\role.guards.ts` (`administratorGuard` refuses with an info toast and redirects to Weekly prep, `homeGuard` sends a Teacher from the dashboard to Weekly prep silently). The admin shell renders navigation from a pure `navigationFor(role)` mapping and shows the Role tag in the user menu. The Week Schedules and Publications stores stop calling Administrator-only endpoints for a Teacher-role User and expose `canChooseTeacher` / `canPublish` / `isNotCreatedYet` / `canManageLifecycle` signals that the pages use to hide controls.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API (authorization policies), EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration.

**Spec:** issue [#89](https://github.com/silagy/DrivingLessonsBooking/issues/89) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · sibling [#90](https://github.com/silagy/DrivingLessonsBooking/issues/90) (data scoping, **not** in this plan) · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Teacher Role) · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** Claude Design project `Users and Roles.html` (see [users-and-roles-design.md](../users-and-roles-design.md)). Frames for this slice:
- 2a: Administrator bar - Dashboard, Cars & Teachers, Students (our "Roster"), Weekly prep, Publications, History, Users (last)
- 2b: Teacher bar - Weekly prep, Publications, History only. Hidden items are **not rendered** (not disabled)
- 2c / 2d / 2e: user menu header with avatar, email (LTR) and a **Role tag** (Administrator: sky tint; Teacher: ocean tint)
- 10a: Weekly prep for a Teacher - no "Create Week Schedule", no "Publish week" (hidden, not disabled)
- 10b: Publications for a Teacher - no lifecycle controls; Excel download stays
- 10d: a refused URL redirects to Weekly prep with an **info** toast "אין לך גישה לדף הזה" / "הועברת למערכת השבועית שלך."

Every frame's layout and copy is spelled out in the task that builds it, so the tasks can be built without the `claude_design` MCP. Task 7 compares against the frames when the MCP is connected.

**Branch:** `89-teacher-role-navigation`, from `main` at `f2a4b8f` (#88 merged). This plan is committed on the branch before task 1. The PR targets `main`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Policies | `AuthorizationPolicies.Administrator` only. `Program.cs` sets `FallbackPolicy = RequireAuthenticatedUser()` and adds the Administrator policy (`RequireRole("administrator")`) | `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs`, `Program.cs:58-64` |
| Controllers | `UserCommandController` / `UserQueryController` carry `[Authorize(Policy = Administrator)]`. `MeCommandController` has a bare `[Authorize]`. Auth login and the two Submission controllers are `[AllowAnonymous]`. Every other controller has no attribute (fallback: any signed-in User) | `src\DrivingLessons.Presentation.Web\Controllers\**` |
| Token | `sub`, `email`, `role` (`administrator` / `teacher`), `teacher_id` (only when linked), `security_stamp`. A Role change rotates the stamp, so a token's `role` is never stale for more than one request | `src\DrivingLessons.Infrastructure\Auth\JwtTokenGenerator.cs`, `AuthClaims.cs` |
| Client auth | `AuthService`: `token`, `isAuthenticated`, `email`, `userId`; no Role | `client\src\app\core\auth.service.ts` |
| Guards | `authGuard` only (signed in or `/login`) | `client\src\app\core\auth.guard.ts`, `app.routes.ts` |
| Admin routes | `''` Dashboard, `teachers`, `roster`, `week-schedules`, `publications` (+ `history`), `users`; no Role check | `client\src\app\features\admin-shell\admin.routes.ts` |
| Shell nav | Seven hard-coded `<a>` links; user menu header shows avatar + email only (#88 decision 10 deferred the Role tag to this slice) | `client\src\app\features\admin-shell\admin-shell.component.html` |
| Role enum | `Role { administrator, teacher }` lives in the users feature, so `core\` can't use it | `client\src\app\features\users\domain\role.enum.ts` |
| Weekly prep | Loads the Teacher picker from `api/teachers/find`; a missing Week Schedule (404) is created on the fly (`POST api/week-schedules`); "Publish week" shows for a Draft Publication | `client\src\app\features\week-schedules\state\week-schedules.store.ts`, `ui\pages\weekly-prep\` |
| Publications | Loads the Teacher picker from `api/teachers/find`; Publish / Extend window / Reopen buttons by state; `?publish=1` auto-opens the publish dialog; the empty state links to "Prepare this week" | `client\src\app\features\publications\state\publications.store.ts`, `ui\pages\publications-dashboard\` |
| Toasts | `ToastService.success` and `apiError`; generic keys for 404 / 409 only | `client\src\app\core\services\toast.service.ts` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Administrator is both the fallback and the default policy.** "The default for every existing endpoint" (AC 1) is implemented once, in `AuthorizationPolicies.Configure`, not by stamping an attribute on every controller. A new controller is Administrator-only unless it opts in, and a bare `[Authorize]` also means Administrator. The existing explicit `[Authorize(Policy = Administrator)]` on the User controllers stays (harmless, explicit). |
| 2 | **Teacher-or-Administrator on exactly eight actions** (AC 2): `WeekScheduleQueryController.GetByTeacherAndWeek`, `WeekScheduleCommandController.MarkSlotUnavailable` / `MarkSlotAvailable`, all four `PublicationQueryController` actions (`GetByWeek`, `GetDashboard` = Submissions, `FindHistory`, `DownloadExcel`), and `MeCommandController.ChangePasswordAsync`. Class-level on the two query controllers and Me, action-level on the two slot commands. `WeekScheduleCommandController.Create` and every `PublicationCommandController` action keep the fallback (AC 3). There are no "open" / "close" endpoints: those transitions run as scheduled jobs. |
| 3 | **A reflection test pins the matrix** (`ControllerAuthorizationTest`): the Teacher-or-Administrator set, the anonymous set, and "everything else is Administrator". An action that combines two policies (class + action) shows as `Administrator+TeacherOrAdministrator` and fails the test, because ASP.NET ANDs them. A policy-evaluation test (`AuthorizationPoliciesTest`) proves which Roles each policy admits. |
| 4 | **`Role` moves to `shared\models\role.enum.ts`** so `core\AuthService` can type the parsed claim without importing a feature. The users feature imports it from there. An unknown `role` claim parses as `null`, which is neither Role: every Role-gated thing is refused. |
| 5 | **Client Role checks are UX, not security.** The server refuses with 403 regardless (CLAUDE.md rule 12). Guards, hidden nav items and hidden buttons only keep a Teacher away from screens and controls they can't use. A 403 that still reaches the client gets the generic toast `errors.forbidden`. |
| 6 | **A refused URL gets the design's info toast and lands on Weekly prep (10d).** `administratorGuard` is a `CanMatchFn` on `teachers`, `roster` and `users`, so a Teacher never even downloads those lazy chunks. Landing on `/` (after sign-in, or by clicking the logo) is not a refusal: `homeGuard` sends a Teacher to Weekly prep **without** a toast. |
| 7 | **The info toast waits for the translations.** A Teacher who types `/users` into a fresh tab hits the guard before Transloco has loaded `he.json`; `ToastService.info` awaits `transloco.load(activeLang)` before translating, so the toast never shows raw keys. |
| 8 | **Navigation by Role is a pure mapping** (`features\admin-shell\domain\navigation.ts`, `navigationFor(role)`), specced on its own (AC 6). Administrator: the seven items in today's order. Teacher: Weekly prep, Publications, History (design 2b). No Role (unknown claim): no items. |
| 9 | **The user menu header gets the Role tag (design 2c / 2d)**, reusing the Users screen's tints (sky for Administrator, ocean for Teacher). Name and linked Teacher name stay out: the token carries neither, and fetching them is not in #89. |
| 10 | **For a Teacher-role User, the two stores never call Administrator-only endpoints.** Without this, Weekly prep and Publications would fail to load for a Teacher (403 on `api/teachers/find`, and on `POST api/week-schedules` for a missing week). So: the Teacher picker isn't loaded or shown, the selected Teacher is the token's `teacher_id`, `selectTeacher` is ignored, and a missing Week Schedule shows "not prepared yet" instead of being created. This is the minimum for the screens to work; it does **not** scope the server's data (any Teacher's data is still reachable by a crafted request). The locked Teacher chip (10a), "(me)" in the picker (10c), the Administrator-linked-to-a-Teacher default and the server-side scoping are #90. |
| 11 | **Teacher-only copy where the Administrator copy tells you to act:** a Draft Publication shows `publications.dashboard.draftPromptTeacher` ("The Administrator publishes it...") instead of "Publish it to open...", and the empty state hides the "Prepare this week" link. A missing Week Schedule shows `weekSchedules.notCreatedYet`. |
| 12 | **Browser checks and smoke tests run against throwaway databases** (`drivinglessons_us89_smoke`, `drivinglessons_us89_verify`), like #88 decision 12. Colors come from the app theme. |

## Global Constraints

- `Domain` depends on nothing, and `Application` depends only on `Domain`. Authorization lives in `Presentation.Web` (`Auth\`), which may read `Infrastructure.Auth.AuthClaims` as `Program.cs` already does.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]`, `EndpointSummary` and `ProducesResponseType` on every action, zero logic.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only. Emails render `dir="ltr"`.
  - Route paths only from `AppRoutes`.
- Terminology: User, Role, Administrator, Teacher Role, Week Schedule, Publication, Slot. Never "account" for a User. Never "admin" in user-visible copy.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/core/auth.service.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **An endpoint nobody listed must still refuse a Teacher.** A future controller with no attribute, or with a bare `[Authorize]`, has to be Administrator-only, not "any signed-in User". Covered in task 1 (`Endpoints_Without_A_Policy_Are_Administrator_Only` evaluates both the fallback and the default policy, and `Every_Other_Endpoint_Is_Administrator_Only` sweeps every action).
2. **A Teacher who types an admin URL into a fresh tab sees translated copy, not keys.** The guard runs before `he.json` loads. Covered in task 3 (`waits for the translations before showing the info toast`) and task 7 step 4 (hard reload on `/users`).
3. **Signing in as a Teacher must not greet them with "no access".** Login navigates to `/`, which is the Administrator dashboard. Covered in task 3 (`sends a Teacher from the dashboard to weekly prep without a toast`) and task 7 step 3.
4. **A Teacher opening a week the Administrator hasn't prepared gets a message, not a load error or a 403.** Covered in task 5 (`shows a missing week as not prepared instead of creating it, for a Teacher`) and task 7 step 5.
5. **A token with an unexpected `role` value grants nothing.** Covered in task 2 (`grants no Role for an unknown role claim`), task 3 (guard refuses when `isAdministrator` is false), task 4 (`shows no navigation without a known Role`) and task 1 (`Policy_Admits_Only_Its_Roles` with `student`).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs` | `TeacherOrAdministrator`, `Configure` | 1 |
| `src\DrivingLessons.Presentation.Web\Program.cs` | `AddAuthorization(AuthorizationPolicies.Configure)` | 1 |
| `src\DrivingLessons.Presentation.Web\Controllers\WeekSchedule\WeekScheduleQueryController.cs`, `WeekScheduleCommandController.cs` | Teacher-or-Administrator | 1 |
| `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs` | Teacher-or-Administrator | 1 |
| `src\DrivingLessons.Presentation.Web\Controllers\Me\MeCommandController.cs` | Teacher-or-Administrator | 1 |
| `tests\DrivingLessons.Application.Test\Auth\AuthorizationPoliciesTest.cs`, `ControllerAuthorizationTest.cs` | **New** | 1 |
| `.claude\rules\api-guidelines.md` | Auth section | 1 |
| `client\src\app\shared\models\role.enum.ts` | Moved from `features\users\domain\` | 2 |
| `client\src\app\features\users\**` (13 imports) | Import from `shared\models\role.enum` | 2 |
| `client\src\app\core\auth.service.ts`, `.spec.ts` | `role`, `teacherId`, `isAdministrator`, `isTeacher` | 2 |
| `client\src\app\core\services\toast.service.ts`, `.spec.ts` | `info`, 403 → `errors.forbidden` | 3 |
| `client\src\app\core\role.guards.ts`, `.spec.ts` | **New** | 3 |
| `client\src\app\features\admin-shell\admin.routes.ts` | Guards on routes | 3 |
| `client\public\i18n\he.json`, `en.json` | `access.*`, `errors.forbidden` (3); `shell.roles.*` (4); `weekSchedules.notCreatedYet` (5); `publications.dashboard.draftPromptTeacher` (6) | 3-6 |
| `.claude\rules\client-architecture.md` | Role guards line | 3 |
| `client\src\app\features\admin-shell\domain\navigation.ts`, `.spec.ts` | **New** | 4 |
| `client\src\app\features\admin-shell\admin-shell.component.ts`, `.html`, `.scss` | Nav from `navigationFor`, Role tag in the menu | 4 |
| `client\src\app\features\week-schedules\state\week-schedules.store.ts`, `.spec.ts` | Role-aware | 5 |
| `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html` | Hide picker, not-prepared state | 5 |
| `client\src\app\features\publications\state\publications.store.ts`, `.spec.ts` | Role-aware | 6 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`, `.ts`, `.spec.ts` | Hide picker and lifecycle controls | 6 |
| `docs\modules\auth\users-and-roles-design.md` | Slice (2) links this plan | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-authorization-policies.md](task-01-authorization-policies.md) | Two policies, Administrator as fallback + default, eight opt-ins, matrix test, API smoke for both Roles | ✅ own commit |
| 2 | [task-02-client-role-from-token.md](task-02-client-role-from-token.md) | `Role` to `shared\`, `AuthService` parses Role and linked Teacher, specs first | ✅ own commit |
| 3 | [task-03-client-role-guards.md](task-03-client-role-guards.md) | `ToastService.info` + 403 key, `administratorGuard` / `homeGuard`, guarded routes, `access.*` copy | ✅ own commit |
| 4 | [task-04-client-navigation-by-role.md](task-04-client-navigation-by-role.md) | `navigationFor(role)`, shell renders it, Role tag in the user menu | ✅ own commit |
| 5 | [task-05-client-weekly-prep-for-teachers.md](task-05-client-weekly-prep-for-teachers.md) | Week Schedules store and page for a Teacher: no picker, no create, no publish | ✅ own commit |
| 6 | [task-06-client-publications-for-teachers.md](task-06-client-publications-for-teachers.md) | Publications store and dashboard for a Teacher: no picker, no lifecycle controls | ✅ own commit |
| 7 | [task-07-verify-and-pr.md](task-07-verify-and-pr.md) | Full suites, browser verification (both Roles, Hebrew RTL, English), design comparison, PR | ✅ PR |

The PR targets `main`, closes #89 (`Closes #89`), references the parent spec #82, and says #90 follows.
