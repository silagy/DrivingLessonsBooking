# Task 5: The slot requests sheet (phone) and dialog (desktop)

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 7 to 11 first. Design: frame **08c Slot requests sheet** (`.sheet`, `.grab`, `.shd`, `.sl2`, `.sfoot` in `Driving Lesson Mockup\Admin Mobile.html`).

**Files:**
- Create: `client\src\app\shared\dates\grid-date.ts` + `.spec.ts`
- Modify: `client\src\app\shared\components\week-grid\week-grid.component.ts` (`dateFor` uses `gridDayDate`)
- Create: `client\src\app\shared\directives\sheet-drag-close.directive.ts` + `.spec.ts`
- Create: `client\src\app\features\publications\ui\dialogs\slot-requests\slot-requests.dialog.ts` / `.html` / `.scss` + `.spec.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts` / `.html` / `.spec.ts`
- Modify: `client\src\styles\_global.scss`
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes: from task 3, the store's `openSlotRequests`, `closeSlotRequests`, `openedSlot`, `slotRequests`, `slotRequestsAreLoading`, `slotRequestsError`, `windowTimes`, `selectedWeekStart`; from task 4, the cell's `selected` input and `opened` output (an `HTMLElement`); the `InitialsPipe` from `shared\pipes\initials.pipe.ts`.
- Produces:
  - `gridDayDate(weekStart: string, day: DayOfWeek, locale: string): string`: the date of `day` in the Sunday-first week starting at `weekStart`, formatted with `toLocaleDateString(locale, { day: 'numeric', month: 'numeric' })`, as `WeekGridComponent.dateFor` does today.
  - `SheetDragCloseDirective`:
    - selector `[appSheetDragClose]`, output `dismissed = output<void>()`;
    - exported `const SHEET_DISMISS_DISTANCE_PX = 80`;
    - exported `shouldDismissSheet(distance: number): boolean`, true when `distance >= SHEET_DISMISS_DISTANCE_PX`.
  - `SlotRequestsDialog`:
    - standalone, OnPush;
    - injects `PublicationsStore`, `DynamicDialogRef` and `LanguageService`;
    - no `DynamicDialogConfig.data`, because it reads `store.openedSlot()`.
  - `PublicationsDashboardPage.onSlotOpened(slotId: string, cell: HTMLElement): void`.
  - Keys under `publications.slotRequests`:

| Key | he | en |
|-----|----|----|
| `title` | `יום {{day}} {{date}} · {{window}}` | `{{day}} {{date}} · {{window}}` |
| `requestedByOne` | `תלמיד אחד ביקש` | `1 student requested` |
| `requestedByMany` | `{{count}} תלמידים ביקשו` | `{{count}} students requested` |
| `choice` | `בחירה {{rank}} מ-{{count}}` | `Choice {{rank}} of {{count}}` |
| `none` | `אף תלמיד לא מבקש את המשבצת הזו כרגע. רעננו את הדף.` | `No student requests this slot now. Refresh the page.` |
| `loadFailed` | `לא ניתן לטעון את רשימת התלמידים.` | `The student list couldn't load.` |

The ✕ and the footer button reuse `general.close` ("סגירה" / "Close").

- [ ] **Step 1: Write the failing specs**

`grid-date.spec.ts`. 2026-10-18 is a Sunday; frame 08c's head reads "א׳ 18.10 ... ד׳ 21.10".

```typescript
    it('dates a weekday of the week in Hebrew', () => {
        expect(gridDayDate('2026-10-18', DayOfWeek.wednesday, 'he')).toBe('21.10');
    });

    it('dates a weekday of the week in English', () => {
        expect(gridDayDate('2026-10-18', DayOfWeek.wednesday, 'en')).toBe('10/21');
    });

    it('dates the Sunday the week starts on', () => {
        expect(gridDayDate('2026-10-18', DayOfWeek.sunday, 'he')).toBe('18.10');
    });
```

`sheet-drag-close.directive.spec.ts`:
- A host component renders `<div class="p-dialog"><div appSheetDragClose (dismissed)="dismissed = true"></div></div>`.
- Drive it with `new MouseEvent('pointerdown' | 'pointermove' | 'pointerup', { clientY, bubbles: true })`. jsdom has no `PointerEvent`, and the directive reads only `clientY`; stub `setPointerCapture` on the element.

```typescript
    it('closes the sheet when dragged down far enough', ...);   // 100 → 190 → up: dismissed true
    it('springs back when let go early', ...);                  // 100 → 150 → up: dismissed false; .p-dialog style.translate '' 
    it('ignores dragging up', ...);                             // 300 → 100 → up: dismissed false; translate never set
    it('moves the sheet with the finger', ...);                 // 100 → 140 (no up): .p-dialog style.translate '0px 40px'
```

