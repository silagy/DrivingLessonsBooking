# Task 5: Tokens in shared components, the Student form and the brand mark

Part of [Dark mode](README.md). Read the README's Global Constraints and Decisions 8 and 10 first.

**Files:**
- Modify: `client\src\app\theme\no-hardcoded-colours.spec.ts` (`SCANNED`)
- Modify: the SCSS files in the table below (line numbers from `main` at `44e4e10`)
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.scss`

**Interfaces:**
- Consumes: the tokens from task 2; `SCANNED` and `ALLOWED` from task 4.
- Produces: nothing new. After this task the guard covers all of `src\app` and `_global.scss`.

- [ ] **Step 1: Widen the guard and watch it fail**

Set `SCANNED = ['src/app', 'src/styles/_global.scss']`. Run: `ng.js test --watch=false --include src/app/theme/no-hardcoded-colours.spec.ts`.
Expected: FAIL, listing only lines from the table below and `brand-logo.component.scss`.

- [ ] **Step 2: Replace the literals**

| File (under `client\src\app\`) | Lines | Today | Token |
|---|---|---|---|
| `shared\components\week-grid\week-grid.component.scss` | 48 | `surface-100` (void cell) | `--app-bg-muted` |
| `shared\components\week-grid-legend\week-grid-legend.component.scss` | 11 | `surface-600` | `--app-text-secondary` |
| `shared\dialogs\dialog-form.scss` | 16, 39 | `surface-500`, `red-600` | `--app-text-secondary`, `--app-error` |
| `features\student-form\ui\components\details-step\details-step.component.scss` | 80 | `sky-700` | `--app-brand-deep` |
| `features\student-form\ui\components\pick-sheet\pick-sheet.component.scss` | 117-119 | selected `sky-500 / sky-50 / sky-700` | `--app-selected-border / -bg`, `--app-brand-deep` |
| same | 124 | `sky-500` (focus) | `--app-brand` |
| `features\student-form\ui\components\review-step\review-step.component.scss` | 62, 66 | `sky-500` (preferred item, drag preview border) | `--app-selected-border` |
| same | 163-164 | `sky-50 / sky-700` (double tag) | `--app-tag-sky-bg / -fg` |
| `features\student-form\ui\components\slot-day-list\slot-day-list.component.scss` | 72 | `sky-500` (focus) | `--app-brand` |
| same | 77, 78, 82 | selected `sky-500 / sky-50 / sky-700` | `--app-selected-border / -bg`, `--app-brand-deep` |
| `features\student-form\ui\components\slots-step\slots-step.component.scss` | 23 | `green-600` | `--app-success` |
| `features\student-form\ui\components\status-message\status-message.component.scss` | 37-39 | `red-200 / red-50 / red-600` | `--app-error-border / -bg`, `--app-error` |
| `features\student-form\ui\components\target-step\target-step.component.scss` | 38 | `sky-500` (focus) | `--app-brand` |

`publication-stat-chip`, `share-link-box`, `publication-week-heading`, `new-version-notice` and `locked-field` already read tokens only; the guard confirms it.

- [ ] **Step 3: Keep the brand mark's colours (Decision 10)**

In `brand-logo.component.scss`:
- Add to `:host`: `--brand-mark-sky: var(--p-sky-500);`, `--brand-mark-deep: var(--p-sky-700);`, `--brand-mark-soft: var(--p-sky-300);`, `--brand-mark-page: var(--p-surface-0);`.
- `.brand__calendar` and `.brand__check` read `--brand-mark-sky`; `.brand__rings` and `.brand__disc` read `--brand-mark-deep`; `.brand__hub` reads `--brand-mark-soft`; `.brand__page` reads `--brand-mark-page`.
- Split `.brand__halo, .brand__window`: `.brand__halo` keeps `var(--brand-logo-backdrop, var(--app-bg-card))`, and `.brand__window` reads `--brand-mark-page`.
- The wordmark (`.brand__name`, `.brand__accent`) and tagline stay as they are.

- [ ] **Step 4: Run the guard and all client specs, then build**

Run `ng.js test --watch=false`, then `ng.js build --project client`. Expected: all green, the guard included; `brand-assets.spec.ts` unchanged; no new budget warning.

- [ ] **Step 5: Check it in the browser**

In dark, at 390 × 844 Hebrew:
- **The sign-in page** (signed out): the logo's calendar is `rgb(0, 87, 255)`, its page and the wheel's windows `rgb(255, 255, 255)`, the halo the page colour `rgb(18, 20, 24)`. The wordmark is `rgb(169, 198, 255)` with the accent `rgb(92, 149, 255)`.
- **The top bar and drawer:** the same mark on `rgb(27, 29, 34)`.
- **Weekly prep and the dashboard grid:** void cells `rgb(31, 34, 40)` and the stripes on Unavailable cells. The legend text has ≥ 4.5:1 contrast.
- **The Student form** (`/s/{token}` of a dev Publication that is Open; if none is Open, reopen one from `/publications` on the dev database): every step, with the pick sheet open, a selected slot, the review step (preferred item, double tag) and an error message (submit with too few picks).
- On each of these, run task 4's light-leftover script. Expected: `[]`.

In light, the same pages look as before: the logo, the selected slot (`rgb(234, 241, 255)`), the status message (`rgb(254, 242, 242)`).

- [ ] **Step 6: Commit**

```bash
git add client/src/app
git commit -m "feat(theme): shared components and the Student form read colour tokens only; the brand mark keeps its colours"
```
