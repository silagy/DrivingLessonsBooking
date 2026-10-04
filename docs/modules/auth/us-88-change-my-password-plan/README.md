# #88: Change My Own Password - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#88](https://github.com/silagy/DrivingLessonsBooking/issues/88), which is slice (4) of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #88.

**Goal:** Every signed-in User, Administrator or Teacher, can change their own password from a "Change my password" dialog in the shell's user menu. They must enter their current password, and after a successful change they stay signed in with a fresh token.

**Architecture:**
- `User.ChangePassword(PasswordHash)` stores an already-hashed password, rotates the security stamp and raises `UserPasswordChanged`. A new `Password` value object validates the plain new password (non-blank, kept verbatim).
- `ChangeMyPasswordInteractor` works on the signed-in User (`ICurrentUser`). It verifies the current password through `IPasswordHasher.Verify` and refuses a wrong one with `UserCurrentPasswordMustBeCorrectException` (409, never 401). It then hashes, changes, commits, and issues a fresh token through `IJwtTokenGenerator`, so the rotated stamp doesn't leave the caller with a dead session.
- A new "Me" controller exposes `PUT api/me/password`. It has no Administrator policy, so the global fallback policy (any authenticated User) applies.
- Client: `AuthService.useToken` swaps in the fresh token. A `MyPasswordStore` runs the command and keeps the refusal for the dialog. The admin shell replaces its email + "Sign out" button with an avatar button that opens a user menu ("Change my password", "Sign out").

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration (the password hash and stamp columns already exist).