`slot-requests.dialog.spec.ts`:
- Zoneless, `TranslocoTestingModule` with empty `en`, `providePrimeNG()`.
- A fake `PublicationsStore` built from writable signals: `openedSlot` = Wednesday Afternoon `slot-a` with `requestCount: 6`; `slotRequests`; `slotRequestsAreLoading`; `slotRequestsError`; `windowTimes` = `{ afternoon: '15:00-18:00' }`; `selectedWeekStart` = `'2026-10-18'`; `closeSlotRequests: vi.fn()`.
- `{ provide: DynamicDialogRef, useValue: { close: vi.fn() } }`.
- A fake `LanguageService` with `locale: signal('he')`.

```typescript
    it('titles the sheet with the day, date and window', ...);   // h2 contains 'publications.slotRequests.title'; the translate params (spy TranslocoService or render via a he lang with the real key) give day 'weekGrid.daysShort.wednesday', date '21.10', window 'weekGrid.windows.afternoon'
    it('shows the window time left to right', ...);              // bdi[dir=ltr] text '15:00-18:00'
    it('lists the students in the order it gets them', ...);     // 3 items → li .slot-requests__name texts in order; initials in .slot-requests__avatar
    it('says which of the student\'s choices this slot was', ...); // li text contains 'publications.slotRequests.choice' (params rank 2, count 3)
    it('counts the students it lists, not the cell', ...);       // cell says 6, list has 2 → subtitle uses 'requestedByMany' with count 2; list has 1 → 'requestedByOne'
    it('counts from the cell while the list loads', ...);        // loading true, list [] → 'requestedByMany' count 6; a p-progressspinner shows
    it('says nobody requests the slot now', ...);                // loaded, [] → 'publications.slotRequests.none'
    it('says the list could not load', ...);                     // error key shown
    it('closes from the cross and from the Close button', ...);  // both call ref.close()
```

For the title spec, the simplest exact check is to give `TranslocoTestingModule` an `en` with `{ "publications.slotRequests.title": "{{day}}|{{date}}|{{window}}" }`-style nested keys, and expect `"weekGrid.daysShort.wednesday|21.10|weekGrid.windows.afternoon"`. Pick whichever matches how the spec suite already checks params.

`publications-dashboard.page.spec.ts`, added to the existing `describe`, using the spec's existing store fake and a spy on the page's `DialogService` (provided in `providers`; override it with `TestBed.overrideComponent(..., { set: { providers: [{ provide: DialogService, useValue: dialogs }] } })`, where `dialogs.open` returns `{ onClose: subject }`):

```typescript
    it('opens the students of a slot from its count', ...);      // Open week, click a cell button → store.openSlotRequests('slot-a'); dialogs.open called with SlotRequestsDialog and showHeader false, styleClass 'slot-requests-dialog', maskStyleClass 'slot-requests-mask', dismissableMask true, closeOnEscape true
    it('forgets the slot when the list closes', ...);            // subject.next(undefined) → store.closeSlotRequests called
    it('gives focus back to the cell it opened from', ...);      // after close, document.activeElement is that cell's button
```

- [ ] **Step 2: Run them to verify they fail**

Run: `ng.js test --watch=false --include "src/app/shared/dates/grid-date.spec.ts" --include "src/app/shared/directives/**/*.spec.ts" --include "src/app/features/publications/**/*.spec.ts"`
Expected: FAIL. The modules don't exist yet.

- [ ] **Step 3: Implement `gridDayDate` and use it in the grid**

`WeekGridComponent.dateFor` becomes `weekStart ? gridDayDate(weekStart, day, this.language.locale()) : undefined`.
Run the grid's and the dashboard's existing specs. Expected: PASS.

- [ ] **Step 4: Implement `SheetDragCloseDirective`**

- **Host:** `style.touch-action: none`, plus `pointerdown` / `pointermove` / `pointerup` / `pointercancel` listeners.
- **On down:** record `clientY`, call `setPointerCapture`, and find `host.closest('.p-dialog')`.
- **On move:** set the dialog's `style.translate` to `` `0px ${Math.max(0, dy)}px` `` only when `dy > 0`.
- **On up:** if `shouldDismissSheet(dy)`, emit `dismissed`; otherwise clear `translate`.
- Spring-back and slide-out animation is CSS (`transition: translate 160ms ease-out` on `.slot-requests-dialog` in step 6), switched off while dragging with an `is-dragging` class.

- [ ] **Step 5: Implement `SlotRequestsDialog`**

Markup, in order:
1. `div.slot-requests__top[appSheetDragClose]` (`(dismissed)="close()"`), which holds:
   - the `span.slot-requests__grab` (`aria-hidden="true"`);
   - the header: `h2.slot-requests__title`, `p.slot-requests__subtitle` (`<bdi dir="ltr">` time, " · ", the count text), and the ✕ `p-button` (`icon="pi pi-times"`, `rounded`, `text`, `severity="secondary"`, `[ariaLabel]="'general.close' | transloco"`).
