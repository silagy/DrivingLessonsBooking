# #84: User Aggregate Takes Over Sign-In from the Admin Record - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84), the first slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. There are no sub-issues; every commit references #84.

**Goal:** Sign-in moves from the infrastructure-only `AdminUser` record to a new `User` aggregate. The existing admin keeps signing in exactly as today, now as an Administrator User. The configured email and password only create the first Administrator on an empty database.

**Architecture:** A new `User` aggregate in the domain (typed ID, value objects, `Role` enum, created and deleted events, repository interface). EF Core maps it to a new `users` table. A data migration copies the admin row into `users` and drops `admin_users`. `LoginInteractor` looks Users up through `IUserRepository` and hashes and verifies through an `IPasswordHasher` port (the renamed `IPasswordVerifier`), so the domain only ever sees a `PasswordHash` value object. A new `SeedFirstAdministratorInteractor` replaces `AdminSeeder` and does nothing once any User exists. The JWT carries the User id, Role, linked Teacher id (when present) and security stamp.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql, `Microsoft.AspNetCore.Identity.PasswordHasher<T>`, `Microsoft.IdentityModel.JsonWebTokens`, MSTest 4 + Shouldly + FakeItEasy. No new package. No client change.

**Spec:** issue [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) ("User aggregate", "Authentication and authorization", "Testing Decisions") · [CONTEXT.md](../../../../CONTEXT.md) glossary (User, Role, Administrator, Teacher Role, Deleted User) · [ADR 0004](../../../decisions/0004-no-admin-domain-aggregate.md) (the decision this reverses) · `.claude\rules\domain-building-blocks.md`, `ddd-architecture.md`, `domain-testing.md`, `code-style.md`

**Design:** none for this slice, which has no client change. The Users and Roles design for the later client slices of #82 is in [users-and-roles-design.md](../users-and-roles-design.md).