**Spec:** issue [#88](https://github.com/silagy/DrivingLessonsBooking/issues/88) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Teacher Role, Temporary Password) · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** Claude Design project `Users and Roles.html` (see [users-and-roles-design.md](../users-and-roles-design.md)). Frames for this slice:
- 2c / 2d / 2e: the user menu opened from the avatar (header with avatar and email, then "Change my password" with a key icon and "Log out" with a sign-out icon)
- 9a: Change my password (Current password, New password, Confirm new password; Cancel + "Change password")
- 9b: Mismatch, client-side (error under Confirm, submit disabled)
- 9c: Wrong current password, from the server (error message first in the dialog body)
- 9d: Success option A, stay signed in (success toast "Password changed" / "You're still signed in.") - **chosen**
- 9e: Success option B, sign in again - not used

Task 5 spells out the layout and copy, so it can be built without the `claude_design` MCP.

**Branch:** `88-change-my-password`, from `main` at `8cb0831` (#87 merged). This plan is committed on the branch before task 1. The PR targets `main`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| User aggregate | `Create`, `Delete`, `Restore`, `ChangeDetails`, `ChangeRole`, `SetTemporaryPassword(PasswordHash)`. `Delete`, `Restore`, `ChangeRole` and `SetTemporaryPassword` rotate `SecurityStamp`. Private guard `MustNotBeDeleted` throws `UserAlreadyDeletedException(Id)`. | `src\DrivingLessons.Domain\Entities\User.cs` |
| Values | `PasswordHash` (non-blank), `TemporaryPassword` (non-blank, keeps spaces), `SecurityStamp` | `src\DrivingLessons.Domain\Values\` |
| Ports | `IPasswordHasher.Hash(string)` / `Verify(PasswordHash, string)`; `IJwtTokenGenerator.Generate(User)` returns `IssuedToken(AccessToken, ExpiresAtUtc)`; `ICurrentUser.Id`; `IUserRepository.GetAsync(UserId)` | `src\DrivingLessons.Application\Auth\`, `Domain\Repositories\` |
| Sign-in | `LoginInteractor` verifies with `IPasswordHasher.Verify` and returns `LoginResult(AccessToken, ExpiresAtUtc)`; a bad password throws `AuthenticationFailedException`, which the filter maps to **401** | `Application\Auth\LoginInteractor.cs`, `Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Per-request check | A stale stamp gets a 401, and the client's 401 interceptor signs the User out (except on `/api/auth/login`) | `Application\Auth\CheckSignedInUserInteractor.cs`, `client\src\app\core\auth.interceptor.ts` |
| Authorization | `FallbackPolicy` requires an authenticated User; `UserCommandController` adds the Administrator policy at class level | `Presentation.Web\Program.cs`, `Controllers\User\` |
| Precedent | `SetUserTemporaryPasswordInteractor` (value object → hash → domain → commit) and its test | `Application\Commands\SetUserTemporaryPassword\`, `tests\DrivingLessons.Application.Test\Commands\` |
| Client auth | `AuthService`: `token` signal, `email` / `userId` from the token, `login()` stores the token in `localStorage['auth_token']`, `logout()` | `client\src\app\core\auth.service.ts` |
| Admin shell | Top bar: logo, nav, language toggle, avatar initials + email, a text "Sign out" button. No user menu, no `DialogService`. | `client\src\app\features\admin-shell\admin-shell.component.*` |
| Dialog building blocks | `dialog-form.scss` (`.dialog-form`, `.field`, `.field__error`, `.dialog-form__actions`), `DialogRefusalComponent` (error `p-message` titled `general.refusedTitle`), dialogs built with `DynamicDialog` + reactive forms + `p-password` | `client\src\app\shared\components\dialog-refusal\` and `client\src\app\shared\dialogs\dialog-form.scss` (moved here from `features\users`) |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Stay signed in (design 9d, option A).** The endpoint returns a fresh token from the User *after* the stamp rotation and commit. The client stores it, then shows the toast "Password changed" / "You're still signed in.". Every other session of that User (another browser, another device) dies on its next request, because its stamp is now stale. |
| 2 | **A wrong current password is a 409, not a 401.** It throws the new domain-layer rule `UserCurrentPasswordMustBeCorrectException` (code `userCurrentPasswordMustBeCorrect`), not `AuthenticationFailedException`. A 401 would trip the client's interceptor and sign the User out, which is exactly what the design avoids (9c keeps the dialog open). |
| 3 | **`User.ChangePassword(PasswordHash)` is its own method with its own event `UserPasswordChanged(UserId)`**, not a reuse of `SetTemporaryPassword` (CLAUDE.md rule 9; AC 1). It guards `MustNotBeDeleted`, rotates the stamp and carries no hash in the event. There is no same-password guard: neither the spec nor the design asks for one. |
| 4 | **A new `Password` value object for the new password** (`Password.Of`: non-blank, kept verbatim with spaces, `PasswordMustNotBeEmptyException`). `TemporaryPassword` is a different concept and its error copy says "temporary". No length or complexity rule: the spec gives none, and `TemporaryPassword` has none. |
| 5 | **Interactor order:** load the signed-in User (missing → `UserNotFoundException`) → `Password.Of(newPassword)` → verify the current password (a `null` current password counts as empty, so it's wrong) → hash → `user.ChangePassword` → commit → issue the token. Validating the new password first keeps a blank form from reaching the hasher. |
| 6 | **Route:** `PUT api/me/password` on a new `MeCommandController` (`Controllers\Me\`), tag "Me". It returns `200` with `ChangeMyPasswordResponse { accessToken, expiresAtUtc }`, the same shape as `LoginResult`. There is no class-level policy, so the fallback (authenticated User) lets Teacher-role Users in (AC 2). |
| 7 | **The client lives in the `admin-shell` feature**, because the dialog opens from the shell's user menu and one feature must not import another. The dialog building blocks it shares with the `users` dialogs moved to `shared\`: `DialogRefusalComponent` to `shared\components\dialog-refusal\` and `dialog-form.scss` to `shared\dialogs\`, with the title key renamed to `general.refusedTitle` so `shared\` stays feature-agnostic. The slice: `data\me-api.service.ts` (one service per controller pair: Me), `state\my-password.store.ts`, `ui\dialogs\change-my-password\`. The admin shell imports the dialog. |
| 8 | **The dialog provides its own `MyPasswordStore`** (`providers: [MyPasswordStore]`). Each opening gets a fresh store, so a reopened dialog never shows the previous refusal. The shell has no store and passes no data. |
| 9 | **The confirmation check is client-side only** (design 9b, "Mismatch (client-side)"). The pure `passwordsMatch` lives in `domain\`, and the request carries only `currentPassword` and `newPassword`. It is UX feedback, not a business rule (CLAUDE.md rule 12). |
| 10 | **User menu scope.** The menu header shows the avatar initials and the sign-in email (dir="ltr"). Name, Role tag and linked Teacher (design 2c) aren't in the token and belong to slice (2), "Roles, policies and Teacher scoping". "Sign out" keeps the existing `shell.logout` key and copy instead of the mock's "Log out". |
| 11 | **No field-level highlight on a wrong current password**, like #87 decision 10: the store holds the refusal as a translated string, not a code. The refusal message itself is shown first in the dialog body (design 9c). |
| 12 | **Smoke tests and browser checks run against throwaway databases** (`drivinglessons_us88_smoke`, `drivinglessons_us88_verify`), like #87 decision 11. Button casing and colors come from the app theme, like #87 decision 12. |

## Global Constraints

- `Domain` depends on nothing, and `Application` depends only on `Domain`. Raw `Guid` and `string` stop at the interactor boundary; past it, code uses typed IDs and value objects.
- Exceptions: one class per rule, named `{Entity}{Rule}Exception : DomainException` (value objects: `{Value}{Rule}Exception`). Messages never contain names, emails, passwords, hashes or stamps; ids are fine.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors injected via `[FromServices]` on the action, `EndpointSummary` and `ProducesResponseType` on every action, a CQRS split, and zero logic.
- Every new `DomainException` gets an `ApiExceptionFilterTest` case and an `errors.{code}` key in both `client\public\i18n\en.json` and `he.json`.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe to stores.
  - No hardcoded user-visible strings. Hebrew comes first.
  - Logical CSS properties only. Emails and passwords render with `dir="ltr"`.
  - DTO names identical to the backend's.
- Terminology: User, Role, Administrator, Teacher Role, Temporary Password. Never "account" for a User.

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

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **A wrong current password must leave the User signed in**: a 409 with `userCurrentPasswordMustBeCorrect`, the dialog stays open, no redirect to `/login`. Covered in task 2 (`Wrong_Current_Password_Is_A_Conflict_Not_An_Unauthorized`), task 3's smoke test, task 4 (`keeps the refusal and the current token when the server refuses`) and task 6 step 4.
2. **The token the client keeps must carry the new security stamp**, or the very next request is a 401 and the User is thrown out right after "You're still signed in.". Covered in task 2 (`The_Fresh_Token_Carries_The_New_Security_Stamp`), task 3 (the new token gets `200`, the old one `401`) and task 6 step 5.
3. **A Teacher-role User can change their own password**, because the endpoint mustn't inherit the Administrator policy. Covered in task 3's smoke test and task 6 step 7.
4. **A new password with surrounding spaces is stored as typed**, so signing in with exactly what was typed works. Covered in task 1 (`Password_Keeps_Its_Surrounding_Spaces`), task 2 (`Surrounding_Spaces_In_The_New_Password_Are_Kept`) and task 3's smoke test.
5. **Reopening the dialog after a refusal shows a clean dialog**, and a retried change doesn't keep the old refusal. Covered in task 4 (`starts a new change without the previous refusal`) and by decision 8 (a fresh store per opening), checked in task 6 step 4.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Values\Password.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Exceptions\PasswordMustNotBeEmptyException.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Events\UserPasswordChanged.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Entities\User.cs` | `ChangePassword` | 1 |
| `tests\DrivingLessons.Domain.Test\Values\PasswordTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs` | New tests | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserCurrentPasswordMustBeCorrectException.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordRequest.cs`, `ChangeMyPasswordResponse.cs`, `ChangeMyPasswordInteractor.cs` | **New** | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register the interactor | 2 |
| `tests\DrivingLessons.Application.Test\Commands\ChangeMyPasswordInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | Two cases | 2 |
| `client\public\i18n\en.json`, `he.json` | `errors.*` (2); `myPassword.*`, `shell.*` (5) | 2, 5 |
| `src\DrivingLessons.Presentation.Web\Controllers\Me\MeCommandController.cs` | **New** | 3 |
| `client\src\app\core\auth.service.ts`, `.spec.ts` | `useToken`, `storedToken` | 4 |
| `client\src\app\core\auth.interceptor.ts`, `.spec.ts` | A 401 adopts a newer stored token instead of signing out | final review |
| `client\src\app\features\admin-shell\data\change-my-password.request.ts`, `change-my-password.response.ts`, `me-api.service.ts` | **New** | 4 |
| `client\src\app\features\admin-shell\domain\passwords-match.ts`, `.spec.ts` | **New** | 4 |
| `client\src\app\features\admin-shell\state\my-password.store.ts`, `.spec.ts` | **New** | 4 |
| `client\src\app\features\admin-shell\ui\dialogs\change-my-password\*` | **New** | 5 |
| `client\src\app\shared\components\dialog-refusal\*`, `client\src\app\shared\dialogs\dialog-form.scss` | Moved from `features\users`, shared by both features; title key is `general.refusedTitle` | final review |
| `client\src\app\features\admin-shell\admin-shell.component.ts`, `.html`, `.scss` | User menu, opens the dialog | 5 |
| `docs\modules\auth\users-and-roles-design.md` | Slice (4) links this plan | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-user-change-password.md](task-01-user-change-password.md) | `Password` value object, `User.ChangePassword`, `UserPasswordChanged`, TDD | ✅ own commit |
| 2 | [task-02-change-my-password-interactor.md](task-02-change-my-password-interactor.md) | Interactor with current-password check and fresh token, rule exception, filter cases, `errors.*` translations, TDD | ✅ own commit |
| 3 | [task-03-me-password-endpoint.md](task-03-me-password-endpoint.md) | `PUT api/me/password`, API smoke for both Roles and the token swap | ✅ own commit |
| 4 | [task-04-client-my-password-state.md](task-04-client-my-password-state.md) | `AuthService.useToken`, DTOs, `MeApiService`, `passwordsMatch`, `MyPasswordStore`, specs first | ✅ own commit |
| 5 | [task-05-client-change-my-password-dialog.md](task-05-client-change-my-password-dialog.md) | Dialog, shell user menu, `myPassword.*` / `shell.*` translations | ✅ own commit |
| 6 | [task-06-verify-and-pr.md](task-06-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL, English, both Roles), PR | ✅ PR |

The PR targets `main`, closes #88 (`Closes #88`) and references the parent spec #82.
