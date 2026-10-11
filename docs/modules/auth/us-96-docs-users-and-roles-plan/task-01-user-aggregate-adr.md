# Task 1 of 5: ADR 0006 for the User aggregate, ADR 0004's status and the README

> Part of [#96: Docs for the User aggregate, the Roster amendment, requirements and glossary](README.md). Work on branch `96-docs-users-and-roles`. Requires the plan commit only.

**Files:**
- Create: `docs\decisions\0006-user-aggregate-and-roles.md`
- Modify: `docs\decisions\0004-no-admin-domain-aggregate.md` (line 3, the status line only)
- Modify: `README.md` (Documentation table, Status section)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: the ADR path `docs/decisions/0006-user-aggregate-and-roles.md` and its title "ADR 0006: User Aggregate with Two Roles". Tasks 2 and 4 link to it with these exact paths: from `docs\requirements.md` as `decisions/0006-user-aggregate-and-roles.md`, and from `CLAUDE.md` as `docs/decisions/0006-user-aggregate-and-roles.md`.

**Why:** #96 AC 1: "A new ADR records the User aggregate and the two Roles; it supersedes ADR 0004, and ADR 0004 is marked superseded."

- [ ] **Step 1: Read the predecessor**

Read `docs\decisions\0004-no-admin-domain-aggregate.md` in full, especially "Rationale" and "Revisit if". ADR 0006 answers it point by point: multiple admins, roles, admin lifecycle.

- [ ] **Step 2: Check the facts the ADR will state against the code**

Run each command. Each one must print what the "Expected" column says. If one doesn't, **change the ADR text in step 3 to match the code**, not the other way round, and note the difference for the PR description.

```bash
git grep -n "public enum Role" -A4 -- src/DrivingLessons.Domain/Values/Role.cs
git grep -n "public void \|public static User Create" -- src/DrivingLessons.Domain/Entities/User.cs
git grep -n "SecurityStamp = SecurityStamp.New()" -- src/DrivingLessons.Domain/Entities/User.cs
git grep -n "const string" -- src/DrivingLessons.Infrastructure/Auth/AuthClaims.cs
git grep -n "DefaultPolicy\|FallbackPolicy\|AddPolicy" -- src/DrivingLessons.Presentation.Web/Auth/AuthorizationPolicies.cs
git grep -n "context.Fail" -- src/DrivingLessons.Presentation.Web/Auth/SignedInUserJwtBearerEvents.cs
git grep -n "MayReach" -A3 -- src/DrivingLessons.Application | grep -c "NotFoundException"
git grep -n "AnyExistAsync" -- src/DrivingLessons.Application/Commands/SeedFirstAdministrator/SeedFirstAdministratorInteractor.cs
git grep -n "admin_users" -- "src/DrivingLessons.Infrastructure/EntityFramework/Migrations/*_MoveAdminToUsers.cs"
ls src/DrivingLessons.Domain/Exceptions | grep -E "^User(MustNot|Must|AlreadyHas|SignIn|Current|WithTeacher|Linked)"
ls src/DrivingLessons.Domain/Exceptions src/DrivingLessons.Application/Common/Exceptions | grep -E "TeacherAlreadyLinkedToUser|TeacherMustNotHaveActiveUser"
git grep -n "ExpiryHours" -- src/DrivingLessons.Presentation.Web/appsettings.json
```

| Command | Expected |
|---------|----------|
| `Role` enum | `Administrator = 10`, `Teacher = 20`, nothing else |
| `User` methods | `Create`, `Delete`, `Restore`, `ChangeDetails`, `ChangeRole`, `SetTemporaryPassword`, `ChangePassword` |
| Stamp renewals | 5 lines: in `Delete`, `Restore`, `ChangeRole`, `SetTemporaryPassword`, `ChangePassword` (not `ChangeDetails`) |
| `AuthClaims` | claim names `role`, `teacher_id`, `security_stamp`; values `administrator`, `teacher` |
| Policies | `Administrator` is both the default and the fallback policy; `TeacherOrAdministrator` is the second |
| `context.Fail` | one line: the deleted / revoked message |
| `MayReach` → not found | a count of at least 4 |
| Seeding | `AnyExistAsync` guards the create |
| Migration | `MoveAdminToUsers` copies from and drops `admin_users` |
| User exceptions | include `UserMustNotDeleteSelfException`, `UserMustNotChangeOwnRoleException`, `UserMustNotBeLastActiveAdministratorException`, `UserMustNotDemoteLastActiveAdministratorException`, `UserSignInEmailAlreadyInUseException`, `UserCurrentPasswordMustBeCorrectException`, `UserWithTeacherRoleMustHaveLinkedTeacherException`, `UserLinkedTeacherMustNotBeDeletedException`, `UserAlreadyHasRoleException` |
| Teacher-link exceptions | `TeacherAlreadyLinkedToUserException`, `TeacherMustNotHaveActiveUserException` |
| Token lifetime | `"ExpiryHours": 12` (if missing, read the `Jwt` section's default in `Infrastructure\Auth` and use that number) |

- [ ] **Step 3: Write the ADR**

Create `docs\decisions\0006-user-aggregate-and-roles.md` with exactly this content (adjusted only where step 2 found a difference):

```markdown
# ADR 0006: User Aggregate with Two Roles

**Status:** Accepted. Supersedes [ADR 0004](0004-no-admin-domain-aggregate.md).
**Date:** 11 October 2026

## Context

[ADR 0004](0004-no-admin-domain-aggregate.md) kept the administrator out of the domain: one administrator, no Roles, no operations, and credentials re-applied from configuration on every start-up. It named the conditions for revisiting that choice: multiple administrators, roles or permissions, and an administrator lifecycle.

Spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) brings all three. Teachers sign in and report their own availability, so the owner no longer marks every Teacher's Slots on their behalf. Administrators add, edit, delete and restore the people who can sign in, change their Role and set a Temporary Password, and everyone changes their own password. These operations come with rules: a Teacher-Role User is tied to exactly one Teacher, the school is never left without an Administrator, revoking access takes effect at once, and an operation already in its target state is refused. An infrastructure record with no behaviour can't hold those rules, and they can't be unit tested there.

## Decision

**A User is a domain aggregate** (`DrivingLessons.Domain\Entities\User.cs`), introduced in [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84) and replacing the `AdminUser` record and the `admin_users` table.

- **State:** name, sign-in email (the `Email` value object, so it is trimmed and lower-cased and a unique index makes it unique case-insensitively), password hash, Role, optional linked `TeacherId`, a security stamp, and a deleted flag.
- **Operations:** `Create`, `ChangeDetails`, `ChangeRole`, `SetTemporaryPassword`, `ChangePassword`, `Delete`, `Restore`. Each is non-idempotent: it refuses with a domain exception when the User is already in the target state, and raises a past-tense event (`UserCreated`, `UserDetailsChanged`, `UserRoleChanged`, `UserTemporaryPasswordSet`, `UserPasswordChanged`, `UserDeleted`, `UserRestored`).

**Exactly one Role per User, and only two Roles:**

- **Administrator** can do everything. An Administrator may be linked to a Teacher (the owner who also teaches), and then also works with that Teacher's schedule like any Teacher.
- **Teacher Role** reaches only its linked Teacher's Week Schedules and Publications (with their Submissions, share link and Excel download), and marks that Teacher's Slots Open or Unavailable. It cannot create Week Schedules or publish, open, close or reopen a Publication.

There are no other Roles, no per-permission configuration, and no User holds several Roles.

**The Teacher link.** A Teacher-Role User must be linked to a Teacher. The link is set when the User is created and never changes, so a sign-in can never be redirected to another Teacher's data. A Teacher has at most one User. A Teacher-Role User can be promoted to Administrator and keeps the link; an Administrator can be demoted only when linked.

**Where each rule lives.**

- *In the aggregate* (rules about one User): a Teacher Role needs a link; changing to the current Role is refused; a Deleted User can't be changed, deleted again or restored while active.
- *In interactors, through repositories and query ports* (rules that span Users or aggregates): the sign-in email is unique; a Teacher has at most one User and must not be deleted; no one deletes themselves or changes their own Role; the last active Administrator is never deleted or demoted; changing your own password requires the current one; a Teacher can't be deleted while an active User is linked to it.

**Passwords stay out of the domain.** The application port `IPasswordHasher` (implemented by `IdentityPasswordHasher` in Infrastructure, over ASP.NET Core's `PasswordHasher`) hashes and verifies. The aggregate receives a `PasswordHash` and never sees plaintext. `Password` and `TemporaryPassword` value objects only validate input at the application boundary.

**Sign-in and immediate lockout.** Signing in loads the User by sign-in email; a Deleted User can't sign in. The token carries the User id, the Role (`administrator` | `teacher`), the linked Teacher id when there is one, and the security stamp. The stamp changes whenever access must be re-evaluated: Role change, delete, restore, Temporary Password, password change. On every authenticated request the server checks that the User exists, isn't deleted and that the token's stamp is the current one; otherwise the request is a 401 and the client signs the User out. The token lifetime (12 hours) no longer governs revocation.

**Authorization.** Two policies. `Administrator` is the default and the fallback, so every endpoint is Administrator-only unless it says otherwise. `TeacherOrAdministrator` is on the few endpoints a Teacher needs. Teacher scoping is enforced in the application layer: interactors read the current-user port `ICurrentUser` (User id, Role, linked Teacher id) and `MayReach(TeacherId)`. Administrators reach every Teacher. A Teacher-Role User asking for another Teacher's Week Schedule or Publication gets the same not-found answer (404) as for one that doesn't exist, so a URL reveals nothing about other Teachers.

**The first Administrator.** Configuration (`ADMIN_EMAIL`, `ADMIN_PASSWORD`) creates an Administrator only when no Users exist. Once any User exists, configuration changes nothing: Users are managed in the app. The `MoveAdminToUsers` migration turned the existing admin row into an Administrator User named "Administrator", with no Teacher link and the same password hash, and dropped `admin_users`.

**Same bounded context.** ADR 0004 expected a `User` to live in a separate Identity/Access bounded context. It doesn't: `User` lives in `DrivingLessons.Domain` next to the scheduling aggregates and refers to a Teacher only by `TeacherId`. With one deployable, one database and one link (the `TeacherId`), a separate context would add projects and mapping without protecting anything.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| Keep the infrastructure `AdminUser` record and add a role column | Rejected: the rules (the last active Administrator, the fixed Teacher link, refusing an operation already in its target state) would live in infrastructure, outside the domain tests |
| ASP.NET Core Identity's user store (`IdentityUser`, `UserManager`, its tables) | Rejected: it brings its own schema and user model for what is a handful of fields here; only its password hasher is reused |
| Rely on a short token lifetime instead of a per-request check | Rejected: a deleted or demoted User would keep access until the token expired; #82 requires the lockout to take effect at once |
| Permission sets, more Roles, or several Roles per User | Rejected for now: two fixed Roles cover one school; out of scope in #82 |
| A separate Identity/Access bounded context (ADR 0004's forecast) | Rejected for now: see "Same bounded context" above |

## Consequences

- User administration is ordinary domain code: `UserTest` covers each operation's happy path, event and guards, and interactor tests cover the rules that span Users.
- Every authenticated request costs one lookup of the User to compare the security stamp. At one school's traffic this is negligible.
- Teachers report their own availability, so the owner's weekly preparation shrinks to creating the Week Schedules and running the Publication.
- Requirements decision #1 ("single login, no teacher logins") is superseded by decision #24 in [requirements.md](../requirements.md). ADR 0004 is superseded.
- Still out of scope: self-service "forgot password", password emails and invitation links, forcing a new password after a Temporary Password, sign-in for Students, and an audit log of User administration beyond the domain events.
- **Revisit if:** the school needs finer permissions than two Roles, a User needs several Roles, or multi-school tenancy arrives and a User must belong to a School (requirements §12). At that point the separate bounded context becomes worth its cost.
```

- [ ] **Step 4: Check the ADR's file references exist**

```bash
for f in src/DrivingLessons.Domain/Entities/User.cs src/DrivingLessons.Application/Auth/ICurrentUser.cs src/DrivingLessons.Application/Auth/IPasswordHasher.cs src/DrivingLessons.Infrastructure/Auth/IdentityPasswordHasher.cs src/DrivingLessons.Domain/Values/Password.cs src/DrivingLessons.Domain/Values/TemporaryPassword.cs tests/DrivingLessons.Domain.Test/Entities/UserTest.cs; do test -e "$f" && echo "ok $f" || echo "MISSING $f"; done
```

Expected: seven `ok` lines. A `MISSING` line means the ADR names something that doesn't exist: find the real name with `git grep` and fix the ADR.

- [ ] **Step 5: Point ADR 0004 at ADR 0006**

In `docs\decisions\0004-no-admin-domain-aggregate.md`, replace line 3:

```markdown
**Status:** Superseded by issue [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84): sign-in now uses the `User` aggregate. The replacement ADR comes with the docs slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82).
```

with:

```markdown
**Status:** Superseded by [ADR 0006](0006-user-aggregate-and-roles.md): sign-in uses the `User` aggregate since issue [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84).
```

Change nothing else in ADR 0004.

- [ ] **Step 6: Update the README**

In `README.md`, replace the ADR row of the Documentation table:

```markdown
| [docs/decisions/](docs/decisions/) | Architecture Decision Records: [0001 monorepo + single deployable](docs/decisions/0001-monorepo-single-deployable.md), [0002 hosting on AWS Lightsail](docs/decisions/0002-hosting-aws-lightsail.md) |
```

with these two rows:

```markdown
| [docs/decisions/](docs/decisions/) | Architecture Decision Records: [0001 monorepo + single deployable](docs/decisions/0001-monorepo-single-deployable.md), [0002 hosting on AWS Lightsail](docs/decisions/0002-hosting-aws-lightsail.md), [0003 roster CSV and the weekly link](docs/decisions/0003-roster-csv-and-weekly-link-model.md), [0004 no admin aggregate (superseded)](docs/decisions/0004-no-admin-domain-aggregate.md), [0005 email over SES SMTP](docs/decisions/0005-email-over-ses-smtp.md), [0006 User aggregate with two Roles](docs/decisions/0006-user-aggregate-and-roles.md) |
| [CONTEXT.md](CONTEXT.md) | Glossary: the domain terms to use, and the words to avoid |
```

Replace the Status section's body:

```markdown
Requirements approved. Implementation not started.
```

with:

```markdown
v1 is implemented, including Users and Roles and Student management from spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Open work is on the [issue tracker](https://github.com/silagy/DrivingLessonsBooking/issues).
```

- [ ] **Step 7: Check and commit**

```bash
git grep -n "0006-user-aggregate-and-roles" -- README.md docs/decisions
git diff --stat
```

Expected: README and ADR 0004 each link to the new file; the diff touches exactly `README.md` and `docs/decisions/0004-no-admin-domain-aggregate.md`, plus the new untracked ADR.

```bash
git add docs/decisions/0006-user-aggregate-and-roles.md docs/decisions/0004-no-admin-domain-aggregate.md README.md
git commit -m "docs(auth): ADR 0006 for the User aggregate and its two Roles, superseding ADR 0004 (#96)"
```

End the commit message with the attribution trailer from the session's instructions.