2. The body:
   - while loading, a spinner;
   - on error, `p.slot-requests__state`;
   - when the loaded list is empty, the `none` text;
   - otherwise `ul.slot-requests__list`, one `li` per item (`track item.studentId`), each holding `span.slot-requests__avatar` (initials), `span.slot-requests__name` and `span.slot-requests__choice`.
3. `div.slot-requests__footer` with a full-width secondary `p-button` (`general.close`).

The title and subtitle are `computed`s over the store and `LanguageService.locale()`.

`close()` calls `ref.close()`. The store is cleared by the page, which listens to `onClose`.

Component styles, from frame 08c:
- **Header:** title `var(--app-font-display)`, 1.25rem, weight 500; subtitle 0.8125rem `var(--app-text-secondary)`.
- **List rows:** `min-block-size: 3.5rem`, a `gap` of 0.75rem, and a 1px `var(--app-bg-muted)` separator.
- **Avatar:** 2rem, `var(--app-steel-light)` background, `var(--app-whale)` text.
- **Name:** 0.9375rem, weight 500, `overflow-wrap: anywhere`, `min-inline-size: 0`.
- **Choice:** 0.8125rem (the design's 12px is under the 13px floor), `var(--app-text-secondary)`, `margin-inline-start: auto`, `flex: none`, `white-space: nowrap`. It's a caption-like label of a few characters, the only `nowrap` text.
- **Footer:** a 1px `var(--app-border)` top border; the button `block-size: 2.75rem` and full width.
- **Grab handle:** `display: none` by default; 40×4px `var(--app-border)` with radius 4px inside the phone media query.

- [ ] **Step 6: Wire the page and the global sheet styles**

- **Template:** both grids' cells get `[selected]="store.openedSlot()?.slotId === cell.slotId"` and `(opened)="onSlotOpened(cell.slotId, $event)"`.
- **`onSlotOpened`:**
  - Keep `cell` in a private field and call `store.openSlotRequests(slotId)`.
  - Open the dialog with `{ showHeader: false, width: NARROW_DIALOG_WIDTH, modal: true, dismissableMask: true, closeOnEscape: true, styleClass: 'slot-requests-dialog', maskStyleClass: 'slot-requests-mask' }`.
  - On `onClose`, call `store.closeSlotRequests()` and focus the kept cell.
- **`_global.scss`:**
  - At all widths, `.slot-requests-dialog .p-dialog-content` gets padding 0 and no own scroll.
  - Inside the existing `@media screen and (max-width: 639px)` block:
    - `.slot-requests-mask` gets `align-items: flex-end`.
    - `.p-dialog.slot-requests-dialog` gets `min-inline-size: 100%`, `max-inline-size: 100%`, `max-block-size: 78dvh`, `margin: 0`, `border-radius: 20px 20px 0 0`, the transition, `display: flex` and `flex-direction: column`.
    - The list scrolls (`overflow-y: auto`, `min-block-size: 0`, `flex: 1`) and the footer gets `padding-block-end: max(1.25rem, env(safe-area-inset-bottom))`.
- **Keys:** add them to both JSON files.

- [ ] **Step 7: Run the specs and the build**

Run the step 2 command, then `ng.js test --watch=false` and `ng.js build --project client`.
Expected: all PASS, and the build has no new budget warning. The dialog's SCSS stays under 4 kB.

- [ ] **Step 8: Check it in the browser pane**

On `/publications`, on an Open week with requests (dev data, or picks submitted through the link in task 2), at 390px in Hebrew:
1. Tap a cell with 3 or more requests.
   Expected:
   - a bottom sheet rises from the bottom edge;
   - the cell shows the sky outline;
   - the title reads "יום ד׳ 21.10 · אחה״צ" (or the tapped day's equivalent), and the time reads left to right;
   - the names are in submission order (compare task 2's `psql` query);
   - the page doesn't scroll sideways.
2. Drag the handle down about 120px and let go: the sheet closes, the outline clears, and focus is on the cell. Drag 40px: it springs back.
3. Close it once each with ✕, Close, the scrim and Escape: each closes it, and focus returns to the cell.
4. At 360px, in Hebrew and English, rename a scratch Student to "אלכסנדרה בן-דוד רוזנטל-אברהמי" (or use the longest dev name) and open their slot. Measure with `javascript_tool`: the sheet's `scrollWidth <= clientWidth`, the `.slot-requests__choice` is on one line, and the name wraps.
5. At 1280px, a centred 28rem dialog shows with no handle, and the Weekly prep grid and the dashboard otherwise look as before.

Record the measurements in the commit body.

- [ ] **Step 9: Commit**

```bash
git add client/src client/public/i18n
git commit -m "feat(publications): tap a count to see who requested the slot, as a bottom sheet on a phone"
```
