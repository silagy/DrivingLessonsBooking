# Task 4 of 6: An `xmin` concurrency token on Student

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires task 1 committed (`CommitAsync` maps `DbUpdateConcurrencyException`).

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\StudentConfiguration.cs`
- Create: the migration `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\<timestamp>_AddStudentConcurrencyToken.cs` and its `.Designer.cs`. `DrivingLessonsDbContextModelSnapshot.cs` changes too.

**Interfaces:**
- Consumes (task 1): `DrivingLessonsDbContext.CommitAsync()` throws `ConcurrentChangeException` on `DbUpdateConcurrencyException`.
- Produces: every `UPDATE students` that EF issues carries `WHERE xmin = @original`. A Student changed by another request since it was read fails to save, and the request answers 409 `concurrentChange`. The `Student` domain class doesn't change: the token is a shadow property.

**Why:** README decision 5. The Teacher and Car locks of tasks 2 and 3 don't cover two requests on the *same* Student that lock different rows. Example: Change Car checks the new Car against the Student's Teacher as read, while a parallel Change Teacher replaces that Teacher. The token makes the later commit fail instead of saving a Car the new Teacher doesn't teach on. Task 6's scenario E proves it on Postgres.

- [ ] **Step 1: Map `xmin` as Student's row version**

In `StudentConfiguration.Configure`, after the `is_active` property mapping, add:

```csharp
        builder
            .Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
```

Notes:
- `xmin` is a Postgres system column that every row has; it changes on every update of the row.
- Npgsql reads it back after each save and puts it in the `WHERE` of the next `UPDATE`.
- There is no domain property: `Student` stays free of persistence concerns, and no code reads the value.

- [ ] **Step 2: Build**

```bash
dotnet build
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Add the migration**

Before anything touches the database, switch to the compose Postgres: `docker stop dl-postgres`, then `docker compose up -d postgres`. Then:

```bash
dotnet ef migrations add AddStudentConcurrencyToken --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

Expected: a new `<timestamp>_AddStudentConcurrencyToken.cs` next to `20261005191744_RemoveRosterDeactivation.cs` in `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\`. Its `Up` holds an `AddColumn<uint>(name: "xmin", table: "students", type: "xid", rowVersion: true, ...)`, and `Down` holds the matching `DropColumn`. If the files land in another folder, move them next to the other migrations and fix their namespace to match.

- [ ] **Step 4: Prove the migration creates no DDL**

`xmin` already exists on every table, so the migration must not try to add it.

```bash
dotnet ef migrations script 20261005191744_RemoveRosterDeactivation AddStudentConcurrencyToken --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Expected: only `START TRANSACTION;`, the `INSERT INTO "__EFMigrationsHistory" ...` line for `AddStudentConcurrencyToken`, and `COMMIT;`. No `ALTER TABLE`. Npgsql's migrations SQL generator skips system columns.

**If an `ALTER TABLE students ADD xmin` does appear**, it would fail at start-up with `column "xmin" conflicts with a system column name`. In that case:
- Empty the migration's `Up` and `Down` bodies, leaving only the method signatures, and keep the `.Designer.cs` and snapshot as generated, so the model and snapshot still agree.
- Re-run the script command and expect no `ALTER TABLE`.
- Record this in the task report.

- [ ] **Step 5: Check the model and snapshot agree, and run the suites**

```bash
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: `No changes have been made to the model since the last migration.`, then both suites pass.

- [ ] **Step 6: Apply it to a throwaway database**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us109_migrate"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us109_migrate"
dotnet ef database update --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --connection "Host=localhost;Port=5432;Database=drivinglessons_us109_migrate;Username=app;Password=devpassword"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us109_migrate -At -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1 DESC LIMIT 1"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE drivinglessons_us109_migrate"
```

Expected: `database update` ends with `Done.`, and the last migration id ends in `_AddStudentConcurrencyToken`. If the password differs on this machine, take it from `appsettings.Development.json`'s `ConnectionStrings:Default`.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/StudentConfiguration.cs src/DrivingLessons.Infrastructure/EntityFramework/Migrations
git commit -m "feat(students): refuse saving a Student another request changed meanwhile (xmin token) (#109)"
```

End the commit message with the attribution trailer from the session's instructions.
