# Task 3 of 7: Cars & teachers on a phone

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires tasks 1 and 2 committed. Steps 6 to 8 need the Browser pane tools.

**Files:**
- Modify: `client\src\app\features\teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.scss`
- Modify: `client\src\app\features\teachers\ui\components\car-card\car-card.component.html` / `.scss` / `.ts`
- Modify: `client\src\app\features\teachers\ui\components\teacher-card\teacher-card.component.html` / `.scss`
- Modify: `client\src\app\features\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Test: `client\src\app\features\teachers\ui\components\car-card\car-card.component.spec.ts` (new)
- Test: `client\src\app\features\teachers\ui\components\teacher-card\teacher-card.component.spec.ts` (new)

**Interfaces:**
- Consumes: `initials(name: string): string` from `client\src\app\shared\text\initials.ts`; the cards' existing `edit` / `remove` outputs; `Car`, `Teacher`, `Transmission`.
- Produces: the phone page-header pattern (stacked title block, then a `grid-template-columns: 1fr 1fr` button row with 44px buttons), which tasks 5 and 6 repeat for their headers. New markup: `.car__chip-initials`, `.car__edit--wide` / `.car__edit--compact`, `.card__edit--wide` / `.card__edit--compact`, `.card__delete`.

**Why:** frame 02 and rules 2 and 4:
- "Title and description stack full width. Action buttons sit in a 2-column row under them (`grid 1fr 1fr`), 44px tall, text on one line."
- "Car cards wrap into 3 rows: car + transmission, teachers + Assign, then Edit/Delete as 44px icon buttons."

## The phone layout, from frame 02

| Piece | At ≤ 639px |
|-------|-----------|
| `.fleet__header` | `flex-direction: column; align-items: stretch; gap: 1rem` |
| `.fleet__actions` | `display: grid; grid-template-columns: 1fr 1fr; gap: 0.625rem`. Each `p-button` stretches (`inline-size: 100%`), `min-block-size: 2.75rem`, `white-space: nowrap`. DOM order stays: Add teacher (secondary), then Add car (primary) |
| `.fleet__grid` (teachers) | `grid-template-columns: minmax(0, 1fr)` |
| `.car` | `flex-wrap: wrap; row-gap: 0.75rem; padding: 0.875rem 0.875rem 0.25rem` |
| Row 1 | icon (2.5rem square, radius 10px), `.car__identity` (`flex: 1; min-inline-size: 0`, name 1rem/600 with an ellipsis), transmission pill at inline-end |
| Row 2 | `.car__teachers` with `flex-basis: 100%`: chips 2rem tall, each with `.car__chip-initials` (1.375rem circle, `var(--app-steel-light)` / `var(--app-whale)`, 0.625rem bold), then the assign trigger |
| Row 3 | `.car__actions` with `flex-basis: 100%; justify-content: flex-end; margin-inline: -0.875rem; padding: 0.125rem 0.375rem; border-block-start: 1px solid var(--app-bg-muted)`. Shows "Shared" (when shown), `.car__edit--compact` (pencil) and the trash, both 44 × 44 |
| Hidden | `.car__edit--wide`, `.card__edit--wide` on the phone; `.car__chip-initials`, `.car__edit--compact`, `.card__edit--compact` at ≥ 640px |
| `.card` (teacher) | `padding: 0.875rem; gap: 0.75rem`; header keeps avatar, identity, then `.card__edit--compact` and `.card__delete`, both 44 × 44 |
| `.assign__trigger` | `min-block-size: 2.75rem; padding-inline: 0.625rem; font-size: 0.8125rem` |
| Body text floor (rule 7) | `.fleet__subtitle`, `.car__type`, `.car__chip`, `.card__email` and `.card__chip` become `font-size: 0.8125rem` (13px; the design's `.kd` and `.chip`) |

- [ ] **Step 1: Write the failing specs**

`car-card.component.spec.ts`: render `CarCardComponent` through a host component, with `TranslocoTestingModule` (empty `en`), `provideZonelessChangeDetection()` and `providePrimeNG()`. Use this car:

```typescript
const CAR: Car = {
    id: 'car-1',
    name: 'Sandero',
    type: 'Private',
    transmission: Transmission.manual,
    assignedTeachers: [
        { id: 't-1', name: 'Osherat Peer' },
        { id: 't-2', name: 'Ziv Peer' },
    ],
};

it('gives each assigned teacher chip their initials', async () => {
    //given
    const card = await render(CAR);

    //when
    const initials = [...card.querySelectorAll('.car__chip-initials')].map((chip) => chip.textContent?.trim());

    //then
    expect(initials).toEqual(['OP', 'ZP']);
});

it('emits edit from the compact edit button', async () => {
    //given
    const card = await render(CAR);

    //when
    card.querySelector<HTMLButtonElement>('button.car__edit--compact')!.click();

    //then
    expect(host.edits).toBe(1);
});
```

`teacher-card.component.spec.ts`, the same setup with `{ id: 't-2', name: 'Ziv Peer', contactEmail: 'ziv@drivecal.co.il' }` and `cars: []`:

```typescript
it('labels the compact edit button for screen readers', async () => {
    //given
    const card = await render(TEACHER);

    //when
    const button = card.querySelector('button.card__edit--compact');

    //then
    expect(button?.getAttribute('aria-label')).toBe('teachers.editTeacherAria');
});

