# Task 3: Verify and Open the PR

Part of [#108 plan](README.md). No commit unless a check finds a defect. Fix it in the task that owns the code, as a new commit referencing #108.

- [ ] **Step 1: Full builds and suites**

Run: `dotnet build` and `dotnet test` at the repo root. Then, from `client\` (memory "Client build / npm workaround"), `npx ng build` and `npx ng test --watch=false`.
Expected: zero warnings introduced, all green.

- [ ] **Step 2: Postgres smoke on a throwaway database**

Unit tests can't prove that `IgnoreQueryFilters()` really loads a soft-deleted row (Review Focus 2). Point the API at `drivinglessons_us108_smoke` on `drivinglessonsbooking-postgres-1` (memory "Local postgres: use compose"; migrations create it on start-up). Sign in as the Administrator and use **file payloads** for any Hebrew (memory "Seeding Hebrew via Windows curl"). Use valid national IDs (memory "Design sample national IDs invalid").

Set up Teachers T1, T2, Cars C1 (assigned to T1), C2 (assigned to T1), C3 (assigned to T1 and T2), and Students S1 (T1/C1), S2 (T1/C2), S3 (T2/C3), S4 (T1/C3). Then:

| # | Do | Expect |
|---|----|--------|
| a | Deactivate S1, delete C1, `POST api/students/{S1}/reactivate` | 409 `studentCarMustNotBeDeleted`; S1 still inactive |
| b | Deactivate S3, delete T2 (S3 is its only Student, now inactive; C3 stays assigned to T2), reactivate S3 | 409 `studentTeacherMustNotBeDeleted` (not 404 `teacherNotFound`) |
| c | Deactivate S2, unassign C2 from T1, reactivate S2 | 409 `studentCarMustBeAssignedToTeacher` |
| d | Assign C2 back to T1, reactivate S2 | 204; S2 active |
| e | Reactivate S2 again | 409 `studentAlreadyActive` |
| f | Deactivate S4, reactivate S4 | 204 |
| g | Import a Roster CSV with S1 on T1/C2 | S1 updated to T1/C2 and active |

Drop the database afterwards.

- [ ] **Step 3: Browser check, Hebrew (RTL) and English (LTR)**

Start the dev server with `preview_start`. Use DOM reads rather than screenshots (memory "Browser pane screenshots"). On the Students screen with the "All" filter, Reactivate the cases from step 2 again:

- (a)/(b)/(c): each shows an error toast with its `errors.*` text in both languages, and the row stays "לא פעיל" / "Inactive".
- (d): the success toast, and the row turns active.
- Add Student and Change Car (Review Focus 5): open the dialog and pick a Teacher and Car. In another tab, unassign that Car from that Teacher, then Save. The dialog's refusal shows the reworded `errors.studentCarMustBeAssignedToTeacher`, and it reads correctly in both languages.

- [ ] **Step 4: Open the PR into `82-users-and-roles`**

```bash
git push -u origin 108-reactivate-student-guards
```

```bash
gh pr create --base 82-users-and-roles --title "Refuse reactivating a Student whose Teacher or Car was removed (#108)" --body-file <scratchpad>/pr-108.md
```

Body: Closes #108. Summarise the three refusals and their codes, the guard order (Decision 3), the fix path (Decision 2), and the reworded shared message (Decision 8). List the smoke table results and the browser checks. End with the attribution line from the session's system reminder. Then bind the PR with the `ccd_pr` tools.
