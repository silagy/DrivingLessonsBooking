# #85: Add Users from a New Users Screen - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#85](https://github.com/silagy/DrivingLessonsBooking/issues/85), slice (3) of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #85.

**Goal:** Administrators get a Users screen that lists everyone who can sign in and an Add User dialog (name, sign-in email, Role, linked Teacher, Temporary Password). Deleting a Teacher who still has an active linked User is refused.

**Architecture:** A `CreateUserInteractor` checks the two cross-User rules (sign-in email unique, a Teacher has at most one User) through a new `IUserQueries` port, hashes the Temporary Password through the existing `IPasswordHasher` and calls the existing `User.Create`. `FindUsers` / `GetUser` query interactors project Users left-joined to their Teacher. A new `Administrator` authorization policy (Role claim `administrator`) guards the new `api/users` controllers. `DeleteTeacherInteractor` asks `IUserQueries` whether an active User is linked before deleting. The client gets a lazy `users` feature (signals-only store, PrimeNG table, Add User dialog) and a Users entry in the admin shell.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql, MSTest 4 + Shouldly + FakeItEasy; Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco, Vitest via `ng test`. No new package, no migration.

**Spec:** issue [#85](https://github.com/silagy/DrivingLessonsBooking/issues/85) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) ("Managing Users" stories 11-16 and 18, story 34, "User aggregate", "Cross-aggregate guards", "API contract", "Client", "Testing Decisions") · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Teacher Role, Temporary Password, Deleted User) · design brief [claude-design-prompt.md](../claude-design-prompt.md) sections 3, 4 and 11 · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** the Claude Design project in [users-and-roles-design.md](../users-and-roles-design.md). Task 6 spells out the layout, controls and copy, taken from the brief, so it can be built without the `claude_design` MCP. If that MCP is connected in the executing session, compare the Users screen and Add User dialog against `Users and Roles.html` during task 7's browser check and list any visible difference in the PR.

**Branch:** `85-add-users-screen`, from `84-user-aggregate-sign-in` at `c38655c`. #85 is blocked by #84 ([PR #98](https://github.com/silagy/DrivingLessonsBooking/pull/98), open), so this branch is stacked on it. Commit this plan on the branch before task 1. Do **not** merge `main` into the branch: it would pull unrelated client commits into a PR whose base is `84-user-aggregate-sign-in`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| User aggregate | `User.Create(UserName, Email, PasswordHash, Role, Teacher?)` throws `UserWithTeacherRoleMustHaveLinkedTeacherException` for the Teacher Role without a Teacher; `User.Delete()`; properties `Name`, `SignInEmail`, `PasswordHash`, `Role`, `TeacherId`, `SecurityStamp`, `IsDeleted` | `src\DrivingLessons.Domain\Entities\User.cs` |
| Repository | `IUserRepository`: `GetByEmailAsync(Email)`, `AnyExistAsync()`, `Add(User)` | `Domain\Repositories\IUserRepository.cs`, `Infrastructure\EntityFramework\Repositories\UserRepository.cs` |
| Persistence | table `users`, unique index on `email`, **no** soft-delete query filter (Deleted Users are visible to every query) | `Infrastructure\EntityFramework\EntityConfigurations\UserConfiguration.cs` |
| Hashing | `IPasswordHasher.Hash(string) : PasswordHash` | `Application\Auth\IPasswordHasher.cs` |
| Token | claims `sub`, `email`, `role` (`administrator` / `teacher`), `security_stamp`, `teacher_id` when linked; names in `AuthClaims` | `Infrastructure\Auth\JwtTokenGenerator.cs`, `Infrastructure\Auth\AuthClaims.cs` |
| Authorization | only a fallback policy "authenticated user"; no Role check anywhere; JWT bearer maps inbound claims (default) | `Presentation.Web\Program.cs` |
| Delete Teacher | loads the Teacher and calls `teacher.Delete()`, no cross-aggregate check | `Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs` |
| Teachers | soft-deleted with a query filter (`HasQueryFilter(x => !x.IsDeleted)`) | `Infrastructure\EntityFramework\EntityConfigurations\TeacherConfiguration.cs` |
| Client | no Users feature; admin shell nav has Dashboard, Cars & teachers, Roster, Weekly prep, Publications, History | `client\src\app\features\admin-shell\` |
| Error codes | the filter turns every exception type name into a camelCase `code`; the client shows `errors.{code}` when the key exists | `Presentation.Web\Filters\ApiExceptionFilter.cs`, `client\src\app\core\services\toast.service.ts` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **"A Teacher has at most one User" counts Deleted Users.** Restoring a Deleted User (#86) must never produce two Users for one Teacher, so `ExistsLinkedToTeacherAsync` ignores `IsDeleted`. Sign-in email uniqueness also counts Deleted Users: the unique index on `users.email` already does, and the interactor check must agree with it or a reused email becomes a 500. |
| 2 | **The Delete Teacher guard counts only active Users** (`ActiveExistsLinkedToTeacherAsync`), as #85 and #82 story 34 say. A Teacher whose only User is deleted can be deleted. |
| 3 | **The cross-User checks are interactor checks through `IUserQueries`, as #85 asks, with no new database constraint.** There is no unique index on `users.teacher_id`. Two Administrators creating Users at the same instant can still race; ddd-architecture "Deliberately Omitted" accepts this for admin edits. |
| 4 | **The three new rule exceptions live in `Domain\Exceptions` and are thrown by interactors**, like `WeekScheduleAlreadyExistsException`: `UserSignInEmailAlreadyInUseException`, `TeacherAlreadyLinkedToUserException`, `TeacherMustNotHaveActiveUserException`. The client translates them through the existing `code` extension (`errors.{code}`). That is the "problem type the client translates" in #85: no new `ProblemTypes` constant, because only the student form branches on `type`. |
| 5 | **A blank Temporary Password is refused** by a new `TemporaryPassword` value object (`TemporaryPasswordMustNotBeEmptyException`). It is validated before hashing, keeps surrounding spaces (a password is not trimmed), and its message never contains the value. There is no length or complexity rule: neither #82 nor #85 sets one. |
| 6 | **A new named `Administrator` policy guards only the new `api/users` controllers.** Making it the default for every existing endpoint is #89. JWT bearer stops mapping inbound claims (`MapInboundClaims = false`) and reads Roles from the `role` claim (`RoleClaimType = AuthClaims.Role`), so the policy is `RequireRole("administrator")` against the token #84 issues. |
| 7 | **The sign-in email is called `signInEmail` in every DTO and client model**, matching `User.SignInEmail` and keeping it apart from `Teacher.ContactEmail`. |
| 8 | **The list shows the linked Teacher's name even when that Teacher is deleted** (the query joins `Teachers.IgnoreQueryFilters()`). This can only happen for a Deleted User. |
| 9 | **The list is ordered active first, then by name.** The design brief allows "Deleted Users sorted last" instead of a toggle. Nothing in this slice can delete a User yet (#86), but the list already shows the deleted state. |
| 10 | **The client marks Teachers that already have a User as disabled in the picker** ("Already has a User", from the brief). It works this out from the Users list it already holds. This is display only: the server still refuses with `teacherAlreadyLinkedToUser` (CLAUDE.md rule 12). |
| 11 | **"Teacher required for the Teacher Role" is duplicated in the dialog as a form-level validator**, for immediate feedback. The server rule (`UserWithTeacherRoleMustHaveLinkedTeacherException`) stays the authority. |
| 12 | **The Add User dialog defaults to the Teacher Role.** Giving each Teacher a sign-in is the common case (brief, user goal 1). |
| 13 | **No "You" row marker and no row actions in this slice.** They exist for the self-protection rules and actions of #86 / #87. |
| 14 | **The `users` feature has its own `TeacherOptionsApiService`**, as `publications` and `week-schedules` do, because features never import each other. |
| 15 | **Smoke tests run against a throwaway database** (`drivinglessons_us85_smoke`). A Teacher linked to a User can't be deleted, and Users can't be deleted until #86, so test Users would otherwise stay in the dev database for good. |

## Global Constraints

- `Domain` depends on nothing. `Application` depends on `Domain`. Raw `Guid` and `string` stop at the interactor boundary: typed IDs and value objects inside. Query interfaces may take value objects (precedent: `IStudentQueries.GetActiveByNationalIdAsync(NationalId, ...)`).
- Value objects: nominal records, private constructor, `Of()` factory, one `{Value}Must{Rule}Exception : DomainException` per rule. PII and secret values (emails, names, passwords, hashes) never go into exception messages.
- No comments in code. Test section markers `//given //when //then //expected` are the only exception.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` guard this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, all parameters used, `is null` outside expression trees (EF projections use `== null`, precedent `IdentifyStudentResponse`).
- Inside `DrivingLessons.Infrastructure`, `Email` resolves to the `DrivingLessons.Infrastructure.Email` namespace. Write `DrivingLessons.Domain.Values.Email` in full there (precedent: `UserRepository`).
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]` on the action, `EndpointSummary` and `ProducesResponseType` on every action, CQRS split.
- Every new `{Entity}{Rule}Exception` / `{Entity}NotFoundException` gets an `ApiExceptionFilterTest` case and an `errors.{code}` key in both `client\public\i18n\en.json` and `he.json` (api-guidelines "Rule Codes").
- Client: standalone, `inject()`, OnPush, signals-only stores that expose readonly signals, components never subscribe to stores, `resource()` values read through `hasValue()` (a resource in error throws on `value()`), no hardcoded user-visible strings, Hebrew first with plural imperatives as in the existing `he.json` ("רעננו", "הזינו"), logical CSS properties only, emails and passwords rendered LTR, DTO names identical to the backend's.
- Terminology: User, Role, Administrator, Teacher Role, Temporary Password, Deleted User. Never "account", "admin user", or "login" as a noun for a person.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Client, from `client\` in PowerShell (the default `npm` can't install on this machine; running tests and builds through the local Angular CLI works):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

If `client\node_modules` is missing, install with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci` from `client\`.

Before anything touches the database, use the compose Postgres, not `dl-postgres`: `docker stop dl-postgres` then `docker compose up -d postgres`.

## Review Focus

1. **A sign-in email that differs from an existing User's only by case or surrounding spaces** (`" Owner@School.Example "` when `owner@school.example` exists) must be refused with 409 `userSignInEmailAlreadyInUse`, not reach the unique index and fail with 500. Covered in task 1 (`Sign_In_Email_In_Use_Is_Rejected_Ignoring_Case_And_Spaces`).
2. **A sign-in email or Teacher that belongs only to a Deleted User** must still count as taken (decision 1). The rule lives in the SQL of `UserQueries`, which no unit test reaches. Covered by task 3's smoke steps, which delete a User directly in the database and retry.
3. **A blank or whitespace-only Temporary Password** (`""`, `"   "`) must be refused with 409 `temporaryPasswordMustNotBeEmpty` before anything is hashed or stored. Covered in task 1 (`TemporaryPasswordTest`, `Blank_Temporary_Password_Is_Rejected_Before_Hashing`).
4. **A Teacher-role token calling the Users endpoints** must get 403 while Administrator-only is not yet the default (#89). Covered by task 3's smoke steps (Teacher-role sign-in, `GET /api/users/find` → 403).
5. **Deleting a Teacher whose only User is deleted** must succeed, while one with an active User is refused with 409 `teacherMustNotHaveActiveUser`, shown translated on the Cars & teachers screen. Covered in task 4 (`Deletes_A_Teacher_Whose_Linked_User_Is_Deleted`) and task 7's browser check.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Values\TemporaryPassword.cs`, `Exceptions\TemporaryPasswordMustNotBeEmptyException.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserSignInEmailAlreadyInUseException.cs`, `TeacherAlreadyLinkedToUserException.cs` | **New** | 1 |
| `src\DrivingLessons.Application\Queries\IUserQueries.cs` | **New** (1), gains `FindAsync` / `GetAsync` (2), `ActiveExistsLinkedToTeacherAsync` (4) | 1, 2, 4 |
| `src\DrivingLessons.Application\Commands\CreateUser\CreateUserRequest.cs`, `CreateUserResponse.cs`, `CreateUserInteractor.cs` | **New** | 1 |
| `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs` | **New** (1), grows in 2 and 4 | 1, 2, 4 |
| `src\DrivingLessons.Application\DependencyInjection.cs`, `src\DrivingLessons.Infrastructure\DependencyInjection.cs` | Register interactors and `IUserQueries` | 1, 2 |
| `tests\DrivingLessons.Domain.Test\Values\TemporaryPasswordTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Application.Test\Commands\CreateUserInteractorTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | One case per new exception | 1, 2, 4 |
| `src\DrivingLessons.Application\Queries\FindUsers\FindUsersInteractor.cs`, `ItemForFindUsersResponse.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Queries\GetUser\GetUserInteractor.cs`, `GetUserResponse.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Common\Exceptions\UserNotFoundException.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Queries\GetUserInteractorTest.cs` | **New** | 2 |
| `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs` | **New** | 3 |
| `src\DrivingLessons.Presentation.Web\Program.cs` | Claim mapping, Role claim type, `Administrator` policy | 3 |
| `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`, `UserQueryController.cs` | **New** | 3 |
| `src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveUserException.cs` | **New** | 4 |
| `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs` | Refuses while an active User is linked | 4 |
| `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs` | **New** | 4 |
| `client\public\i18n\en.json`, `he.json` | `errors.teacherMustNotHaveActiveUser` (4); `shell.nav.users`, `users.*`, the other new `errors.*` (6) | 4, 6 |
| `client\src\app\features\users\domain\role.enum.ts`, `user.model.ts`, `teacher-option.model.ts`, `linkable-teacher.model.ts`, `teacher-link.ts`, `teacher-link.spec.ts` | **New** | 5 |
| `client\src\app\features\users\data\users-api.service.ts`, `teacher-options-api.service.ts`, `item-for-find-users.response.ts`, `item-for-find-teachers.response.ts`, `create-user.request.ts`, `create-user.response.ts` | **New** | 5 |
| `client\src\app\features\users\state\users.store.ts`, `users.store.spec.ts` | **New** | 5 |
| `client\src\app\features\users\users.routes.ts` | **New** | 6 |
| `client\src\app\features\users\ui\pages\users\users.page.ts`, `.html`, `.scss` | **New** | 6 |
| `client\src\app\features\users\ui\dialogs\add-user\add-user.dialog.ts`, `.html`, `ui\dialogs\dialog-form.scss` | **New** | 6 |
| `client\src\app\shared\config\app-routes.ts`, `features\admin-shell\admin.routes.ts`, `admin-shell.component.html` | Users route and nav entry | 6 |
| `.claude\rules\api-guidelines.md` (Auth section), `docs\modules\auth\users-and-roles-design.md` | Administrator policy note; slice (3) links this plan | 7 (rules), plan commit (design doc) |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-create-user.md](task-01-create-user.md) | `TemporaryPassword`, the two uniqueness exceptions, `IUserQueries` existence checks, `CreateUserInteractor`, TDD | ✅ own commit |
| 2 | [task-02-user-queries.md](task-02-user-queries.md) | `FindUsers` / `GetUser` interactors, responses, `UserNotFoundException`, `UserQueries` projections, TDD | ✅ own commit |
| 3 | [task-03-users-endpoints.md](task-03-users-endpoints.md) | `Administrator` policy, `api/users` command and query controllers, API smoke test on a throwaway database | ✅ own commit |
| 4 | [task-04-teacher-delete-guard.md](task-04-teacher-delete-guard.md) | `TeacherMustNotHaveActiveUserException`, `DeleteTeacherInteractor` guard, translations, TDD | ✅ own commit |
| 5 | [task-05-client-users-state.md](task-05-client-users-state.md) | `users` feature domain, data and signal store, specs first | ✅ own commit |
| 6 | [task-06-client-users-screen.md](task-06-client-users-screen.md) | Users page, Add User dialog, route, shell nav, translations | ✅ own commit |
| 7 | [task-07-verify-and-pr.md](task-07-verify-and-pr.md) | Rules note, full suites, browser verification (Hebrew RTL + English), PR | ✅ PR |

The PR targets `84-user-aggregate-sign-in` while #98 is open, closes #85 (`Closes #85`) and references the parent spec #82.
