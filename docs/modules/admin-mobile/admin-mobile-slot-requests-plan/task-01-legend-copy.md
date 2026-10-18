# Task 1: "No lessons" and the dashboard legend hint

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 12 and 13 first.

**Files:**
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Modify: `client\src\app\shared\components\week-grid-legend\week-grid-legend.component.ts` / `.html`
- Create: `client\src\app\shared\components\week-grid-legend\week-grid-legend.component.spec.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` (both `<app-week-grid-legend>`)

**Interfaces:**
- Consumes: nothing.
- Produces: `WeekGridLegendComponent.firstLabelKey = input('weekGrid.legend.open')`. Keys:
  - `weekGrid.legend.noLessons`: he "אין שיעורים (שישי אחה״צ/ערב, שבת)", en "No lessons (Fri PM, Saturday)". It replaces `weekGrid.legend.outsideGrid`, which is deleted from both files.
  - `publications.dashboard.legendRequests`: he "מספר בקשות - הקישו לרשימת התלמידים", en "Requests - tap or click a count to see the students".

- [ ] **Step 1: Write the failing spec**

Write `week-grid-legend.component.spec.ts` in the style of `features\students\ui\components\car-flag\car-flag.component.spec.ts`: zoneless, `TranslocoTestingModule.forRoot({ langs: { en: {} }, translocoConfig: { availableLangs: ['en'], defaultLang: 'en' } })`, so text renders as the key. Add `describe('WeekGridLegendComponent', ...)`:

```typescript
    it('says the open label first by default', async () => { ... });
    // first .week-grid-legend__item text contains 'weekGrid.legend.open'

    it('says the label it is given first', async () => { ... });
    // setInput('firstLabelKey', 'publications.dashboard.legendRequests') → first item contains that key

    it('names the slots that never have lessons', async () => { ... });
    // last item contains 'weekGrid.legend.noLessons' and not 'outsideGrid'
```

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/shared/components/week-grid-legend/week-grid-legend.component.spec.ts`
Expected: FAIL. `firstLabelKey` isn't an input, and the last item says `weekGrid.legend.outsideGrid`.

- [ ] **Step 3: Implement**

- Add the input and use it in the first item.
- Point the last item at `weekGrid.legend.noLessons`.
- Rename the key in both JSON files. Hebrew is "אין שיעורים (שישי אחה״צ/ערב, שבת)" and English is "No lessons (Fri PM, Saturday)"; the parenthesis is unchanged.
- Add `publications.dashboard.legendRequests` next to `publications.dashboard.requests`.
- In the dashboard template, give both `<app-week-grid-legend class="dashboard__legend">` `firstLabelKey="publications.dashboard.legendRequests"`.

- [ ] **Step 4: Run the spec, then grep for leftovers**

Run the spec again. Expected: PASS.
Then run `rg -n "outsideGrid|מחוץ לרשת|Outside grid" client\src client\public src docs\requirements.md CONTEXT.md`. Expected: no matches. The earlier plans' docs may keep the old words; they record history.

- [ ] **Step 5: Check the hint fits on a phone**

With the dev servers running, open `/publications` on an Open or Closed week at 360px, in Hebrew and in English, and run the phone measurement.
Expected:
- `pageOverflows: false`;
- the legend wraps inside the grid card, with no item clipped;
- `/week-schedules`'s legend still says "פתוח" / "Open" and "אין שיעורים (...)" / "No lessons (...)".

- [ ] **Step 6: Commit**

```bash
git add client/public/i18n/he.json client/public/i18n/en.json client/src/app/shared/components/week-grid-legend client/src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.html
git commit -m "feat(publications): call Friday's closed slots \"No lessons\" and hint that counts open the student list"
```
