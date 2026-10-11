# Task 2 of 2: Client suite, build and the PR

> Part of [#106: The admin header fits at tablet width](README.md). Work on branch `106-admin-header-tablet`. Requires task 1 committed.

**Files:**
- No planned changes. A defect found here gets its own commit, `fix(auth): <what> (#106)`, before the PR.

**Interfaces:**
- Consumes: task 1's commit and its measurement tables (RED at 768px, GREEN for each width × language × page, the long email, the user menu, the three-link nav).
- Produces: the PR into `82-users-and-roles` that closes #106.

- [ ] **Step 1: Client suite and production build**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec passes. That includes `core/source-text.spec.ts`, which scans `.scss` for long dashes and ellipsis characters. The build succeeds with no new warnings; a component style budget warning for `admin-shell.component.scss` counts as new.

- [ ] **Step 2: Push and open the PR**

```bash
git push -u origin 106-admin-header-tablet
```

Open the PR with `gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 106-admin-header-tablet`. Title: `The admin header fits at tablet width (#106)`. The body has:
- **Summary:**
  - At ≤ 992px the bar wraps to two rows, with the nav on its own full-width row.
  - The email is capped at 12rem with an ellipsis.
  - The nav shrinks and scrolls inside the bar, never the page.
  - Say whether the 993-1199px fallback was needed, from task 1 step 5.
- **Before / after:** the RED numbers (768px Hebrew `/students`: `scrollWidth` 1022 > 768) and the GREEN table: each width × language with `pageOverflows`, `navOverflows` and `barHeight`, every page `false`. Add the screenshot from task 1 step 4 if one was taken.
- **Edge cases checked:** the long email (step 6), the user menu at 768px (step 7), and the three-link Teacher nav (step 8).
- **Decisions to look at:** README decisions 1 (two rows, not a menu), 4 (the measured fallback) and 6 (`overflow-x`).
- `Closes #106` and `Part of #82`. Note that it won't auto-close on this base, so close #106 by hand after the merge.
- The PR-description attribution line from the session's instructions, at the end.

After it opens, follow the session's PR instructions: bind the PR and read its status.
