# Task 2 of 2: API smoke, the new columns from a real database

> Part of [Detail Sheet Customer Feedback](README.md). Requires task 1 committed. No source change in this task.

**Files:**
- Local only, never staged: `.claude\launch.json` (an `api-smoke` entry), `.superpowers\sdd\feedback-smoke\` (git-ignored smoke files)

**Interfaces:**
- Consumes: task 1's sheet and query, through `GET api/publications/{id}/excel?teacherId=…`.
- Produces: a running `api-smoke` server on database `drivinglessons_feedback_smoke`, an open smoke publication with its link token in `$LINK`, and `$SMOKE/cohen.xlsx`. The student-form plan's last task reuses the server and the link.

This proves what the unit tests cannot: that EF Core translates the car-name and pick-count projection, and that a soft-deleted car and a slot blocked after picking behave as the README promises.

- [ ] **Step 1: Set up the smoke week**

Follow [US-46 task 2](../us-46-excel-detail-sheet-plan/task-02-detail-query-and-generator.md) Step 8, items 1-5, with these changes:
- the database is `drivinglessons_feedback_smoke` (in `createdb` and in the `api-smoke` connection string);
- `SMOKE=.superpowers/sdd/feedback-smoke`;
- in `detail-rows.js`, the column letters are `'ABCDEFGH'`.

If the roster import refuses that CSV's header row, take the header from the current `RosterCsvParser` tests. Expected: the same results that step lists (`import roster.csv: 201` with 4 added, `publish: 204`, `link state: open`).

- [ ] **Step 2: Submit, block a slot, delete a car, read Cohen's file**

Use Step 9's `identify`, `body`, `download` and `rows` helpers. Change `submit` so that it writes the body to `$SMOKE/body.json` with `printf '%s'` and posts it with `-d @body.json` from inside `$SMOKE`, because the Hebrew constraint would arrive as `?????` from argv. Then run:
- A (`000000018`): target 2, picks `cohen 0|single`, `cohen 21|double|רק אחרי 16:00`, `cohen 4|single`
- E (`000000067`): target 1, picks `cohen 0|single|=1+1`, `cohen 3|single`
- Dana (`000000034`): target 1, pick `cohen 0|double`
- mark A's Friday Noon (`cohen 21`) Unavailable; delete car `Smoke Old`
- `download cohen $COHEN_ID && rows cohen`

Expected: three submits `204`, then `204` for the block and for the delete, then `rightToLeft=true`, the header `יום | משבצת | שם התלמיד/ה | רכב | סוג שיעור | דירוג | שיעורים מבוקשים | אילוצים`, and exactly:

```
2 | ראשון | בוקר | Smoke Student A | Smoke Auto | יחיד | 1 | 2 מתוך 3 |
3 | ראשון | בוקר | Smoke Student E | Smoke Auto | יחיד | 1 | 1 מתוך 2 | =1+1
4 | ראשון | בוקר | דנה כהן | Smoke Old | כפול | 1 | 1 מתוך 1 |
5 | ראשון | ערב | Smoke Student E | Smoke Auto | יחיד | 2 | 1 מתוך 2 |
6 | שני | בוקר | Smoke Student A | Smoke Auto | יחיד | 3 | 2 מתוך 3 |
7 | שישי | צהריים | Smoke Student A | Smoke Auto | כפול | 2 | 2 מתוך 3 | רק אחרי 16:00
```

Row 4 proves Review Focus 2 (a deleted car keeps its name). Rows 2, 6 and 7 prove Review Focus 1 (A's blocked Friday still counts in `3`).

- [ ] **Step 3: Nothing identifying is left**

Run `rows cohen | grep -c "0000000\|050-"`. Expected: `0`. Then `preview_logs` on `api-smoke` with `search: "0000000"`. Expected: no line.

- [ ] **Step 4: The file, opened in Excel, by your human partner**

`SendUserFile` with `$SMOKE/cohen.xlsx`, `display: "attach"`, caption: "Smoke Excel for Smoke Cohen. Please open פירוט בקשות and check the car and 'שיעורים מבוקשים' columns read right, right-to-left, without resizing." Continue with the next plan while waiting. If they report a layout problem, fix it in `RequestDetailSheet.cs` behind a failing test first, then commit it separately.

Keep `api-smoke`, the smoke database and the shell variables (`API`, `AUTH`, `LINK`, `SMOKE`) for the student-form plan's last task.

---

**Next:** [the Double-counts-once plan](../../student-form/double-counts-once-plan/README.md)
