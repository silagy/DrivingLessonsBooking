# Task 4: Tokens in the admin pages

Part of [Dark mode](README.md). Read the README's Global Constraints and Decisions 7 to 9 first.

**Files:**
- Create: `client\src\app\theme\no-hardcoded-colours.spec.ts`
- Modify: the SCSS files in the table below (line numbers are from `main` at `44e4e10`)

**Interfaces:**
- Consumes: the tokens from task 2.
- Produces: `no-hardcoded-colours.spec.ts` with a `SCANNED: readonly string[]` list of paths relative to `client\`, which task 5 widens.

- [ ] **Step 1: Write the failing guard spec**

Write `no-hardcoded-colours.spec.ts`. It walks each path in `SCANNED` (`readdirSync(path, { recursive: true })`, `.scss` files only; a path may also be a single file) and collects every line matching:

```typescript
const LITERAL = /#[0-9a-fA-F]{3,8}\b|\brgba?\(|--p-(?:sky|ocean|steel|whale|plum|primary|surface|red|green|blue|orange|yellow|gray|zinc|slate)-\d+/;
const ALLOWED = /^\s*--brand-mark-[\w-]+\s*:/;
```

Skip lines that match `ALLOWED` (Decision 10).

```typescript
const SCANNED = [
    'src/app/features/admin-shell', 'src/app/features/users', 'src/app/features/students',
    'src/app/features/publications', 'src/app/features/teachers', 'src/app/features/roster',
    'src/app/features/week-schedules', 'src/styles/_global.scss',
];

it('keeps palette steps and literal colours out of component styles', ...);
// offenders (as `path:line: text`) toEqual []
```

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/theme/no-hardcoded-colours.spec.ts`
Expected: FAIL, listing about 70 lines, all from the table below.

- [ ] **Step 3: Replace the literals**

Apply Decision 8 file by file:

