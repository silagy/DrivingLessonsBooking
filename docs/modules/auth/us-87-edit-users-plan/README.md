# #87: Edit a User's Details and Role, and Set a Temporary Password - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#87](https://github.com/silagy/DrivingLessonsBooking/issues/87), which is slice (3) of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #87.

**Goal:** Administrators can do three new things to a User:
- edit their name and sign-in email
- change their Role
- set a new Temporary Password

A Role change or a new password signs the User out on their next request.

**Architecture:**
- `User` gains three methods: `ChangeDetails`, `ChangeRole` and `SetTemporaryPassword`. Each raises its own past-tense event, and `ChangeRole` and `SetTemporaryPassword` rotate the security stamp. #86's per-request check (`CheckSignedInUserInteractor`) then turns the old token into a 401.
- Three interactors run the application rules:
  - email uniqueness, checked only when the email changes
  - you can't change your own Role
  - the last active Administrator keeps the Administrator Role
- The interactors are exposed as `PUT api/users/{id}/details`, `/role` and `/temporary-password`.
- The Users screen's row menu gets Edit details, Change Role and Set Temporary Password. Each opens a dialog that shows 409 refusals inside itself, through one store-level `refusal` signal shared with the Delete dialog.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration.

**Spec:** issue [#87](https://github.com/silagy/DrivingLessonsBooking/issues/87) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Teacher Role, Temporary Password, Deleted User) · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** Claude Design project `Users and Roles.html` (see [users-and-roles-design.md](../users-and-roles-design.md)). Frames for this slice:
- 3b: row actions menu (Edit details, Change Role, Set Temporary Password, divider, Delete)
- 5a / 5b: Edit details; refused because the email is already used
- 6a-6f: Change Role (promote, demote; refused for: no linked Teacher, yourself, last active Administrator, already has this Role)
- 7a: Set Temporary Password

Task 5 spells out the layout and copy, so it can be built without the `claude_design` MCP.

**Branch:** `87-edit-users`, from `main` at `ed7fed0` (#86 merged). This plan is committed on the branch before task 1. The PR targets `main`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| User aggregate | `Create`, `Delete`, `Restore` (both rotate `SecurityStamp`). Private guards `MustHaveDefinedRole`, `MustHaveLinkedTeacherForTeacherRole` (static, takes a `Teacher?`), `MustNotBeDeleted`, `MustBeDeleted`. | `src\DrivingLessons.Domain\Entities\User.cs` |
| Values | `UserName`, `Email` (trims, lowercases), `Role` (`Administrator = 10`, `Teacher = 20`), `PasswordHash`, `TemporaryPassword` (non-blank), `SecurityStamp` | `src\DrivingLessons.Domain\Values\` |
| Test builder | `UserFakeBuilder.Build()` / `BuildDeleted()` / `WithRole` / `WithTeacher` (links for both Roles) | `tests\DrivingLessons.Domain.Test\Entities\Fake\UserFakeBuilder.cs` |
| Ports | `IPasswordHasher.Hash(string)`, `ICurrentUser.Id`, `IUserRepository.GetAsync(UserId)`, `IUserQueries.ExistsWithSignInEmailAsync(Email)` (matches Deleted Users too; unique DB index), `CountActiveAdministratorsAsync()` | `Application\Auth\`, `Domain\Repositories\`, `Application\Queries\IUserQueries.cs` |
| Precedents | `ChangeTeacherDetailsInteractor` + `PUT api/teachers/{id}/details`; `DeleteUserInteractor` (self and last-Administrator guards) | `Application\Commands\` |
| Exception filter | Every `DomainException` becomes a 409. Its `code` is the class name without `Exception`, in camelCase. No filter change is needed. | `Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Per-request check | A stale stamp, a Deleted User or a missing User gets a 401. The client's 401 interceptor signs the User out. | `Application\Auth\CheckSignedInUserInteractor.cs`, `Presentation.Web\Auth\SignedInUserJwtBearerEvents.cs` |
| Users API | `POST`, `DELETE {id}`, `POST {id}/restore`, `GET find`, `GET {id}`. All require the Administrator policy. | `Presentation.Web\Controllers\User\` |
| Client store | `deleteRefusal` / `clearDeleteRefusal()`; `delete()` returns `Promise<boolean>` and keeps the refusal through `ToastService.messageOf` | `client\src\app\features\users\state\users.store.ts` |
| Client dialogs | `add-user` (reactive form, closes with a result); `delete-user` (who-card, in-dialog refusal, data contract `refusal` / `isDeleting` / `confirm`); shared `dialog-form.scss`. The `.role-switch` pill styling lives in `add-user.dialog.scss` only. | `client\src\app\features\users\ui\dialogs\` |
| Row menu | One shared popup `p-menu` holding only Delete | `client\src\app\features\users\ui\pages\users\users.page.ts` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **All three domain methods guard `MustNotBeDeleted`**, so a stale screen gets `userAlreadyDeleted`. `ChangeDetails` raises `UserDetailsChanged` and keeps the stamp: the AC names only Role and password as stamp changes, and the email claim isn't used for authorization. `ChangeDetails` has no same-value guard, like `Teacher.ChangeDetails`; saving unchanged details is harmless. `ChangeRole` raises `UserRoleChanged`, rotates the stamp and refuses the current Role with a new `UserAlreadyHasRoleException`. `SetTemporaryPassword(PasswordHash)` raises `UserTemporaryPasswordSet(UserId)`, carrying no hash, and rotates the stamp. |
| 2 | **Moving a User to the Teacher Role without a link reuses `UserWithTeacherRoleMustHaveLinkedTeacherException`.** It is the same invariant as at creation. Its `errors.*` copy is reworded to the design's "A User who isn't linked to a Teacher can't get the Teacher Role.", which reads correctly in both Add User and Change Role. **Flag this in the PR.** |
| 3 | **The linked Teacher is immutable.** No request carries a `teacherId`. Domain and interactor tests assert that `TeacherId` survives `ChangeDetails` and both directions of `ChangeRole` (AC 5). |
| 4 | **Interactors.** `ChangeUserDetailsInteractor` checks email uniqueness only when the normalized email differs from the User's own. `ChangeUserRoleInteractor` checks in this order: not found → **self** (`UserMustNotChangeOwnRoleException`) → **last active Administrator** (`UserMustNotDemoteLastActiveAdministratorException`, only when an active Administrator moves to Teacher and at most one is active) → domain. Self runs first so that an unlinked Administrator changing their own Role sees the self message (design 6d). `SetUserTemporaryPasswordInteractor` validates `TemporaryPassword`, hashes it through `IPasswordHasher`, and has **no self guard**: an Administrator who sets their own password is signed out on the next request. **Flag this in the PR.** |
| 5 | **New exceptions instead of #86's.** `UserMustNotDeleteSelf` and `UserMustNotBeLastActiveAdministrator` carry delete-specific copy ("You can't delete..."). The design gives Change Role its own copy (`err.demoteSelf`, `err.lastAdmin`, `err.sameRole`), so Change Role gets its own codes. The last-Administrator guard can't be reached through the UI, for the same reason as #86 decision 3. It stays because the issue asks for it. |
| 6 | **Routes** follow `PUT api/teachers/{id}/details`. `PUT api/users/{id}/details`, `PUT api/users/{id}/role` and `PUT api/users/{id}/temporary-password` each return 204, with 404 / 409 ProblemDetails. The controller's Administrator policy covers all three. |
| 7 | **Refusals appear inside the dialog** (design 5b, 6c-6f; inventory: "Server refusal (409) in a dialog"). `deleteRefusal` / `clearDeleteRefusal` become one `refusal` / `clearRefusal()`, because only one dialog is open at a time. Every dialog command returns `Promise<boolean>` through one private `runDialogCommand`. The success toasts `users.detailsChanged`, `users.roleChanged` and `users.temporaryPasswordSet` are new, since the design shows none. |
| 8 | **The who-card becomes a shared component** (`ui\components\user-who-card\`), used by the Delete, Change Role and Set Temporary Password dialogs. The `.role-switch` pill styling moves to `dialog-form.scss` so Add User and Change Role share it. |
| 9 | **Change Role keeps the self row's actions visible.** The server refuses (CLAUDE.md rule 12, design 6d). The current Role is disabled in the pill switch and labelled "(current)", and the other Role is preselected. The promote or demote note is picked by the pure `roleChangeNoteKey` helper, which selects copy only and enforces no rule. |
| 10 | **Edit details shows the linked Teacher as a locked read-only row** (design 5a). Known difference from 5b: the email field isn't outlined as invalid on a 409, because the store holds the refusal as a translated string, not a code. |
| 11 | **Smoke tests and browser checks run against throwaway databases** (`drivinglessons_us87_smoke`, `drivinglessons_us87_verify`), like #86 decision 13. |
| 12 | **The danger color and button casing come from the existing theme and keys** (title case, not the mock's capitals), like #86 decision 10. |

## Global Constraints

- `Domain` depends on nothing, and `Application` depends only on `Domain`. Raw `Guid` and `string` stop at the interactor boundary; past it, code uses typed IDs and value objects.
- Exceptions: one class per rule, named `{Entity}{Rule}Exception : DomainException`. Messages never contain names, emails, passwords or stamps; ids are fine.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Inside `DrivingLessons.Infrastructure`, write `DrivingLessons.Domain.Values.Email` in full (namespace clash).
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors injected via `[FromServices]` on the action, `EndpointSummary` and `ProducesResponseType` on every action, a CQRS split, and zero logic.
- Every new `{Entity}{Rule}Exception` gets an `ApiExceptionFilterTest` case and an `errors.{code}` key in both `client\public\i18n\en.json` and `he.json`.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe to stores.
  - No hardcoded user-visible strings. Hebrew comes first, using plural imperatives ("רעננו").
  - Logical CSS properties only. Emails and passwords render with `dir="ltr"`.
  - DTO names identical to the backend's.
- Terminology: User, Role, Administrator, Teacher Role, Temporary Password, Deleted User. Never "account", "disable" or "suspend" for a User.

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

1. **Saving Edit details while keeping the User's own email**, even typed in a different case, must succeed and not be refused as "already used". Covered in task 2 (`Keeping_The_Own_Email_Does_Not_Check_Uniqueness`) and in task 3's smoke test.
2. **An email that differs from another User's only by case or surrounding spaces** must be refused, because `Email.Of` normalizes it. Covered in task 2 (`Email_Another_User_Has_Is_Rejected` with `[DataRow("RONIT@school.example")] [DataRow(" ronit@school.example ")]`).
3. **An unlinked Administrator changing their own Role** must see `userMustNotChangeOwnRole`, not the Teacher-link rule. Covered in task 2 (`Demoting_Yourself_Is_Rejected_Before_The_Teacher_Link_Rule`) and in task 3's smoke test.
4. **A demoted Administrator's still-valid token** must get a 401 on the very next request, not at expiry. Covered in task 1 (`ChangeRole__Changes_The_Security_Stamp`), task 3's smoke test and task 6 step 6.
5. **Reopening any dialog after a refusal** must show a clean dialog, and a retried command must not keep the old refusal. Covered in task 4 (`clears the refusal so a reopened dialog starts clean`, `starts a new command without the previous refusal`) and in task 5's `openUserDialog`, which calls `clearRefusal()` first.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\User.cs` | `ChangeDetails`, `ChangeRole`, `SetTemporaryPassword`, `MustNotHaveRole`, `MustHaveLinkedTeacherToTakeTeacherRole` | 1 |
| `src\DrivingLessons.Domain\Events\UserDetailsChanged.cs`, `UserRoleChanged.cs`, `UserTemporaryPasswordSet.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserAlreadyHasRoleException.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs` | New tests | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserMustNotChangeOwnRoleException.cs`, `UserMustNotDemoteLastActiveAdministratorException.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Commands\ChangeUserDetails\*`, `ChangeUserRole\*`, `SetUserTemporaryPassword\*` | **New** (request + interactor each) | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register three interactors | 2 |
| `tests\DrivingLessons.Application.Test\Commands\ChangeUserDetailsInteractorTest.cs`, `ChangeUserRoleInteractorTest.cs`, `SetUserTemporaryPasswordInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | Three cases | 2 |
| `client\public\i18n\en.json`, `he.json` | `errors.*` (2); `users.*` (5) | 2, 5 |
| `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs` | Three `PUT` actions | 3 |
| `client\src\app\features\users\data\change-user-details.request.ts`, `change-user-role.request.ts`, `set-user-temporary-password.request.ts` | **New** | 4 |
| `client\src\app\features\users\data\users-api.service.ts` | Three methods | 4 |
| `client\src\app\features\users\state\users.store.ts`, `.spec.ts` | `refusal` rename, three commands, `runDialogCommand`; spec rewritten | 4 |
| `client\src\app\features\users\domain\role-change.ts`, `.spec.ts` | **New** | 5 |
| `client\src\app\features\users\ui\components\user-who-card\*` | **New** | 5 |
| `client\src\app\features\users\ui\dialogs\edit-user\*`, `change-user-role\*`, `set-temporary-password\*` | **New** | 5 |
| `client\src\app\features\users\ui\dialogs\dialog-form.scss`, `add-user\add-user.dialog.scss`, `delete-user\*` | Shared styles, who-card | 5 |
| `client\src\app\features\users\ui\pages\users\users.page.ts` | `refusal` rename (4); row menu, openers (5) | 4, 5 |
| `docs\modules\auth\users-and-roles-design.md` | Slice (3) links this plan | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-user-domain-changes.md](task-01-user-domain-changes.md) | `User.ChangeDetails` / `ChangeRole` / `SetTemporaryPassword`, three events, `UserAlreadyHasRoleException`, TDD | ✅ own commit |
| 2 | [task-02-edit-user-interactors.md](task-02-edit-user-interactors.md) | Three interactors, two rule exceptions, filter cases, `errors.*` translations, TDD | ✅ own commit |
| 3 | [task-03-edit-user-endpoints.md](task-03-edit-user-endpoints.md) | Three `PUT` endpoints, API smoke including the demote lockout | ✅ own commit |
| 4 | [task-04-client-users-state.md](task-04-client-users-state.md) | DTOs, API methods, store `refusal` + three commands, spec first | ✅ own commit |
| 5 | [task-05-client-users-dialogs.md](task-05-client-users-dialogs.md) | Who-card, three dialogs, row menu, `users.*` translations | ✅ own commit |
| 6 | [task-06-verify-and-pr.md](task-06-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL, English, lockout), PR | ✅ PR |

The PR targets `main`, closes #87 (`Closes #87`) and references the parent spec #82.