**Branch:** `84-user-aggregate-sign-in`, from `main` at `e20e8fe`. Commit this plan (and `CONTEXT.md`, where the plan's glossary terms come from) on it before task 1.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Admin record | `AdminUser` (`Id`, `Email`, `PasswordHash`), plain infrastructure class, table `admin_users` with PascalCase columns `"Id"`, `"Email"`, `"PasswordHash"` | `src\DrivingLessons.Infrastructure\Auth\AdminUser.cs`, `EntityConfigurations\AdminUserConfiguration.cs` |
| Lookup | `IAdminAccountGateway.FindByEmailAsync(normalizedEmail)` | `Application\Auth\IAdminAccountGateway.cs`, `Infrastructure\Auth\AdminAccountGateway.cs` |
| Seeding | `AdminSeeder.SeedAsync` **re-hashes the configured password on every start** and overwrites the stored row | `Infrastructure\Auth\AdminSeeder.cs`, called from `Presentation.Web\Program.cs` |
| Hashing | `IPasswordVerifier.Verify(string hash, string password)` over `PasswordHasher<object>` | `Application\Auth\IPasswordVerifier.cs`, `Infrastructure\Auth\PasswordVerifier.cs` |
| Token | `Generate(Guid adminId, string email)`, claims `sub`, `email`, hard-coded `role=admin` | `Infrastructure\Auth\JwtTokenGenerator.cs` |
| Login | `LoginInteractor.Handle(LoginCommand, CancellationToken)`, normalizes the email by hand | `Application\Auth\LoginInteractor.cs`, `Presentation.Web\Controllers\AuthController.cs` |

The client reads only the `email` claim (`client\src\app\core\auth.service.ts`), so the token keeps `email` and no client change is needed in this slice.

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **`User.Delete()` ships in this slice.** #84 requires "a deleted User cannot sign in", and Application.Test can only build a deleted User through a domain method (it doesn't reference Domain.Test and there's no reflection in tests). `Delete` has its "already deleted" guard, its event and a new security stamp. The self-delete and last-Administrator guards stay in the Users-screen slice, where the interactor lives. |
| 2 | **The sign-in email reuses the existing `Email` value object.** `Email.Of` already trims and lower-cases, so a unique index on the stored value gives case-insensitive uniqueness. The property is named `SignInEmail` to set it apart from `Teacher.ContactEmail`. |
| 3 | **`IPasswordVerifier` becomes `IPasswordHasher`** with `Hash` and `Verify` over `PasswordHash`. This is "the existing hasher abstraction" from #82. The implementation is `IdentityPasswordHasher`, so it doesn't share a name with `Microsoft.AspNetCore.Identity.PasswordHasher<T>`. It keeps `PasswordHasher<object>`, which verifies hashes written by the old `PasswordHasher<AdminUser>`, because the hash format doesn't depend on the user type. Task 4 pins this with a test. |
| 4 | **A malformed email at login is a 401, not a 409.** `Email.Of` throws `EmailMustBeValidException`, which the filter maps to 409 `emailMustBeValid`. That would tell a caller the address is malformed. `LoginInteractor` turns it into the same `AuthenticationFailedException` as a wrong password. |
| 5 | **The data migration runs in the same migration that drops `admin_users`** (`MoveAdminToUsers`, task 5), not in the one that creates `users` (task 3). The copy then reads the row at the moment it is removed. `WHERE NOT EXISTS` on the email makes it safe on a database where `users` already holds that address. |
| 6 | **The migrated and seeded Administrator is named `Administrator`.** `admin_users` has no name column. `AdminOptions` gains `Name` with the default `"Administrator"`, so `.env` and `docker-compose.yml` need no change. The Users screen (later slice) lets an Administrator rename it. |
| 7 | **Role claim values are `administrator` and `teacher`**, camelCase like the API's JSON enums. The claim names are `role`, `teacher_id`, `security_stamp`, kept as constants in `Infrastructure\Auth\AuthClaims.cs` so the policies slice reads the same names. |
| 8 | **No foreign key from `users.teacher_id` to `teachers`.** It mirrors `students.teacher_id`, which has none. "A Teacher has at most one User" is an interactor rule in the Users-screen slice (#82), so no filtered unique index yet. |
| 9 | **`users` has no soft-delete query filter**, unlike `teachers`. Login must load a Deleted User to reject it, and the later Users list and Restore need Deleted Users too. |
| 10 | **`LoginInteractor.Handle(command, cancellationToken)` becomes `ExecuteAsync(command)`.** Repositories in this codebase take no cancellation token, so the parameter would be unused, which `code-style.md` forbids. This also matches every other interactor. |

## Global Constraints

- `Domain` depends on nothing. `Application` depends on `Domain`. Raw `Guid` and `string` stop at the interactor boundary: typed IDs and value objects inside.
- Value objects: nominal records, private constructor, `Of()` / `New()` factories only, normalization inside `Of()`, one `{Value}Must{Rule}Exception : DomainException` per rule. PII and secret values (emails, names, hashes) never go into exception messages.
- Aggregates: private constructors, a static `Create` that defines every initial value as a local, method bodies ordered guards → mutations → events, one past-tense event per state change, non-idempotent operations.
- No comments in code. Test section markers `//given //when //then //expected` are the only exception.
- No long dashes or ellipsis characters in source (`SourceTextTest` guards this). Use a plain `-`.
- Always `var`, braces on every block, ternaries multiline, no nested method calls outside tests, all parameters used.
- Tests: MSTest + Shouldly. FakeItEasy in Application.Test only. Domain tests go through the aggregate root and build data with `Faker`.
- Snake_case table and column names. Enums stored as their numeric value.

**Commands** (from the repository root):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Before anything touches the database, run the compose Postgres, not `dl-postgres`: `docker stop dl-postgres` then `docker compose up -d postgres`.

## Review Focus

1. **A malformed or missing email at login** (`"not-an-email"`, `""`, `null`) must be the generic 401, not a 409 `emailMustBeValid` that reveals the format check. Covered in task 5 (`Malformed_Email_Is_Rejected_Like_A_Wrong_Password`).
2. **The sign-in email typed with other casing or surrounding spaces** (`" Owner@School.Example "`) must still sign the User in. Covered in task 5 (`Sign_In_Email_Ignores_Case_And_Surrounding_Spaces`).
3. **A Deleted User with the correct password** must get exactly the wrong-password failure, and no token is issued. Covered in task 5 (`Deleted_User_Is_Rejected_Like_A_Wrong_Password`).
4. **The existing admin's stored hash**, written by `PasswordHasher<AdminUser>`, must still verify after the move to `users`. Otherwise the migrated Administrator is locked out. Covered in task 4 (`Verifies_A_Hash_Written_By_The_Old_Admin_Seeder`) and by the browser check in task 6.
5. **A restart with a different configured email or password** once a User exists must create nothing and re-hash nothing. Covered in task 4 (`Leaves_Existing_Users_Untouched`) and by the restart check in task 6.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Values\UserId.cs`, `UserName.cs`, `PasswordHash.cs`, `SecurityStamp.cs`, `Role.cs` | **New** value types | 1 |
| `src\DrivingLessons.Domain\Exceptions\UserNameMustNotBeEmptyException.cs`, `PasswordHashMustNotBeEmptyException.cs`, `SecurityStampMustNotBeEmptyException.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Values\UserNameTest.cs`, `PasswordHashTest.cs`, `SecurityStampTest.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Entities\User.cs` | **New** aggregate: `Create`, `Delete` | 2 |
| `src\DrivingLessons.Domain\Events\UserCreated.cs`, `UserDeleted.cs` | **New** | 2 |
| `src\DrivingLessons.Domain\Exceptions\UserWithTeacherRoleMustHaveLinkedTeacherException.cs`, `UserAlreadyDeletedException.cs` | **New** | 2 |
| `src\DrivingLessons.Domain\Repositories\IUserRepository.cs` | **New** | 2 |
| `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`, `Entities\Fake\UserFakeBuilder.cs` | **New** | 2 |
| `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\UserIdConverter.cs`, `UserNameConverter.cs`, `PasswordHashConverter.cs`, `SecurityStampConverter.cs` | **New** | 3 |
| `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\UserConfiguration.cs` | **New** | 3 |
| `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\UserRepository.cs` | **New** | 3 |
| `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` | `Users` set (3), `AdminUsers` set removed (5) | 3, 5 |
| `src\DrivingLessons.Infrastructure\DependencyInjection.cs` | Register `IUserRepository` (3), `IPasswordHasher` (4), drop gateway (5) | 3, 4, 5 |
| `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\*_AddUsers.cs` (+ Designer, snapshot) | **New**, generated | 3 |
| `src\DrivingLessons.Application\Auth\IPasswordVerifier.cs` → `IPasswordHasher.cs` | Renamed, gains `Hash` | 4 |
| `src\DrivingLessons.Infrastructure\Auth\PasswordVerifier.cs` → `IdentityPasswordHasher.cs` | Renamed, gains `Hash` | 4 |
| `src\DrivingLessons.Application\Commands\SeedFirstAdministrator\SeedFirstAdministratorInteractor.cs`, `SeedFirstAdministratorRequest.cs` | **New** | 4 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register the seeding interactor | 4 |
| `tests\DrivingLessons.Application.Test\Auth\IdentityPasswordHasherTest.cs`, `Commands\SeedFirstAdministratorInteractorTest.cs` | **New** | 4 |
| `src\DrivingLessons.Application\Auth\IAdminAccountGateway.cs`, `Infrastructure\Auth\AdminAccountGateway.cs` | Hash typed as `PasswordHash` (4), **deleted** (5) | 4, 5 |
| `src\DrivingLessons.Application\Auth\LoginInteractor.cs` | Hasher port (4), Users + `ExecuteAsync` (5) | 4, 5 |
| `src\DrivingLessons.Application\Auth\IJwtTokenGenerator.cs`, `Infrastructure\Auth\JwtTokenGenerator.cs` | `Generate(User)`, new claims | 5 |
| `src\DrivingLessons.Infrastructure\Auth\AuthClaims.cs` | **New** claim names and Role values | 5 |
| `src\DrivingLessons.Infrastructure\Auth\AdminUser.cs`, `AdminSeeder.cs`, `EntityConfigurations\AdminUserConfiguration.cs` | **Deleted** | 5 |
| `src\DrivingLessons.Infrastructure\Options\AdminOptions.cs` | Gains `Name` | 5 |
| `src\DrivingLessons.Presentation.Web\Program.cs` | Seeds through the interactor | 5 |
| `src\DrivingLessons.Presentation.Web\Controllers\AuthController.cs` | Calls `ExecuteAsync` | 5 |
| `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\*_MoveAdminToUsers.cs` (+ Designer, snapshot) | **New**, generated then edited | 5 |
| `tests\DrivingLessons.Application.Test\Auth\LoginInteractorTest.cs`, `JwtTokenGeneratorTest.cs` | **New** | 5 |
| `docs\development\running-the-project.md`, `docs\decisions\0004-no-admin-domain-aggregate.md`, `CLAUDE.md` | Seeding wording, ADR status, domain snapshot | 6 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-user-values.md](task-01-user-values.md) | `UserId`, `UserName`, `PasswordHash`, `SecurityStamp`, `Role`, TDD | ✅ own commit |
| 2 | [task-02-user-aggregate.md](task-02-user-aggregate.md) | `User.Create` / `User.Delete`, events, guards, `IUserRepository`, `UserTest` + `UserFakeBuilder`, TDD | ✅ own commit |
| 3 | [task-03-persist-users.md](task-03-persist-users.md) | Converters, `UserConfiguration`, `UserRepository`, `AddUsers` migration | ✅ own commit |
| 4 | [task-04-password-hasher-and-seeding.md](task-04-password-hasher-and-seeding.md) | `IPasswordHasher` port, `SeedFirstAdministratorInteractor`, TDD | ✅ own commit |
| 5 | [task-05-sign-in-through-users.md](task-05-sign-in-through-users.md) | Login through Users, token claims, seeding wired, admin record removed, `MoveAdminToUsers` data migration, TDD | ✅ own commit |
| 6 | [task-06-docs-verify-and-pr.md](task-06-docs-verify-and-pr.md) | Docs, full suites, in-app verification of migration and restart, PR | ✅ PR |

The PR body closes #84 (`Closes #84`) and references the parent spec #82.
