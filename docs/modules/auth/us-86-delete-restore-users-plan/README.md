# #86: Delete and Restore Users with Immediate Lockout - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#86](https://github.com/silagy/DrivingLessonsBooking/issues/86), slice (3) of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #86.

**Goal:** Administrators can delete a User, who is then signed out on their very next request, and can restore a Deleted User. You can't delete yourself or the last active Administrator.

**Architecture:**
- `User.Restore()` joins the existing `User.Delete()`. Both are non-idempotent and both change the security stamp.
- `DeleteUserInteractor` checks the two lockout rules through a new `ICurrentUser` port and `IUserQueries.CountActiveAdministratorsAsync()`.
- `RestoreUserInteractor` refuses to restore a User whose linked Teacher was deleted.
- A `JwtBearerEvents.OnTokenValidated` hook calls `CheckSignedInUserInteractor` on every authenticated request. A missing User, a Deleted User or a stale security stamp fails authentication, giving a 401. The client's existing 401 interceptor then signs the User out.
- The Users screen gets row actions: a kebab with Delete, and a Restore button on deleted rows. It also gets a "You" marker and a Delete dialog that shows refusals inside it.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql, MSTest 4 + Shouldly + FakeItEasy; Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco, Vitest via `ng test`. No new package, no migration.

**Spec:** issue [#86](https://github.com/silagy/DrivingLessonsBooking/issues/86) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Deleted User) · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** Claude Design project `Users and Roles.html` (see [users-and-roles-design.md](../users-and-roles-design.md)). Frames for this slice:
- 3a/3b: populated list; row actions menu
- 3f: Restore success toast
- 3g: Restore refused, already active
- 8a: Delete confirmation
- 8b: Delete refused, deleting yourself
- 8c: Delete refused, last active Administrator
- 8d: Delete refused, already deleted

Task 6 spells out the layout and copy, so it can be built without the `claude_design` MCP.

**Branch:** `86-delete-restore-users`, from `main` at `1d1dd10` (#84 and #85 merged). Commit this plan on the branch before task 1. The PR targets `main`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| User aggregate | `User.Delete()` exists (`MustNotBeDeleted` → `UserAlreadyDeletedException`, new `SecurityStamp`, `UserDeleted`). No Restore. | `src\DrivingLessons.Domain\Entities\User.cs` |
| Test builder | `UserFakeBuilder.Build()` / `BuildDeleted()` / `WithRole` / `WithTeacher` | `tests\DrivingLessons.Domain.Test\Entities\Fake\UserFakeBuilder.cs` |
| Repository | `GetByEmailAsync`, `AnyExistAsync`, `Add`. No `GetAsync(UserId)`. | `Domain\Repositories\IUserRepository.cs`, `Infrastructure\EntityFramework\Repositories\UserRepository.cs` |
| Queries | `FindAsync` (active first), `GetAsync`, `ExistsWithSignInEmailAsync`, `ExistsLinkedToTeacherAsync`, `ActiveExistsLinkedToTeacherAsync` | `Application\Queries\IUserQueries.cs`, `Infrastructure\EntityFramework\Queries\UserQueries.cs` |
| Persistence | `users` has **no** soft-delete query filter (Deleted Users stay loadable). Teachers **do** have one. | `UserConfiguration.cs`, `TeacherConfiguration.cs` |
| Token | `sub` (User id), `email`, `role`, `security_stamp`, `teacher_id`. `MapInboundClaims = false`. | `Infrastructure\Auth\JwtTokenGenerator.cs`, `AuthClaims.cs`, `Presentation.Web\Program.cs` |
| Per-request check | **None.** A Deleted User's token works until it expires (12 h). | `Program.cs` |
| Current user | **No abstraction.** Nothing reads the caller's id. | - |
| Users API | `POST api/users`, `GET api/users/find`, `GET api/users/{id}`, Administrator policy | `Presentation.Web\Controllers\User\` |
| Client 401 | `authInterceptor` calls `auth.logout()` on any 401 except login | `client\src\app\core\auth.interceptor.ts` |
| Client auth | `AuthService.token`, `isAuthenticated`, `email` (decoded from JWT). No user id. | `client\src\app\core\auth.service.ts` |
| Toasts | `success(key)`, `apiError(error)` (translates `errors.{code}`) | `client\src\app\core\services\toast.service.ts` |
| Users screen | List with Status tags, deleted rows muted. No actions column, no "You" marker. | `client\src\app\features\users\` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Restore is a one-click row action, with no confirmation** (design 3f, "Restore is a one-click row action"). **Delete opens a dedicated `DeleteUserDialog`**, not the shared `ConfirmDialog`: design 8a-8d needs a who-card, three bullets and in-dialog refusals. |
| 2 | **Delete refusals (409) appear inside the Delete dialog** as an error `p-message` titled "That didn't go through", and the dialog stays open (design 8b-8d). Restore refusals are an error toast (design 3g). |
| 3 | **Self and last-active-Administrator guards are interactor checks.** They go through a new `ICurrentUser` port and `IUserQueries.CountActiveAdministratorsAsync()`. The exceptions live in `Domain\Exceptions`, like #85 decision 4. Through the UI the last-Administrator guard can't trigger today: the caller is always an active Administrator, and deleting yourself is refused first. It stays because the issue asks for it, it covers concurrent deletes, and #87 (change Role) reuses it. Two Administrators deleting each other at the same instant can still race; this is accepted, like #85 decision 3. |
| 4 | **Restoring a User whose linked Teacher is deleted is refused** with `UserLinkedTeacherMustNotBeDeletedException`. #85 decision 2 lets a Teacher be deleted once their User is deleted. Without this rule, a restore would produce an active Teacher-role User pointing at a deleted Teacher. **Not in #86's text: flag it in the PR.** |
| 5 | **The per-request check runs in `JwtBearerEvents.OnTokenValidated`.** `SignedInUserJwtBearerEvents` (Presentation.Web, wired through `options.EventsType`) passes the `sub` and `security_stamp` claims to `CheckSignedInUserInteractor` (Application). The interactor returns `false` for a malformed id, a blank stamp, a missing User, a Deleted User or a stamp mismatch. `context.Fail(...)` then turns it into a bare 401 challenge that never reaches `ApiExceptionFilter`. Cost: one primary-key lookup per authenticated request. A failed token on an `AllowAnonymous` endpoint (login, student link) just leaves the request anonymous. |
| 6 | **`ICurrentUser` lives in `Application\Auth` and is implemented in Presentation.Web** (`Auth\HttpCurrentUser`, which reads `sub` through `IHttpContextAccessor`). Only the web layer knows HTTP. |
| 7 | **Restore also changes the security stamp** (AC 1). Tokens issued before the delete stay dead after a restore, so the restored User signs in again. |
| 8 | **The "You" marker uses a new `AuthService.userId`** read from the token's `sub`. Delete is **not** hidden on your own row: the server refuses, and the dialog shows `errors.userMustNotDeleteSelf` (CLAUDE.md rule 12, design 8b). |
| 9 | **Row actions are a kebab button plus one shared popup `p-menu`**, holding only Delete for now. #87 adds Edit details, Change Role and Set Temporary Password. Deleted rows show a text `Restore` button instead (design inventory: "Deleted rows replace the menu with one text Button"). |
| 10 | **The danger color comes from the existing theme** (`severity="danger"`), not the mock's plum, per the handoff rule "take colors from the existing app theme". |
| 11 | **Routes:** `DELETE api/users/{id}` → 204 and `POST api/users/{id}/restore` → 204, matching `POST api/publications/{id}/reopen`. |
| 12 | **The Delete refusal is held by the store as an already-translated string** (`deleteRefusal`), produced by a new public `ToastService.messageOf(error)`. That method reuses the toast's `errors.{code}` resolution. The page clears it before opening the dialog, so a reopened dialog never shows an old refusal. |
| 13 | **Smoke tests run against a throwaway database** (`drivinglessons_us86_smoke`), like #85 decision 15. |

## Global Constraints

- `Domain` depends on nothing. `Application` depends on `Domain`. Raw `Guid` and `string` stop at the interactor boundary; inside are typed IDs and value objects.
- Exceptions: one class per rule, `{Entity}{Rule}Exception : DomainException`. Messages never contain names, emails, passwords or stamps (ids are fine, precedent `UserAlreadyDeletedException`).
- No comments in code. Test section markers `//given //when //then //expected` are the only exception.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts`). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, all parameters used, `is null` outside expression trees.
- Inside `DrivingLessons.Infrastructure`, write `DrivingLessons.Domain.Values.Email` in full (namespace clash).
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]` on the action, `EndpointSummary` and `ProducesResponseType` on every action, CQRS split.
- Every new `{Entity}{Rule}Exception` gets an `ApiExceptionFilterTest` case and an `errors.{code}` key in both `client\public\i18n\en.json` and `he.json`.
- Client: standalone, `inject()`, OnPush, signals-only stores exposing readonly signals, components never subscribe to stores, `resource()` read through `hasValue()`, no hardcoded user-visible strings, Hebrew first with plural imperatives ("רעננו"), logical CSS properties only, emails rendered LTR in `<bdi dir="ltr">`, DTO names identical to the backend's.
- Terminology: User, Role, Administrator, Deleted User, Restore. Never "account", "disable", "suspend" or "remove" for a User.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Client, from `client\` in PowerShell (the default `npm` can't install on this machine; the local Angular CLI works):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

If `client\node_modules` is missing, install with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci` from `client\`.

Before anything touches the database, use the compose Postgres, not `dl-postgres`: `docker stop dl-postgres` then `docker compose up -d postgres`.

## Review Focus

1. **A Deleted User's still-valid token** must get 401 on the very next request, not at expiry. Covered in task 4 (`Deleted_User_Is_Not_Signed_In`) and task 4's smoke steps.
2. **A token whose `sub` isn't a Guid, or whose stamp claim is missing or blank,** must be a 401, not a 500 from `UserId.Of` / `SecurityStamp.Of`. Covered in task 4 (`Malformed_User_Id_Is_Not_Signed_In`, `Blank_Security_Stamp_Is_Not_Signed_In` with `[DataRow(null)] [DataRow("")] [DataRow("  ")]`).
3. **Restoring a User whose linked Teacher was deleted after the User was** must be a 409 `userLinkedTeacherMustNotBeDeleted`, never an active User with a dangling link. Covered in task 2 (`User_Whose_Linked_Teacher_Is_Deleted_Is_Not_Restored`).
4. **Reopening the Delete dialog after a refusal** must show a clean dialog, not the previous refusal. Covered in task 5 (`clears the refusal`) and task 6's page flow (`clearDeleteRefusal()` before `open`).
5. **A stale or revoked token sent to an anonymous endpoint** (sign-in, public student link) must still work and must not be a 401. Covered by task 4's smoke step 6.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\User.cs` | `Restore()`, `MustBeDeleted` | 1 |
| `src\DrivingLessons.Domain\Events\UserRestored.cs`, `Exceptions\UserAlreadyActiveException.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs` | Restore tests | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserMustNotDeleteSelfException.cs`, `UserMustNotBeLastActiveAdministratorException.cs`, `UserLinkedTeacherMustNotBeDeletedException.cs` | **New** | 2 |
| `src\DrivingLessons.Domain\Repositories\IUserRepository.cs`, `Infrastructure\...\Repositories\UserRepository.cs` | `GetAsync(UserId)` | 2 |
| `src\DrivingLessons.Application\Queries\IUserQueries.cs`, `Infrastructure\...\Queries\UserQueries.cs` | `CountActiveAdministratorsAsync()` | 2 |
| `src\DrivingLessons.Application\Auth\ICurrentUser.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Commands\DeleteUser\DeleteUserInteractor.cs`, `Commands\RestoreUser\RestoreUserInteractor.cs` | **New** | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register interactors | 2, 4 |
| `tests\DrivingLessons.Application.Test\Commands\DeleteUserInteractorTest.cs`, `RestoreUserInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | Five cases | 2 |
| `client\public\i18n\en.json`, `he.json` | `errors.*` (2); `users.*` (6) | 2, 6 |
| `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs` | **New** | 3 |
| `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs` | Delete, Restore | 3 |
| `src\DrivingLessons.Presentation.Web\Program.cs` | `ICurrentUser` (3); JWT events (4) | 3, 4 |
| `tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs` | **New** | 3 |
| `src\DrivingLessons.Application\Auth\CheckSignedInUserInteractor.cs` | **New** | 4 |
| `src\DrivingLessons.Presentation.Web\Auth\SignedInUserJwtBearerEvents.cs` | **New** | 4 |
| `tests\DrivingLessons.Application.Test\Auth\CheckSignedInUserInteractorTest.cs` | **New** | 4 |
| `client\src\app\core\auth.service.ts`, `auth.service.spec.ts` | `userId`; spec **new** | 5 |
| `client\src\app\core\services\toast.service.ts`, `.spec.ts` | `success` detail, `messageOf` | 5 |
| `client\src\app\features\users\data\users-api.service.ts` | `deleteUser`, `restoreUser` | 5 |
| `client\src\app\features\users\state\users.store.ts`, `.spec.ts` | `currentUserId`, `deleteRefusal`, `delete`, `restore` | 5 |
| `client\src\app\features\users\ui\pages\users\users.page.{ts,html,scss}` | Actions column, "You", menu, Restore | 6 |
| `client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.{ts,html,scss}` | **New** | 6 |
| `docs\modules\auth\users-and-roles-design.md` | Slice (3) links this plan | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-user-restore.md](task-01-user-restore.md) | `User.Restore`, `UserRestored`, `UserAlreadyActiveException`, TDD | ✅ own commit |
| 2 | [task-02-delete-and-restore-interactors.md](task-02-delete-and-restore-interactors.md) | `ICurrentUser`, repo / query additions, three rule exceptions, Delete / Restore interactors, filter cases, `errors.*` translations, TDD | ✅ own commit |
| 3 | [task-03-delete-restore-endpoints.md](task-03-delete-restore-endpoints.md) | `HttpCurrentUser`, `DELETE` / `POST restore` endpoints, API smoke | ✅ own commit |
| 4 | [task-04-per-request-lockout.md](task-04-per-request-lockout.md) | `CheckSignedInUserInteractor`, `SignedInUserJwtBearerEvents`, lockout smoke | ✅ own commit |
| 5 | [task-05-client-users-state.md](task-05-client-users-state.md) | `AuthService.userId`, `ToastService` additions, API + store, specs first | ✅ own commit |
| 6 | [task-06-client-users-actions.md](task-06-client-users-actions.md) | Actions column, "You" tag, row menu, Restore button, Delete dialog, translations | ✅ own commit |
| 7 | [task-07-verify-and-pr.md](task-07-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL + English + lockout), PR | ✅ PR |

The PR targets `main`, closes #86 (`Closes #86`) and references the parent spec #82.
