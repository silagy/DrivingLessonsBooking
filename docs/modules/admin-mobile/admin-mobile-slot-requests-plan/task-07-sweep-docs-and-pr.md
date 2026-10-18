# Task 7: Every admin page against the rules, docs, PR

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decision 1 first.

**Files:**
- Modify only where the sweep finds drift (record each fix in the README's As built).
- Modify: `docs\requirements.md` (§ Dashboard, around line 144)
- Modify: `docs\modules\admin-mobile\admin-mobile-slot-requests-plan\README.md` (As built)

**Interfaces:**
- Consumes: everything from tasks 1 to 6.
- Produces: the PR.

- [ ] **Step 1: Sweep every admin page at four sizes**

Run each page at 390 × 844 and 360 × 780, in Hebrew and in English, with the phone measurement after `await document.fonts.ready`:
- `/` (home) and `/students/import` (Roster import; not designed, so the overflow check only);
- `/teachers` (Cars & teachers);
- `/week-schedules`;
- `/publications` in each state the dev data has (Draft, Open, Closed, none);
- `/publications/history`;
- `/students`, including the no-match state (filter by a nonsense search);
- `/users`.

Expected for each: `pageOverflows: false`, `clippedAtInlineEdge: []`, `smallText: []`, and `shortTargets` holding only the known 42px selects.
Record a table like the pages plan's As built (`pageScrollWidth` per page, size and language).

- [ ] **Step 2: Check the request's rules that the script can't see**

Use `javascript_tool` / `read_page`, at 390px in Hebrew unless noted. Each line is pass or fix:
1. **Bar and drawer:**
   - The bar is 56px with the logo, the avatar and the menu button.
   - The drawer opens from the right in Hebrew and the left in English.
   - Nav items are 48px; the active one has a sky tint and a 3px gradient bar.
   - Language toggle, email and Sign out sit at the bottom.
2. **Headers:** the title and description are full width, then a `1fr 1fr` action grid of 44px buttons with labels on one line; a single action spans both columns (`/students` Import + Add, `/users` Add only, `/teachers`).
3. **Cards:**
   - avatar and name on top, with the status tag at the end of that row;
   - details in a `dl` with `grid-template-columns: auto 1fr`;
   - the ⋮ is 44px and comes right after the tag.
4. **Muted cards:**
   - An Inactive Student and a Deleted User have no shadow and the `--g7` background (`--app-bg-page`).
   - Their avatar, name and details are at opacity 0.55, while the tag, ⋮ and Restore are at 1.
   - Restore is a full-width 44px row.
5. **LTR values:** national ID, phone, email and share link sit in `dir="ltr"` isolates. A 40-character email and the share link wrap (`overflow-wrap: anywhere`), with no ellipsis anywhere (`text-overflow` computed `clip`).
6. **Weekly prep:**
   - full-width selects;
   - grid card padding `12px 10px`;
   - 48px cells, with only "חסום" labelled;
   - short day letters;
   - the sticky full-width publish bar.
7. **History:** cards with week + Teacher, a status tag, `dl` pairs, and a full-width action at the bottom.
8. **Students:**
   - the filter order is select, segmented thirds, search;
   - "ייבוא רשימה" with the upload icon shows in Hebrew below 640px only;
   - the "חדש" tag and tint show on a Roster-added Student;
   - the count line sits below the list;
   - no-match keeps the filters and shows a full-width Clear filters.
9. **Users:**
   - Add User is full width;
   - the "You" tag and the email sit under the name;
   - Administrator has a sky tag and Teacher an ocean tag;
   - an Administrator's Teacher shows a muted "-";
   - when you are the only User, the empty state shows and the header Add is hidden. Check this on a scratch database or by reading the spec.
10. **Publications dashboard:**
    - the teacher select at 24px, then "שבוע N" + the state tag, the week pill, and the window status;
    - Open: Extend at the end of the status line, then Refresh + Excel;
    - Closed: Reopen + Excel;
    - Draft: one full-width action;
    - Published: no row;
    - stats in 3 equal columns, with the stamp on its own line;
    - the share link wraps, with Copy;
    - the grid card at `12px 10px`, a 46px label column and 4px gaps;
    - the legend hint and "אין שיעורים" show;
    - a count cell opens the sheet (task 5).

Fix any drift in the component that owns it, re-run that page's measurement and note it under As built.

- [ ] **Step 3: Check desktop at 1280px**

Expected: every page looks as before, except:
- the legend reads "אין שיעורים (...)" / "No lessons (...)", and the dashboard's first legend item is the hint;
- dashboard count cells with requests have the stronger border and pointer;
- clicking one opens the centred dialog.

Check the table ⋮ menus and flags as in task 6 step 6.5.

- [ ] **Step 4: Run all suites and the build**

Run from the repo root: `dotnet test`. From `client\`: `ng.js test --watch=false`, then `ng.js build --project client`.
Expected: all green; no new budget warning; `source-text.spec.ts` passes (no long dashes or ellipsis characters in the new copy).

- [ ] **Step 5: Update the docs**

- **`docs\requirements.md` § Dashboard:** after "the summary grid (day × slot) shows the current request count per slot", add a sentence: selecting a count with requests lists the Students who requested that slot, in submission order, with which of their picks it was. The list follows the same page-load rule as the counts.
- **README As built:** where the code differs from the tasks and why, the sweep table, and anything not verified in the browser (for example, no Teacher-role sign-in in the dev data).

- [ ] **Step 6: Commit, push, open the PR**

```bash
git add -A
git commit -m "docs(admin-mobile): slot requests detail and the phone rules as built"
git push -u origin admin-mobile-slot-requests
```

Open the PR to `main` with `gh pr create`. In the body:
- items A and B;
- the rule 4 and rule 7 changes;
- the sweep table;
- the decisions to look at in review: 3, 4, 7, 14, 15.

Then follow the app's PR flow (`get_status` / `bind_pr`). Auto-merge stays off; merge by hand with `--merge` once it's reviewed and green.
