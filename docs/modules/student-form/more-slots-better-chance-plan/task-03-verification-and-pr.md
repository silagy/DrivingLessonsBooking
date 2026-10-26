# Task 3 of 3: Phone check in Hebrew and English, full checks, PR

> Part of [More Slots, Better Chance](README.md). Requires tasks 1 and 2 committed. No source change. Branch `more-slots-better-chance`.

**Files:**
- No source change. Local only, never staged: `.claude\launch.json` (an `api-smoke` entry added in Step 1, removed in Step 4).

**Interfaces:**
- Consumes: the client from tasks 1 and 2; the unchanged API.
- Produces: a verified branch and a PR against `main`.

Verify with `get_page_text`, `read_page`, `find`, `javascript_tool` and `read_network_requests`. Project memory: `screenshot` can hang on this PrimeNG app, so `await document.fonts.ready` first and fall back to DOM evidence.

- [ ] **Step 1: Bring up a smoke week**

Follow [US-38 task 4](../us-38-reorder-ranked-list-plan/task-04-verification-and-pr.md) Step 1, items 1-5, with the database `drivinglessons_more_slots_smoke` (in `createdb`, `dropdb` and the `api-smoke` connection string). If the roster import refuses that CSV's header row, take the header from the current `RosterCsvParser` tests. Expected: the results that step lists (`car: 201`, `week schedule: 201`, `import: 201` with one added, `publish: 204`, `link state: open`) and the `WEEK=… LINK=… PUB_ID=…` line. The smoke student is `000000018`, with every slot of the week Open.

Then `resize_window` to 360×780.

- [ ] **Step 2: The form on a phone, Hebrew then English**

1. `/s/{LINK}`, identify as `000000018`, continue to the target step. Expected: the intro reads task 1's `target.body`; the button reads `לבחירת שיעורים אפשריים`; `document.documentElement.scrollWidth <= innerWidth`.
2. Continue with target 1. Expected on the slots step: title `בחרו שיעורים אפשריים, המועדף קודם`, footer hint `יותר שיעורים אפשריים = יותר סיכוי לקבל את השיעורים`, and no text containing `שעות` or `גיבוי` (`get_page_text`).
3. Pick `ראשון · אחה״צ` only and go to review. Expected: the summary, then `.review__more-room` with `רוצים להגדיל את הסיכוי?` and its body, then the `הוספת שיעורים אפשריים` button; Submit is enabled; no horizontal overflow. This is the customer's one-slot-for-one-lesson case.
4. Press the nudge's button, add `שני · ערב` and `רביעי · אחה״צ`, return to review. Expected: three picks for target 1, no `.review__more-room`. Submit. Expected: `201` or `204` in `read_network_requests`.
5. Toggle to EN, reload, identify again (now a returning student, three picks), raise the target to 2 and go to review. Expected: `Want a better chance?` and the English body (3 < 2 + 2); the summary line reads `Rank 1 is your preferred slot. Every extra slot on the list improves your chance of getting the lessons you asked for.`; no key text anywhere.
6. Back in Hebrew at 360px on step 3's review, `await document.fonts.ready`, take one screenshot and send it with `SendUserFile` (`display: "render"`), captioned "One slot for one lesson: the new suggestion on review." If the screenshot hangs, send the `get_page_text` of `.review` instead.

- [ ] **Step 3: Full checks**

```bash
dotnet build
dotnet test
```

From `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false` and `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client`.

Expected: no new build warnings, every backend and client test passes, and the client build succeeds within its budgets.

- [ ] **Step 4: Clean up**

`preview_stop` both servers. Remove the `api-smoke` entry from `.claude\launch.json`. Run `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_more_slots_smoke`. Then `git status --short`. Expected: clean, apart from any unrelated local edit to `.claude/launch.json`.

- [ ] **Step 5: Push and open the PR** (only once your human partner approves pushing)

```bash
git push -u origin more-slots-better-chance
gh pr create --base main --title "Student form: more slots, better chance" --body "<body>"
```

The body has these parts:
- **Summary.** Customer feedback: students gave one slot for one lesson, or two for two. The student form now calls a slot שיעור אפשרי, says the number of slots doesn't have to match the number of lessons, drops the "backup" framing, and suggests adding slots on review while the list holds fewer than the target plus two and Open slots remain. The rule "picks ≥ target" is unchanged.
- **Notes.** Intent and plan in `docs/modules/student-form/`; requirements §7 and §11 and `CONTEXT.md` updated; no backend change, no migration, no new packages.
- **Test plan.** The translations and domain specs; the page specs for the suggestion; the 360px Hebrew and English checks; the full backend and client suites.
- The attribution line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

Auto-merge is disabled in this repo; your human partner merges by hand with `--merge`.