it('emits edit from the compact edit button', async () => {
    //given
    const card = await render(TEACHER);

    //when
    card.querySelector<HTMLButtonElement>('button.card__edit--compact')!.click();

    //then
    expect(host.edits).toBe(1);
});
```

The host counts `(edit)` emissions in a public `edits` field.

- [ ] **Step 2: Run the specs to see them fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/teachers/ui/components/**/*.spec.ts"`
Expected: FAIL; `.car__chip-initials` and `button.car__edit--compact` don't exist.

- [ ] **Step 3: Add the key**

| Key | he | en |
|-----|----|----|
| `teachers.editTeacherAria` | `עריכת מורה` | `Edit teacher` |

- [ ] **Step 4: Change the card templates**

`car-card`:
- In the `.ts`, expose `protected readonly initialsOf = initials;`.
- Each chip becomes `<span class="car__chip"><span class="car__chip-initials" aria-hidden="true">{{ initialsOf(teacher.name) }}</span>{{ teacher.name }}</span>`.
- Today's Edit `p-button` gets `styleClass="car__edit--wide"`.
- Add after it: `<p-button styleClass="car__edit--compact" icon="pi pi-pencil" severity="secondary" text [rounded]="true" [ariaLabel]="'teachers.editCar' | transloco" (onClick)="edit.emit()" />`.
- The trash button stays and gets `[rounded]="true"`.

`teacher-card`:
- Today's Edit button gets `styleClass="card__edit--wide"`.
- Add `card__edit--compact`: pencil icon, `[ariaLabel]="'teachers.editTeacherAria' | transloco"`, emitting `edit`.
- The trash button gets `styleClass="card__delete"` and `[rounded]="true"`.

- [ ] **Step 5: Run the specs to see them pass**

Run the step 2 command.
Expected: PASS, 4 tests.

- [ ] **Step 6: Style the phone layout**

Add a `@media screen and (max-width: 639px)` block at the end of each file listed above, with the table's values. Put the ≥ 640px hiding rules (`.car__chip-initials`, `.car__edit--compact`, `.card__edit--compact`) outside the media query, and show them again inside it. Reach the PrimeNG buttons with `:host ::ng-deep .car__edit--compact.p-button` and similar.

- [ ] **Step 7: Measure the page (GREEN), with long content (Review Focus 1)**

At 390 × 844, on `/teachers`, in Hebrew:
1. Run the README's phone measurement. Expected: `pageOverflows: false`, `clippedAtInlineEdge: []`, no `fleet__actions`, `car__`, `card__` or `assign__trigger` entry in `shortTargets`, and `smallText: []`.
2. Run:

```javascript
const box = (element) => element.getBoundingClientRect();
const header = box(document.querySelector('.fleet__header'));
const buttons = [...document.querySelectorAll('.fleet__actions .p-button')].map(box);
const car = document.querySelector('app-car-card .car');
const part = (selector) => box(car.querySelector(selector));
const rtl = document.documentElement.dir === 'rtl';
({
    buttonsSideBySide: buttons.length === 2 && Math.round(buttons[0].top) === Math.round(buttons[1].top),
    buttonHeights: buttons.map((button) => Math.round(button.height)),
    buttonsFillHeader: Math.abs(buttons[0].width + buttons[1].width + 10 - header.width) <= 1,
    pillOnFirstRow: Math.abs(part('.car__pill').top - part('.car__identity').top) < 12,
    pillAtInlineEnd: rtl ? part('.car__pill').left < part('.car__identity').left : part('.car__pill').right > part('.car__identity').right,
    teachersBelowPill: part('.car__teachers').top >= part('.car__pill').bottom,
    actionsBelowTeachers: part('.car__actions').top >= part('.car__teachers').bottom,
    actionSizes: [...car.querySelectorAll('.car__actions .p-button')].filter((button) => button.offsetParent).map((button) => Math.round(box(button).height)),
});
```

   Expected: `buttonsSideBySide`, `buttonsFillHeader`, `pillOnFirstRow`, `pillAtInlineEnd`, `teachersBelowPill` and `actionsBelowTeachers` all `true`; `buttonHeights: [44, 44]`; every `actionSizes` entry is 44.
3. Long content: edit the DOM in place:
   - set the first car's `.car__name` to `טויוטה קורולה היברידית אוטומטית לימוד נהיגה`;
   - append three more `.car__chip` copies to its `.car__teachers`;
   - set the first teacher's `.card__email` to `a.very.long.contact.address@driving-school.example.co.il`.

   Then run the phone measurement again. Expected: `pageOverflows: false`; the name ends with an ellipsis (`scrollWidth > clientWidth` on `.car__name`); the chips wrap onto more lines inside the card; the email truncates. Reload to restore.
4. Switch to English and repeat 1 and 2. Expected: the same values; the pill and the action buttons now sit at the right.

Take one screenshot in Hebrew if it doesn't hang.

- [ ] **Step 8: Desktop is unchanged**

At 1280 × 900, reload `/teachers`. Expected:
- each car card is one row with the "Edit" text button;
- no chip initials or pencil buttons are visible;
- the teacher cards show "Edit" text;
- the README measurement passes.

Then `resize_window {preset: "desktop"}`.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/teachers client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(admin-mobile): cars and teachers cards stack on a phone"
```

End the commit message with the attribution trailer from the session's instructions.
