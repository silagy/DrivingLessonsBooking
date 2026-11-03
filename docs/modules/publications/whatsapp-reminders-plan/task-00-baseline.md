# Task 0: Baseline

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** the intent and this plan are committed on the branch.
**Scope guard:** changes no files.

- [ ] **Step 0.1: Check out the branch and confirm it is clean**

Run: `git switch whatsapp-reminders` then `git status --short`.
Expected: nothing listed.

- [ ] **Step 0.2: Build and test the backend**

Run from the repo root: `dotnet build`, then `dotnet test`.
Expected: the build has 0 errors. Record the passed / failed / skipped count of each test project (`DrivingLessons.Domain.Test`, `DrivingLessons.Application.Test`). Failures must be 0.

- [ ] **Step 0.3: Test and build the client**

Run from `client\` (install first if `node_modules` is missing, see README Global Constraints):
- `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false`
- `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client`

Expected: record the number of test files and tests passed. Failures must be 0. The build succeeds; note any budget warnings as they are today.

- [ ] **Step 0.4: Report**

Write the counts from steps 0.2 and 0.3 in your report. Later tasks compare against them: each task adds tests and must never lose one.
