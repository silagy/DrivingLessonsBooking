# Task 6 of 7: Publications history becomes cards on a phone

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires task 5 committed. Step 6 needs the Browser pane tools.

**Files:**
- Modify: `client\src\app\features\publications\ui\pages\publications-history\publications-history.page.html` / `.scss`
- Test: `client\src\app\features\publications\ui\pages\publications-history\publications-history.page.spec.ts` (new)

**Interfaces:**
- Consumes:
  - From `PublicationsStore`: `history()`, `historyIsLoading()`, `historyError()`, `historyIsEmpty()`, `downloadExcel(publicationId, teacherId, weekStart)`.
  - From the page: `weekLabel(row)`, `formatInstant(utcIso)`, `canRedownload(row)`, `onRedownload(row)`, `onViewDashboard(row)`, all unchanged.
  - `PublicationStateTagComponent`.
- Produces: `.history__table-wrap` (the table, wide only) and the `ul.history__cards` list. Each card is `li.history-card`, with:
  - `.history-card__week` and `.history-card__teacher`
  - `dl.history-card__details`
  - `button.history-card__action`

**Why:** frame 04 and rule 3: "History rows turn into cards: week + teacher on top, status tag at the end, details as label/value pairs, row action as a full-width link at the bottom." Labels and actions stay as today (README decision 9).

## The card, from frame 04

| Piece | Value |
|-------|-------|
| `ul.history__cards` | `list-style: none; margin: 1.25rem 0 0; padding: 0; display: flex; flex-direction: column; gap: 0.625rem` |
| `li.history-card` | `background: var(--app-bg-card); border: 1px solid var(--app-border); border-radius: 14px; box-shadow: var(--app-shadow-card); padding: 0.875rem 0.875rem 0.25rem; display: flex; flex-direction: column; gap: 0.625rem` |
| Top row | `.history-card__top`: flex, `align-items: center; gap: 0.75rem`. A block with `<bdi class="history-card__week">` (1rem, 600) over `.history-card__teacher` (0.8125rem, secondary color); then `<app-publication-state-tag>` with `margin-inline-start: auto` |
| Details | `dl.history-card__details`: `display: grid; grid-template-columns: auto 1fr; gap: 0.375rem 0.875rem; margin: 0; font-size: 0.84375rem`; `dt` secondary color, `dd` `margin: 0`. Rows: `publications.history.window` (the same `<bdi>` range or `-` as the table), `publications.history.lastExcel` (the same `v{n}` pill or `-`) |
| Action | `p-button` with `styleClass="history-card__action"`, `text`, the label `publications.history.redownload` when `canRedownload(row)`, else `publications.history.viewDashboard`. `inline-size: calc(100% + 1.75rem); margin-inline: -0.875rem; min-block-size: 2.75rem; justify-content: flex-start; padding-inline: 0.875rem; border-block-start: 1px solid var(--app-bg-muted); border-radius: 0 0 14px 14px` |
| Switch | `.history__cards` is `display: none` at ≥ 640px; `.history__table-wrap` is `display: none` at ≤ 639px |

- [ ] **Step 1: Write the failing spec**

Create `publications-history.page.spec.ts` in the style of `publications-dashboard.page.spec.ts`:
- Fake `PublicationsStore` with `history: signal(ROWS)`, `historyIsLoading: signal(false)`, `historyError: signal(null)`, `historyIsEmpty: signal(false)`, `downloadExcel: vi.fn(async () => undefined)`.
- `LanguageService` fake: `{ lang: signal('en'), locale: signal('en-IL') }`.

