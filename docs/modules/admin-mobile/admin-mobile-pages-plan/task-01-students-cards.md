# Task 1 of 4: Students become cards on a phone

> Part of [Students, Users and Publications on a phone](README.md). Work on branch `admin-mobile-more-pages`. Steps 6 and 7 need the Browser pane tools.

**Files:**
- Create: `client\src\app\features\students\ui\components\student-card\student-card.component.ts` / `.html` / `.scss` / `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.html` / `.scss` / `.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`students.importRosterShort`)

**Interfaces:**
- `StudentCardComponent` (`app-student-card`): inputs `student: StudentRow` (required), `disabled: boolean` (default `false`); output `actionsRequested: Event`.
- Root `article.student-card`, modifiers `student-card--new` (`isNew`) and `student-card--inactive` (`!isActive`). Pieces: `.student-card__top` (avatar, `.student-card__identity` with `.student-card__name` and the New tag, the status `p-tag`, the ⋮ `p-button` with `styleClass="student-card__actions"`), then `dl.student-card__details` (national ID, phone, teacher + deleted flag, car + transmission tag + car marker).
- The page: `ul.students__cards` with one `li` per `store.visibleStudents()` row; `ng-template #noMatch` used by the table's `emptymessage` and by the phone list; `students__card--list` on the section while it holds the list.

**Why:** frames 06 and 06b, rules 2, 3, 8, 9, 11, 12, 13 and 18.

## The card, from frame 06

