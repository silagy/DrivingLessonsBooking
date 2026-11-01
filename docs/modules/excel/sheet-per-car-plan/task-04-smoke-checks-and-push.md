# Task 4 of 4: API smoke, full checks, push

> Part of [Sheet per Car](README.md). Requires task 3 committed. No source change in this task. Branch `excel-sheet-per-car`.

**Files:**
- Local only, never staged: smoke files under a git-ignored folder (`.superpowers/sdd/sheet-per-car-smoke/`) or the scratchpad.

**Interfaces:**
- Consumes: tasks 1-3 through `GET api/publications/{id}/excel?teacherId=…`.
- Produces: a smoke workbook handed to your human partner, a verified branch pushed to `origin`.

This proves what the unit tests cannot: that EF Core translates the projection with `car.Id`, and that real data lands on the right sheets.

- [ ] **Step 1: Smoke the download (when a Postgres is reachable)**

Follow the [slice-5 smoke](../chronological-picks-plan/task-02-download-smoke-and-pr.md) Steps 1-2 with the database `drivinglessons_sheet_per_car_smoke`: same roster, same picks, same blocked slot and deleted car `Smoke Old`. Read Cohen's file's sheet names and rows.

Expected: sheets `סיכום | Smoke Auto | Smoke Old`; every sheet right-to-left; `Smoke Auto` row 1 `Smoke Auto`, row 2 the seven headers, and rows 3-7 the five Smoke Auto rows of the slice-5 smoke without the car column; `Smoke Old` row 1 `Smoke Old` and one row, Dana's Sunday Morning Double `1 מתוך 1`. Five rows plus one equals the summary total of six.

If this container has no Postgres or Docker daemon, skip this step and say so plainly in the final report.

- [ ] **Step 2: The file, opened in Excel, by your human partner**

When Step 1 ran: `SendUserFile` with the smoke workbook, `display: "attach"`, caption: "Smoke Excel for Smoke Cohen: one sheet per car after סיכום. Please check the tab names, the car title row and that no car column is left." A layout problem gets a failing test first, then its own commit.

- [ ] **Step 3: Full checks**

```bash
dotnet build
dotnet test
```

Expected: no new build warnings, every test passes. No client check: the client is unchanged.

- [ ] **Step 4: Clean up**

Stop any smoke server, drop the smoke database, then `git status --short`. Expected: clean.

- [ ] **Step 5: Push**

```bash
git push -u origin excel-sheet-per-car
```

No PR unless your human partner asks for one. If they do, the body has Summary (customer request: one sheet per car after the teacher-wide summary; names made safe and unique; car title row; no car column), Notes (plan in `docs/modules/excel/sheet-per-car-plan/`; requirements §9 and the roadmap updated; no client, API or migration change), Test plan (`CarSheetNamesTest`, the sheet and generator tests, the smoke if it ran, the full backend suite) and the attribution line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
