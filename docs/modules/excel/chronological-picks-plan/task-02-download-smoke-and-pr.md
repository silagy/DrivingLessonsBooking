# Task 2 of 2: API smoke, full checks, PR

> Part of [Chronological Picks](README.md). Requires task 1 committed. No source change in this task. Branch `excel-chronological-picks`.

**Files:**
- Local only, never staged: `.claude\launch.json` (an `api-smoke` entry, added in Step 1, removed in Step 5), `.superpowers\sdd\chronological-smoke\` (git-ignored smoke files)

**Interfaces:**
- Consumes: task 1's sheet and query, through `GET api/publications/{id}/excel?teacherId=…`.
- Produces: `$SMOKE/cohen.xlsx`, a verified branch and a PR against `main`.

This proves what the unit tests cannot: that EF Core still translates the projection with `submission.Id`, and that a rank that disagrees with the calendar, a Double, a slot blocked after picking and a soft-deleted car all come out as the README promises.

- [ ] **Step 1: Set up the smoke week**

Follow the [slice-4 smoke](../detail-sheet-feedback-plan/task-02-download-smoke.md) Step 1, with the database `drivinglessons_chronological_smoke` (in `createdb`, `dropdb` and the `api-smoke` connection string) and `SMOKE=.superpowers/sdd/chronological-smoke`. Expected: the results that step lists (`import roster.csv: 201` with 4 added, `publish: 204`, `link state: open`).

- [ ] **Step 2: Submit against the calendar, block a slot, delete a car, read Cohen's file**

Use the slice-4 smoke's Step 2 helpers, including its `submit` that posts `-d @body.json` from inside `$SMOKE`. A's picks go in **rank order Friday first**, so the ranks disagree with the calendar:
- A (`000000018`): target 2, picks `cohen 21|double|רק אחרי 16:00`, `cohen 4|single`, `cohen 0|single`
- E (`000000067`): target 1, picks `cohen 3|single`, `cohen 0|single|=1+1`
- Dana (`000000034`): target 1, pick `cohen 0|double`
- mark A's Friday Noon (`cohen 21`) Unavailable; delete car `Smoke Old`
- `download cohen $COHEN_ID && rows cohen`

Expected: three submits `204`, then `204` for the block and for the delete, then `rightToLeft=true`, the header `יום | משבצת | שם התלמיד/ה | רכב | סוג שיעור | שיעור אפשרי | שיעורים מבוקשים | אילוצים`, and exactly:

```
2 | ראשון | בוקר | Smoke Student A | Smoke Auto | יחיד | 1 מתוך 3 | 2 |
3 | ראשון | בוקר | Smoke Student E | Smoke Auto | יחיד | 1 מתוך 2 | 1 | =1+1
4 | ראשון | בוקר | דנה כהן | Smoke Old | כפול | 1 מתוך 1 | 1 |
5 | ראשון | ערב | Smoke Student E | Smoke Auto | יחיד | 2 מתוך 2 | 1 |
6 | שני | בוקר | Smoke Student A | Smoke Auto | יחיד | 2 מתוך 3 | 2 |
7 | שישי | צהריים | Smoke Student A | Smoke Auto | כפול | 3 מתוך 3 | 2 | רק אחרי 16:00
```

Rows 2, 6 and 7 prove Review Focus 1, 3 and 4: A's rank-1 Friday Double, blocked after picking, is `3 מתוך 3`. Row 4 shows a deleted car keeps its name.

- [ ] **Step 3: The file, opened in Excel, by your human partner**

`SendUserFile` with `$SMOKE/cohen.xlsx`, `display: "attach"`, caption: "Smoke Excel for Smoke Cohen. Please open פירוט בקשות and check that 'שיעור אפשרי' counts each student's slots from Sunday morning, the target reads as a number, and no rank column is left." If they report a layout problem, fix it in `RequestDetailSheet.cs` behind a failing test first, then commit it separately.

- [ ] **Step 4: Full checks**

```bash
dotnet build
dotnet test
```

Expected: no new build warnings and every test passes. No client check: the client is unchanged.

- [ ] **Step 5: Clean up**

`preview_stop` the `api-smoke` server. Remove the `api-smoke` entry from `.claude\launch.json`. Run `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_chronological_smoke`. Then `git status --short`. Expected: clean, apart from any unrelated local edit to `.claude/launch.json`.

- [ ] **Step 6: Push and open the PR** (only once your human partner approves pushing)

```bash
git push -u origin excel-chronological-picks
gh pr create --base main --title "Excel: number each student's slots from Sunday morning, drop rank" --body "<body>"
```

The body has these parts:
- **Summary.** Customer feedback: rank doesn't matter to the teacher, who books the week day by day. Sheet 2 drops the rank column, a new `שיעור אפשרי` column numbers each student's slots `{position} מתוך {picks}` from Sunday morning to Friday evening, and `שיעורים מבוקשים` shows the target count as a number. Rank stays on the student form and the admin slot-requests dialog.
- **Notes.** Intent and plan in `docs/modules/excel/`; requirements §9 and the Excel roadmap updated; no client change, no API change, no migration.
- **Test plan.** The sheet tests (calendar numbering, same-name students, target as a number); the API smoke with a rank that disagrees with the calendar, a Double, a slot blocked after picking and a deleted car; the full backend suite.
- The attribution line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

Auto-merge is disabled in this repo; your human partner merges by hand with `--merge`.