| Piece | Value |
|-------|-------|
| Card | `background: var(--app-bg-card); border: 1px solid var(--app-border); border-radius: 14px; box-shadow: var(--app-shadow-card); padding: 0.875rem 0.875rem 1.25rem; display: flex; flex-direction: column; gap: 0.625rem` |
| New | `background: var(--p-sky-50); border-color: var(--p-sky-100)` |
| Inactive | `background: var(--app-bg-page); box-shadow: none`; avatar, identity and details `opacity: 0.55`; status tag and ⋮ full strength |
| Top row | flex, `align-items: center; gap: 0.75rem`. Avatar 40px (`0.875rem`, 700). Identity `flex: 1; min-inline-size: 0`; name 1rem, 600, `overflow-wrap: anywhere`, wrapping with the New tag (`gap: 0.25rem 0.5rem`). Status tag `flex: none` (tags as the page's `.students__tag`). ⋮ 44px, `margin: -0.5rem -0.625rem -0.5rem -0.5rem` |
| Details | `display: grid; grid-template-columns: auto 1fr; gap: 0.375rem 0.875rem; margin: 0; font-size: 0.84375rem`; `dt` secondary; `dd` `margin: 0; display: flex; flex-wrap: wrap; align-items: center; gap: 0.25rem 0.5rem; min-inline-size: 0`. ID and phone in `<bdi dir="ltr">` with tabular numbers. Flags 0.8125rem (README decision 13) |

- [ ] **Step 1: Write the failing spec**

Create `student-card.component.spec.ts` in the style of `car-flag.component.spec.ts` (zoneless, `TranslocoTestingModule` with empty `en`, `providePrimeNG()`), with a `STUDENT: StudentRow` fixture (active, not new, `carMarker: null`):

```typescript
it('shows the name, national ID, phone, teacher and car', async () => { ... });            // textContent contains each
it('marks a new Student with the New tag and the tinted card', async () => { ... });      // .student-card--new, 'students.new'
it('mutes an Inactive Student but not its status or actions', async () => { ... });      // .student-card--inactive, tag 'students.status.inactive', ⋮ enabled
it('flags a Car that is not the Teacher\'s', async () => { ... });                         // carMarker notOfTeacher → app-car-flag
it('flags a deleted Teacher', async () => { ... });                                       // isTeacherDeleted → app-deleted-flag
it('asks for the row menu from its button', async () => { ... });                          // click button.student-card__actions → actionsRequested emits an Event
it('names the menu button after the Student', async () => { ... });                       // aria-label 'students.actions.menuFor'
it('disables the menu button while a change is saving', async () => { ... });              // disabled input → button disabled
```

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/components/student-card/student-card.component.spec.ts`
Expected: FAIL; the component doesn't exist.

- [ ] **Step 2: Build the card**

Create the component (OnPush, imports `TranslocoPipe`, `InitialsPipe`, `ButtonModule`, `TagModule`, `TransmissionTagComponent`, `CarFlagComponent`, `DeletedFlagComponent`) with the table's expressions for the tags, flags and `@switch (student().carMarker)`. Style it per the table above.

Run the step 1 command. Expected: PASS, 8 tests.

- [ ] **Step 3: Add the list to the page**

In `students.page.html`:
- Move the table's no-match block into `<ng-template #noMatch>`; the `emptymessage` row renders it with `ngTemplateOutlet`.
- After `p-menu`, add `ul.students__cards`: `@for (row of store.visibleStudents(); track row.id)` an `li` holding `<app-student-card [student]="row" [disabled]="store.isMutating()" (actionsRequested)="onRowActions($event, row)" />`, and `@empty` an `li.students__no-match` with the same template.
- Wrap the table in `div.students__table-wrap`.
- Bind `[class.students__card--list]` on the section to a new `protected readonly showsList = computed(() => !this.store.isLoading() && !this.store.loadError() && !this.store.isEmpty())`.
- Header: next to the import link, a second import link with `students.importRosterShort` and the upload icon; the long one gets `students__wide-only-link`, the short one `students__phone-only-link`.

Add `students.importRosterShort` to `he.json` ("ייבוא רשימה") and `en.json` ("Import Roster").

- [ ] **Step 4: Style the phone**

In `students.page.scss`, `.students__cards` and `.students__phone-only-link` are `display: none`. At the end, a `@media screen and (max-width: 639px)` block:
- Header `flex-direction: column; align-items: stretch; gap: 1rem`; `.students__actions` `display: grid; grid-template-columns: 1fr 1fr; gap: 0.625rem`; its buttons and links `inline-size: 100%; block-size: 2.75rem; white-space: nowrap; justify-content: center`.
- Wide-only link hidden, phone-only link shown.
- Status toggle buttons `block-size: 2.75rem`; the select button's grid `repeat(3, minmax(0, 1fr))`.
- `.students__table-wrap` hidden; `.students__cards` `display: flex; flex-direction: column; gap: 0.625rem; list-style: none; margin: 0; padding: 0`.
- `.students__card--list` `background: none; border: 0; box-shadow: none; border-radius: 0; overflow: visible`.
- `.students__footer` centered, 0.8125rem, `padding-block: 1rem 0`.
- `.students__no-match` is a card (background, border, 14px radius, shadow); its Clear filters button `inline-size: 100%; block-size: 2.75rem`.

Keep the additions small: the page's styles must stay under the 8 kB error budget.

- [ ] **Step 5: Run the client suite**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false`
Expected: all green.

- [ ] **Step 6: Measure the page (GREEN)**

At 390 × 844 on `/students`, Hebrew, run the README's phone measurement. Expected: `pageOverflows: false`, `clippedAtInlineEdge: []`, `smallText: []`, `shortTargets` only the 42px teacher select; `.students__table-wrap` `display: none`; card count equals the footer count. Repeat at 360 × 780 and in English.

- [ ] **Step 7: Edge cases and actions**

1. Open a card's ⋮. Expected: the row menu opens at the button with today's items.
2. An Inactive card: avatar, name and details at 0.55 opacity, status tag and ⋮ at 1.
3. Filter to no match (search "zzzz"). Expected: the no-match card with a full-width 44px Clear filters; tapping it restores the list.
4. Temporarily give a card a 40-character name and a flag in devtools (`javascript_tool`, text only). Expected: it wraps; no overflow.
5. Desktop: 1280 × 900. Expected: the table as today, `.students__cards` `display: none`, the long import label. Then `resize_window {preset: "desktop"}`.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/students client/public/i18n
git commit -m "feat(admin-mobile): students show cards on a phone"
```

End the commit message with the attribution trailer from the session's instructions.
