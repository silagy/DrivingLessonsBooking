# Task 4: Count cells that open

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 6 and 15 first.

**Files:**
- Modify: `client\src\app\features\publications\ui\components\slot-count-cell\slot-count-cell.component.ts` / `.html` / `.scss`
- Create: `client\src\app\features\publications\ui\components\slot-count-cell\slot-count-cell.component.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes: `DayOfWeek` and `SlotWindow` from `shared\models`.
- Produces: on `SlotCountCellComponent`:
  - inputs `requestCount` and `slotState` (as today), plus new inputs `day = input.required<DayOfWeek>()`, `window = input.required<SlotWindow>()` and `selected = input(false)`;
  - `opened = output<HTMLElement>()`, which emits the cell's button.
  - It is openable when `slotState() === SlotState.open && requestCount() > 0`.
  - Key `publications.slotRequests.openAria`: he "{{count}} בקשות, יום {{day}}, {{window}}. הצגת התלמידים", en "{{count}} requests, {{day}}, {{window}}. Show the students". `day` comes from `weekGrid.days.*` and `window` from `weekGrid.windows.*`, both translated in the template.

- [ ] **Step 1: Write the failing spec**

Write `slot-count-cell.component.spec.ts` in the style of `car-flag.component.spec.ts` (zoneless, `TranslocoTestingModule` with empty `en`). Use a `render({ requestCount, slotState, selected? })` helper that also sets `day: DayOfWeek.wednesday` and `window: SlotWindow.afternoon`.

```typescript
    it('opens the students of a slot with requests', async () => { ... });
    // 3, open → button.slot-count exists; click → opened emits that button element

    it('does nothing for a slot nobody requested', async () => { ... });
    // 0, open → no button; .slot-count__value text '0'

    it('does nothing for a blocked slot, even with requests', async () => { ... });
    // 3, unavailable → no button; .slot-count--blocked shows 'weekGrid.legend.unavailable'

    it('names the slot for screen readers', async () => { ... });
    // button aria-label contains 'publications.slotRequests.openAria'; aria-haspopup 'dialog'

    it('marks the slot whose students are showing', async () => { ... });
    // selected true → button has .slot-count--selected and aria-expanded 'true'; false → aria-expanded 'false'
```

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/features/publications/ui/components/slot-count-cell/slot-count-cell.component.spec.ts`
Expected: FAIL. There's no button, and `day` isn't an input.

- [ ] **Step 3: Implement**

- **Template:** openable cells render `<button type="button" class="slot-count slot-count--openable">` with today's value and caption inside. Non-openable cells keep today's `div` markup unchanged.
- **Styles, at all widths:**
  - `.slot-count--openable`: `border-color: var(--p-steel-200)`, `cursor: pointer`, `font: inherit`, `inline-size: 100%`, and a `:focus-visible` outline of 2px `var(--p-sky-500)`.
  - `.slot-count--selected`: `outline: 2px solid var(--p-sky-500)` with `outline-offset: -1px`, `background: var(--p-sky-50)`, and the value's colour `var(--p-sky-700)`.
  - Keep the phone block as it is. The button inherits its 48px min-height.

- [ ] **Step 4: Run the spec**

Run the spec. Expected: PASS.

- [ ] **Step 5: Add the key and bind the new inputs**

- Add `publications.slotRequests.openAria` to both JSON files.
- In `publications-dashboard.page.html`, pass `[day]="cell.day" [window]="cell.window"` to both `<app-slot-count-cell>`. Task 5 adds `selected` and `opened`.
- Run `ng.js build --project client`. Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add client/src/app/features/publications client/public/i18n
git commit -m "feat(publications): count cells with requests are buttons"
```
