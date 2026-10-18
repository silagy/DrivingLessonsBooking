# Task 6: Plum, card ⋮ menus (rule 4) and card flags (rule 7)

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 14 to 16 first. Design: frame **06 Students**, `.pop` (the open menu), `.flag`, and rule 4 / rule 7 text in frames 05 and 09.

**Files:**
- Modify: `client\src\app\theme\app-preset.ts` (`primitive.plum`), `client\src\styles\_tokens.scss` (`--app-plum`)
- Create: `client\src\app\features\students\ui\pages\students\student-card-actions.ts` + `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts` / `.html`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.ts` / `.html`
- Modify: `client\src\styles\_global.scss` (`card-menu`)
- Modify: `client\src\app\features\students\ui\components\car-flag\car-flag.component.scss`, `deleted-flag\deleted-flag.component.scss`, `student-card\student-card.component.scss`
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes: `StudentRow` (`isActive`, `isCarOfTeacher`), and the existing page commands `onEditDetails`, `onChangeTeacher`, `onChangeCar`, `onDeactivate` and `store.reactivate`.
- Produces:
  - `interface StudentCardCommands { editDetails(): void; changeTeacher(): void; changeCar(): void; deactivate(): void; reactivate(): void; }`
  - `studentCardActions(student: StudentRow, translate: (key: string) => string, commands: StudentCardCommands): MenuItem[]`
  - `StudentsPage.onCardActions(event: Event, student: StudentRow): void` and `UsersPage.onCardActions(event: Event, user: User): void`
  - Global classes: `card-menu` (on the `p-menu`), `card-menu__item--fix`, `card-menu__item--destructive`.
  - Key `students.actions.fixCar`: he "תיקון הרכב", en "Fix the car".
  - Token `--app-plum: var(--p-plum-500)`, from `primitive.plum = { 500: '#BA0081' }`.
  - Flag custom properties `--flag-color`, `--flag-font-size`, `--flag-white-space`, `--flag-underline`.

- [ ] **Step 1: Write the failing spec**

`student-card-actions.spec.ts`:
- `translate = (key) => key`;
- `commands` built from `vi.fn()`s;
- `StudentRow` fixtures in the style of `student-card.component.spec.ts`.
- `labels(items)` maps each item to `item.separator ? '---' : item.label`.

```typescript
    it('puts fixing the car first when the car is not the Teacher\'s', () => {
        // active, isCarOfTeacher false
        expect(labels(items)).toEqual([
            'students.actions.fixCar', '---',
            'students.actions.editDetails', 'students.actions.changeTeacher', 'students.actions.changeCar', '---',
            'students.actions.deactivate',
        ]);
        // items[0].styleClass 'card-menu__item--fix'; items[0].command() calls commands.changeCar
    });

    it('offers no fix when the car is the Teacher\'s', () => { ... });
    // active, isCarOfTeacher true → [editDetails, changeTeacher, changeCar, '---', deactivate]

    it('shows deactivating as destructive', () => { ... });
    // the deactivate item's styleClass 'card-menu__item--destructive'; command calls commands.deactivate

    it('offers editing and reactivating an Inactive Student', () => { ... });
    // inactive → [editDetails, reactivate]; reactivate styleClass 'students-menu__item--mirrored-icon'; command calls commands.reactivate
```

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/features/students/ui/pages/students/student-card-actions.spec.ts`
Expected: FAIL. The module doesn't exist.

- [ ] **Step 3: Implement the card menus**

- **`studentCardActions`:** a pure function with the icons the page uses today; the fix item gets `pi pi-exclamation-circle`.
- **`StudentsPage.onCardActions`:** sets `rowActions` to `studentCardActions(student, (key) => this.transloco.translate(key), {...})` and toggles `rowMenu`.
- **`UsersPage`:** refactor `onRowActions` into `private userActions(user: User, destructiveClass: string): MenuItem[]`. `onRowActions` passes `'users-menu__item--danger'`; `onCardActions` passes `'card-menu__item--destructive'`.
- **Templates:** the card's `(actionsRequested)` calls `onCardActions`, and the table's ⋮ keeps `onRowActions`. Both `p-menu`s get `styleClass="card-menu"`.
- **`_global.scss`:**
  - `.card-menu__item--fix .p-menu-item-content` gets `background: var(--p-sky-50)`, and its link, icon and label get `color: var(--p-sky-700)` and `font-weight: 600`.
  - `.card-menu__item--destructive`'s link, icon and label get `color: var(--app-plum)`.
  - Inside the 639px block, `.card-menu .p-menu-item-link` gets `min-block-size: 2.75rem`.
- **Key:** add `students.actions.fixCar`.

Run the spec. Expected: PASS.

- [ ] **Step 4: Add Plum and the card flags**

- **Preset and token:** add `plum: { 500: '#BA0081' }` to `primitive` after `whale`, and add `--app-plum: var(--p-plum-500);` to `_tokens.scss` after `--app-whale`.
- **`car-flag`:**
  - `color: var(--flag-color, var(--p-red-600))`
  - `font-size: var(--flag-font-size, 0.78rem)`
  - `white-space: var(--flag-white-space, nowrap)`
  - `border-block-end-color: var(--flag-underline, var(--p-red-600))`
  - `.car-flag--muted` uses `var(--flag-underline, var(--p-red-300))`.
- **`deleted-flag`:** the same, with its own defaults (`--p-surface-600`, `--p-surface-400`).
- **`student-card` `:host`:** sets `--flag-color: var(--app-plum)`, `--flag-font-size: 0.8125rem`, `--flag-white-space: normal` and `--flag-underline: transparent`. The flag sits inline after the value it describes, as it does in the card today; check that it does.
- If the user card shows a deleted-teacher flag (`rg "app-deleted-flag" client/src/app/features/users`), set the same four properties on `user-card`'s `:host`.

- [ ] **Step 5: Run the suite and the build**

Run `ng.js test --watch=false` and `ng.js build --project client`.
Expected: PASS, and no new style budget warning (`students.page.scss` isn't touched).

- [ ] **Step 6: Check it in the browser pane**

On `/students` at 390px, Hebrew, with a Student whose car isn't their Teacher's (decision #23 flag; the earlier plan's smoke data has one):
1. Tap their card's ⋮.
   Expected:
   - the menu opens at the button;
   - "תיקון הרכב" is first, tinted sky, above a divider;
   - Edit, Change teacher and Change car follow, then a divider, then a Plum "סימון כלא פעיל";
   - every item is ≥ 44px tall (`getBoundingClientRect().height` on `.card-menu .p-menu-item-link`).
2. "תיקון הרכב" opens the Change car dialog.
3. The card's car flag is Plum, 13px, has no underline, and wraps at 360px. Measure `getComputedStyle` color `rgb(186, 0, 129)`, font size `13px` and white-space `normal`.
4. `/users`: an active User's card ⋮ shows Delete in Plum, with 44px items.
5. At 1280px, the table's ⋮ menus are exactly today's (Change car with the "תיקון" badge in place, red Deactivate and Delete, default item height), and the table's flags are red/gray, 12.5px, `nowrap`, underlined.
6. Run the phone measurement on `/students` and `/users` at 360px in Hebrew and English. Expected: pass.

- [ ] **Step 7: Commit**

```bash
git add client/src client/public/i18n
git commit -m "feat(admin-mobile): card menus put the fix first and mark destructive items Plum; card flags are Plum and wrap"
```
