# Task 5 of 5: Verify in the app and open the PR

> Part of [#76: Publications Dashboard - Share Link While Open, Week Picker, Empty State](README.md). Sub-issue [#81](https://github.com/silagy/DrivingLessonsBooking/issues/81). Requires tasks 1–4 committed. Work on branch `76-dashboard-link-and-week`.

**Files:** none changed, unless a step below finds a defect. A defect gets its own fix commit before the PR.

**Interfaces:**
- Consumes: everything from tasks 1–4.
- Produces: the PR against `main` that closes #76–#81.

- [ ] **Step 1: Run the full client suite and the production build** (PowerShell, from `client\`):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every test passes and the build prints `Output location`. The initial-bundle budget warning already existed before this work.

- [ ] **Step 2: Walk through the dashboard in Hebrew**

Run the API (`dotnet watch` from `src\DrivingLessons.Presentation.Web`) against the compose Postgres, and the client dev server. Sign in as the dev admin and open `http://localhost:4200/publications` with no query parameters. Check each line:

- [ ] The title already shows the first teacher by name. No zero counts appear for "nobody".
- [ ] The current week, which has no publication, shows "השבוע הזה עדיין לא הוכן." and a "להכנת השבוע" button, which opens weekly prep.
- [ ] The subtitle shows the week range as a borderless picker. Its open list is in the normal font size, not the small subtitle size.
- [ ] Picking a week with an Open publication shows the stats, the share link, and the grid. "העתקת קישור" copies the link and shows the "הקישור הועתק." toast.
- [ ] A Closed week shows no share link.
- [ ] From History, opening a past week keeps that week selected in the picker, and the teacher from the link stays selected.
- [ ] Switching to English shows the same layout, mirrored, with English strings only.

- [ ] **Step 3: Push and open the PR against `main`**

```bash
git push -u origin 76-dashboard-link-and-week
gh pr create --repo silagy/DrivingLessonsBooking --base main --head 76-dashboard-link-and-week --title "Publications dashboard: share link while open, week picker, empty state"
```

The PR body lists what each task changed and includes `Closes #76`, `Closes #77`, `Closes #78`, `Closes #79`, `Closes #80`, `Closes #81`. It ends with the checked walk-through from Step 2 and the test and build results from Step 1.
