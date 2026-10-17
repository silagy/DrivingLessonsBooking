# Task 4 of 4: Sweep, suites and build

> Part of [Students, Users and Publications on a phone](README.md). Requires task 3 committed.

**Files:**
- Modify: `docs\modules\admin-mobile\admin-mobile-plan\README.md` (the "Still overflowing" line)
- Modify: this plan's README (an "As built" section)

- [ ] **Step 1: Sweep**

On `/students`, `/users` and `/publications`, at 390 × 844 and 360 × 780, in Hebrew and English: the README's phone measurement. Record each page's `pageScrollWidth` in the "As built" section. Expected: every run passes.

- [ ] **Step 2: Desktop**

At 1280 × 900 on the three pages, in Hebrew: tables and the dashboard header as before this plan (one row, actions at inline-end), and no phone-only element visible.

- [ ] **Step 3: Suites and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests green; the build passes with no new budget warning (the existing `students.page.scss` warning and the initial-bundle warning may remain; no style file over 8 kB).

- [ ] **Step 4: Docs**

- In the parent README's "As built", replace "Still overflowing at 390px, not designed yet" with a pointer to this plan.
- Add "As built" to this README for anything that differs from the tasks.

- [ ] **Step 5: Commit**

```bash
git add docs/modules/admin-mobile
git commit -m "docs(admin-mobile): students, users and publications phone layouts as built"
```

The PR waits for `admin-mobile` to merge into `main` (README, Branch).
