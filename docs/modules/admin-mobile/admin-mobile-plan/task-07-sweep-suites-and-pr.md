# Task 7 of 7: Every admin page at phone width, the suites, and the PR

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires tasks 1 to 6 committed. Steps 1 and 2 need the Browser pane tools.

**Files:**
- No planned changes. A defect found here in tasks 1 to 6's work gets its own commit, `fix(admin-mobile): <what>`, before the PR. An undesigned page that still overflows is recorded, not fixed (README decision 11).

**Interfaces:**
- Consumes:
  - every class tasks 1 to 6 produced;
  - the README's phone measurement;
  - each task's RED/GREEN numbers and screenshots.
- Produces: the PR into `main`.

- [ ] **Step 1: Sweep every Administrator page**

Sign in as the Administrator. For each width (390 × 844, then 360 × 780) and each language (Hebrew, then English), open every page through the drawer:
- `/`
- `/teachers`
- `/students`
- `/week-schedules` (with a teacher and week chosen)
- `/publications` (with a week chosen)
- `/publications/history`
- `/users`

Reload after each resize. On each page, run the README's phone measurement.

Expected on the designed pages (`/teachers`, `/week-schedules`, `/publications/history`) and on the shell of every page:
- `pageOverflows: false` and `clippedAtInlineEdge: []`
- no `shell__`, `fleet__`, `car__`, `card__`, `weekly-prep__` or `history-card__` entry in `shortTargets`
- `smallText: []` on the three designed pages

On `/`, `/students`, `/publications` and `/users`, record `pageOverflows`, `pageScrollWidth` and the first `clippedAtInlineEdge` entries in a table. Don't fix them here.

- [ ] **Step 2: Desktop and tablet regression pass**

At 1280 × 900 and 768 × 1024, in Hebrew, open the seven pages. On each, run the README's phone measurement for `pageOverflows`, and check that `button.shell__menu-button` has `display: none`. Expected: `pageOverflows: false` everywhere, the same as before this branch (the #106 numbers). Then `resize_window {preset: "desktop"}` and stop both servers (`preview_stop`).

- [ ] **Step 3: Client suite and production build**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected:
- Every spec passes, including the new ones (shell 7, car card 2, teacher card 2, window times 2 more, week grid 2 more, weekly prep 3, history 4) and `core/source-text.spec.ts`.
- The build succeeds with no new warnings. A component style budget warning for any file this branch touched counts as new: trim the SCSS, don't raise the budget.

The backend is untouched, so `dotnet test` isn't needed. CI runs it on the PR anyway.

- [ ] **Step 4: Push and open the PR**

```bash
git push -u origin admin-mobile
```

Open the PR with `gh pr create --repo silagy/DrivingLessonsBooking --base main --head admin-mobile`. Title: `Admin on a phone: drawer menu, stacked cards, compact week grid`. The body has:
- **Summary:** one bullet per task 1 to 6.
- **Design:** `Driving Lesson Mockup\Admin Mobile.html`, frames 01 to 05. List the deviations: README decisions 3, 5, 8 and 9.
- **Before / after:** the RED numbers (dialog width from task 2) and the sweep table from step 1 for the designed pages, at 390 and 360, he and en. Add the screenshots taken in tasks 1, 3 and 6.
- **Not designed yet:** the step 1 table for `/`, `/students`, `/publications` and `/users`.
- **Edge cases checked:** README Review Focus 1 to 5, each with its result. Say whether task 5 step 7 ran or was skipped for lack of a Teacher-role User.
- The PR-description attribution line from the session's instructions, at the end.

After it opens, follow the session's PR instructions: bind the PR, read its CI status, and offer Auto-fix. Merge by hand with `--merge` once it's reviewed and green (memory: auto-merge is disabled).

- [ ] **Step 5: Propose the follow-ups**

For each undesigned page that overflowed in step 1, propose one follow-up session (`start_session`, `own_initiative`). Its brief names:
- the page and its measured `pageScrollWidth` at 390px;
- that a Claude Design phone frame should come first, following `Admin Mobile.html`'s rules 1 to 7;
- the 639px breakpoint convention from this plan.

If none overflowed, say so in the PR and propose nothing.