| File (under `client\src\app\features\` unless noted) | Lines | Today | Token |
|---|---|---|---|
| `admin-shell\admin-shell.component.scss` | 135-137 | `sky-50 / sky-700 / sky-100` (Administrator tag) | `--app-tag-sky-bg / -fg / -border` |
| same | 141-143 | `ocean-50 / ocean-700 / ocean-100` (Teacher tag) | `--app-tag-ocean-*` |
| `admin-shell\ui\components\shell-drawer\shell-drawer.component.scss` | 23 | `red-600` (Sign out) | `--app-error` |
| same | 47-48 | `sky-50 / sky-700` (active link) | `--app-selected-bg` / `--app-brand-deep` |
| `publications\ui\components\slot-count-cell\slot-count-cell.component.scss` | 23 | `steel-200` (tappable border) | `--app-border-interactive` |
| same | 29 | `sky-500` (focus outline) | `--app-brand` |
| same | 34, 36, 40 | `sky-500 / sky-50 / sky-700` (selected) | `--app-selected-border` / `--app-selected-bg` / `--app-brand-deep` |
| `roster\ui\components\failed-rows-panel\failed-rows-panel.component.scss` | 3, 16, 17, 20, 31, 60 | `red-200, red-50, red-700, red-100, red-100, red-600` | `--app-error-border`, `-bg`, `-strong`, `-bg-strong`, `-bg-strong`, `--app-error` |
| `roster\ui\components\roster-stat-tile\roster-stat-tile.component.scss` | 26, 30, 34 | `green-600, blue-600, red-600` | `--app-success`, `--app-info`, `--app-error` |
| `roster\ui\pages\roster\roster.page.scss` | 9 | `sky-500` | `--app-brand` |
| `students\ui\components\car-flag\car-flag.component.scss` | 6, 9 | default `red-600` | `--app-error` |
| same | 15 | `sky-500` (focus) | `--app-brand` |
| same | 25 | default `red-300` (hover underline) | `--app-error-border` |
| `students\ui\components\deleted-flag\deleted-flag.component.scss` | 6, 9, 15 | `surface-400`, `surface-600`, `sky-500` | `--app-text-muted`, `--app-text-secondary`, `--app-brand` |
| `students\ui\components\student-card\student-card.component.scss` | 13-14 | `sky-50 / sky-100` (new Student card) | `--app-row-new-bg / -border` |
| same | 18 | `--app-bg-page` (Inactive card) | `--app-row-muted-bg` |
| same | 73-75 | "חדש" tag | `--app-tag-sky-*` |
| `students\ui\components\student-who-card\student-who-card.component.scss` | 67 | `red-600` | `--app-error` |
| `students\ui\components\students-empty-state\students-empty-state.component.scss` | 19 | `sky-500` | `--app-brand` |
| `students\ui\components\transmission-tag\transmission-tag.component.scss` | 14-16 | tag | `--app-tag-sky-*` |
| `students\ui\dialogs\add-student\add-student.dialog.scss` | 44, 57 | `sky-500` | `--app-brand` |
| `students\ui\dialogs\change-car\change-car.dialog.scss` | 27, 28 | `sky-500`, `#f2f7ff` (selected option) | `--app-selected-border`, `--app-selected-bg` |
| same | 72, 85 | `sky-500` | `--app-brand` |
| `students\ui\dialogs\change-teacher\change-teacher.dialog.scss` | 27, 40 | `sky-500` | `--app-brand` |
| `students\ui\pages\students\students.page.scss` | 133 | `--app-bg-page` (Inactive row) | `--app-row-muted-bg` |
| same | 141 | `sky-50` (new row) | `--app-row-new-bg` |
| same | 244-246 | "חדש" tag | `--app-tag-sky-*` |
| same | 268, 342, 350, 361 | `red-600` | `--app-error` |
| same | 358 | `red-200` | `--app-error-border` |
| `teachers\ui\components\car-card\car-card.component.scss` | 33-34 | `primary-50 / primary-800` (transmission pill) | `--app-tag-sky-bg / -fg` |
| same | 83-85 | `primary-700 / primary-50 / primary-100` (shared) | `--app-tag-sky-fg / -bg / -border` |
| `teachers\ui\components\teacher-card\teacher-card.component.scss` | 69 | `surface-100` (car chip) | `--app-bg-muted` |
| `teachers\ui\dialogs\dialog-form.scss` | 26 | `red-500` | `--app-error` |
| `teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.scss` | 56 | `red-500` | `--app-error` |
| `users\ui\components\no-other-users\no-other-users.component.scss` | 19 | `sky-500` | `--app-brand` |
| `users\ui\components\user-card\user-card.component.scss` | 14 | `--app-bg-page` (Deleted card) | `--app-row-muted-bg` |
| same | 88-90, 94-96 | role tags | `--app-tag-sky-*`, `--app-tag-ocean-*` |
| `users\ui\components\user-who-card\user-who-card.component.scss` | 53-55, 59-61 | role tags | `--app-tag-sky-*`, `--app-tag-ocean-*` |
| `users\ui\pages\users\users.page.scss` | 56 | `--app-bg-page`, if it is the Deleted row's background | `--app-row-muted-bg` |
| same | 107-109, 113-115 | role tags | `--app-tag-sky-*`, `--app-tag-ocean-*` |
| same | 147, 184 | `red-600` | `--app-error` |
| `client\src\styles\_global.scss` | 88, 92 | `sky-50 / sky-700` (card menu fix item) | `--app-selected-bg` / `--app-brand-deep` |

Change only the value. No selector, property or rule is added or removed. `students.page.scss` must stay under the 8 kB budget error.

- [ ] **Step 4: Run the guard spec**

Expected: PASS.

- [ ] **Step 5: Run all client specs and the build**

Run `ng.js test --watch=false`, then `ng.js build --project client`. Expected: all green; no new budget warning.

- [ ] **Step 6: Check for light leftovers in dark, and no change in light**

With the dev servers running, in dark (`localStorage.setItem('drivecal.theme','dark')` + reload), at 390 × 844 and 1280 × 800 in Hebrew, visit `/`, `/teachers`, `/students`, `/students/import`, `/week-schedules`, `/publications`, `/publications/history` and `/users`. Open the drawer, the user menu, a ⋮ menu and one dialog per page where one exists. On each, run this script after `await document.fonts.ready`:

```javascript
const lightFills = ['rgb(255, 255, 255)', 'rgb(251, 251, 255)', 'rgb(245, 246, 255)', 'rgb(234, 241, 255)',
    'rgb(215, 227, 255)', 'rgb(229, 251, 252)', 'rgb(254, 242, 242)', 'rgb(220, 252, 231)'];
[...document.querySelectorAll('body *')]
    .filter((el) => el.checkVisibility() && lightFills.includes(getComputedStyle(el).backgroundColor))
    .map((el) => `${el.tagName.toLowerCase()}.${[...el.classList].join('.')}: ${getComputedStyle(el).backgroundColor}`);
```

Expected: `[]`. Fix anything listed in the component that owns it. A PrimeNG part means a missing preset token (task 2's file); add it there.

Then in light, on `/users` and `/students` at 1280: the Administrator tag background is `rgb(234, 241, 255)`, the Teacher tag `rgb(229, 251, 252)`, the new-Student row `rgb(234, 241, 255)` and an error text `rgb(220, 38, 38)`, all as before.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/theme client/src/app/features client/src/styles/_global.scss
git commit -m "feat(theme): admin pages read colour tokens only"
```