```typescript
const ROWS: ItemForFindPublicationHistoryResponse[] = [
    {
        publicationId: 'publication-draft', weekStart: '2026-10-18', teacherId: 'teacher-peer', teacherName: 'Ziv Peer',
        state: PublicationState.draft, windowStartUtc: null, windowEndUtc: null, latestExcelVersion: null,
    },
    {
        publicationId: 'publication-closed', weekStart: '2026-10-11', teacherId: 'teacher-peer', teacherName: 'Ziv Peer',
        state: PublicationState.closed, windowStartUtc: '2026-10-07T15:00:00Z', windowEndUtc: '2026-10-09T11:00:00Z',
        latestExcelVersion: 2,
    },
];

it('renders one card per history row with its teacher', async () => {
    //given
    const page = await render(fakeStore({}));

    //when
    const teachers = [...page.querySelectorAll('li.history-card .history-card__teacher')].map((name) => name.textContent?.trim());

    //then
    expect(teachers).toEqual(['Ziv Peer', 'Ziv Peer']);
});

it('offers the dashboard on an unclosed week and the download on a closed one', async () => {
    //given
    const page = await render(fakeStore({}));

    //when
    const actions = [...page.querySelectorAll('li.history-card .history-card__action .p-button-label')].map((label) => label.textContent?.trim());

    //then
    expect(actions).toEqual(['publications.history.viewDashboard', 'publications.history.redownload']);
});

it('downloads the closed week from its card', async () => {
    //given
    const store = fakeStore({});
    const page = await render(store);

    //when
    page.querySelectorAll<HTMLButtonElement>('li.history-card button.history-card__action')[1].click();

    //then
    expect(store.downloadExcel).toHaveBeenCalledWith('publication-closed', 'teacher-peer', '2026-10-11');
});

it('shows the latest Excel version on the card', async () => {
    //given
    const page = await render(fakeStore({}));

    //when
    const versions = [...page.querySelectorAll('li.history-card .history-card__details')].map((details) => details.textContent);

    //then
    expect(versions[1]).toContain('v2');
});
```

- [ ] **Step 2: Run the spec to see it fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/ui/pages/publications-history/publications-history.page.spec.ts`
Expected: FAIL; there's no `li.history-card`.

- [ ] **Step 3: Add the card list**

In the template's last branch:
- Wrap today's `.history__card` table container in a `div.history__table-wrap`.
- After it, add `ul.history__cards`, with one `li.history-card` per `store.history()` row (`track row.publicationId + row.teacherId`), following the card table.
- Reuse the table's expressions for the week label, the window range, the version pill and the two actions.

- [ ] **Step 4: Run the spec to see it pass**

Run the step 2 command.
Expected: PASS, 4 tests.

- [ ] **Step 5: Style the cards**

In `publications-history.page.scss`:
- Add the card rules and `.history__cards { display: none; }`.
- At the end of the file, add a `@media screen and (max-width: 639px)` block with the Switch row's values: hide the table wrap, show the cards as `flex`.
- Reach the action's PrimeNG button with `:host ::ng-deep .history-card__action.p-button`.

- [ ] **Step 6: Measure the page (GREEN)**

At 390 × 844 on `/publications/history`, in Hebrew. Run the README's phone measurement, plus:

```javascript
const box = (element) => element.getBoundingClientRect();
const card = document.querySelector('li.history-card');
const tag = box(card.querySelector('app-publication-state-tag'));
const week = box(card.querySelector('.history-card__week'));
const action = box(card.querySelector('.history-card__action'));
({
    tableShown: getComputedStyle(document.querySelector('.history__table-wrap')).display !== 'none',
    cardCount: document.querySelectorAll('li.history-card').length,
    tagAtInlineEnd: document.documentElement.dir === 'rtl' ? tag.left < week.left : tag.right > week.right,
    actionSpansCard: Math.abs(action.width - box(card).width) <= 2,
    actionHeight: Math.round(action.height),
    weekOnOneLine: Math.round(week.height) < 30,
});
```

Expected:
- `pageOverflows: false`, `smallText: []`, `tableShown: false`
- `cardCount` equals the number of history rows (`store.history().length`, or the row count at 1280px)
- `tagAtInlineEnd: true`, `actionSpansCard: true`, `actionHeight: 44`, `weekOnOneLine: true`

Then:
1. Tap a closed week's "הורדה חוזרת" (if the dev data has one). Expected: the Excel file downloads, as from the table.
2. Tap a draft's "צפייה בלוח". Expected: `/publications` opens on that teacher and week.
3. Switch to English and repeat the measurement.

Take one screenshot in Hebrew if it doesn't hang.

- [ ] **Step 7: Desktop is unchanged**

At 1280 × 900, reload. Expected: the table shows as today, and `ul.history__cards` has `display: none`. Then `resize_window {preset: "desktop"}`.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/publications/ui/pages/publications-history
git commit -m "feat(admin-mobile): publications history shows cards on a phone"
```

End the commit message with the attribution trailer from the session's instructions.
