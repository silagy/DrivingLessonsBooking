# Task 7 of 8: LTR values isolated without being misaligned; no letter-spacing on Hebrew (R6, R7)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–6 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html` (one element)
- Modify: `client\src\app\features\auth\login.component.html` (one attribute)
- Modify: `client\src\app\features\teachers\ui\components\teacher-card\teacher-card.component.scss` (`.card__email`, `.card__chip-tag`)
- Modify: `client\src\app\features\roster\ui\pages\roster\roster.page.scss` (`.roster__ltr-cell`)
- Modify: `client\src\styles\_tokens.scss` (one token)
- Modify (one `letter-spacing` line each): `…\publications-dashboard\publications-dashboard.page.scss`, `…\publications\ui\components\slot-count-cell\slot-count-cell.component.scss`, `…\student-form\ui\components\wizard-step\wizard-step.component.scss`, `…\student-form\ui\components\review-step\review-step.component.scss`, `…\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.scss`, `…\teachers\ui\components\car-card\car-card.component.scss`, `…\teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.scss`

**Interfaces:**
- Consumes: nothing new; `<html lang>` is set by `LanguageService` (`he` / `en`).
- Produces: token `--app-caps-tracking` (`1`, and `0` under `:root[lang='he']`). Every tracked uppercase label multiplies its em value by it. Task 8 Step 6 re-checks every item below on the real screens.

**Why:** README defects R6 and R7, decisions 10 and 11.
- **R6.** An LTR value inside RTL text must be isolated (`client-i18n.md`), but isolating a whole block with an LTR direction also flips what `text-align: start` means for that block. That is why the teacher's email sits on the left under a right-aligned name, and the roster's ID and phone columns sit left under right-aligned headers. `text-align: match-parent` resolves `start` against the parent's direction instead: the value lines up with its column or name, and its characters stay left-to-right. The shell's email has no isolation at all, and the login email field has no `dir`, unlike the teacher form's email field.
- **R7.** `letter-spacing` on uppercase Latin labels is typography for Latin capitals. Hebrew has no case, so the same tracking just pulls the letters of `משותף`, `שלב 1 מתוך 5` and the rest apart.

- [ ] **Step 1: Isolate the shell's email and the login email field**

1. In `client\src\app\features\admin-shell\admin-shell.component.html`, change

```html
          <span class="shell__email">{{ email }}</span>
```

to

```html
          <bdi class="shell__email">{{ email }}</bdi>
```

   `.shell__email` only sets font size and colour, which apply to a `<bdi>` the same way.

2. In `client\src\app\features\auth\login.component.html`, change

```html
        <input pInputText id="email" type="email" formControlName="email" autocomplete="username" />
```

to

```html
        <input pInputText id="email" type="email" formControlName="email" autocomplete="username" dir="ltr" />
```

   This matches the teacher form's email field (`teacher-form.dialog.html`, `dir="ltr"`) and the student form's national-ID field.

- [ ] **Step 2: Align isolated values with their neighbours**

1. In `client\src\app\features\teachers\ui\components\teacher-card\teacher-card.component.scss`, change

```scss
.card__email {
    font-size: 0.78rem;
    color: var(--app-text-secondary);
    display: block;
```

to

```scss
.card__email {
    font-size: 0.78rem;
    color: var(--app-text-secondary);
    display: block;
    text-align: match-parent;
```

2. In `client\src\app\features\roster\ui\pages\roster\roster.page.scss`, change

```scss
.roster__ltr-cell {
    direction: ltr;
    unicode-bidi: isolate;
    font-variant-numeric: tabular-nums;
}
```

to

```scss
.roster__ltr-cell {
    direction: ltr;
    unicode-bidi: isolate;
    font-variant-numeric: tabular-nums;
    text-align: match-parent;
}
```

   Component styles are not layered, so this beats PrimeNG's `td { text-align: start }` from the `primeng` layer. The teacher email keeps its ellipsis: the block is still LTR, so a long address is cut at its end.

- [ ] **Step 3: One tracking token, switched off for Hebrew**

In `client\src\styles\_tokens.scss`, change

```scss
  --app-scrim: rgba(18, 20, 24, 0.4);
}
```

to

```scss
  --app-scrim: rgba(18, 20, 24, 0.4);

  --app-caps-tracking: 1;
}

:root[lang='he'] {
  --app-caps-tracking: 0;
}
```

`LanguageService` sets `lang` on `<html>`, so the token follows the toggle with no code.

- [ ] **Step 4: Scale every tracked uppercase label by the token**

Make exactly these eight one-line edits. Each multiplies the existing value and changes nothing else:

| File (under `client\src\app\features\`) | Selector | Change |
|---|---|---|
| `publications\ui\pages\publications-dashboard\publications-dashboard.page.scss` | `.dashboard__eyebrow` | `letter-spacing: 0.1em;` → `letter-spacing: calc(0.1em * var(--app-caps-tracking));` |
| `publications\ui\components\slot-count-cell\slot-count-cell.component.scss` | `.slot-count__caption` | `letter-spacing: 0.04em;` → `letter-spacing: calc(0.04em * var(--app-caps-tracking));` |
| `student-form\ui\components\wizard-step\wizard-step.component.scss` | `.wizard-step__counter` | `letter-spacing: 0.1em;` → `letter-spacing: calc(0.1em * var(--app-caps-tracking));` |
| `student-form\ui\components\review-step\review-step.component.scss` | `.review__type` | `letter-spacing: 0.05em;` → `letter-spacing: calc(0.05em * var(--app-caps-tracking));` |
| `teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.scss` | `.assign__title` | `letter-spacing: 0.08em;` → `letter-spacing: calc(0.08em * var(--app-caps-tracking));` |
| `teachers\ui\components\teacher-card\teacher-card.component.scss` | `.card__chip-tag` | `letter-spacing: 0.04em;` → `letter-spacing: calc(0.04em * var(--app-caps-tracking));` |
| `teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.scss` | `.fleet__section` | `letter-spacing: 0.09em;` → `letter-spacing: calc(0.09em * var(--app-caps-tracking));` |
| `teachers\ui\components\car-card\car-card.component.scss` | `.car__shared` (**not** `.car__pill`) | see below |

`car-card.component.scss` has `letter-spacing: 0.07em;` twice. Change only the one in `.car__shared`:

```scss
.car__shared {
    font-size: 0.625rem;
    font-weight: 700;
    letter-spacing: 0.07em;
```

to

```scss
.car__shared {
    font-size: 0.625rem;
    font-weight: 700;
    letter-spacing: calc(0.07em * var(--app-caps-tracking));
```

`.car__pill` keeps its value: its existing `:host-context([dir='rtl'])` rule already resets letter-spacing for Hebrew and also changes its size and weight (README decision 10). The negative tracking on headings (`-0.01em`, `-0.005em`) and the national-ID input's `0.04em` (digits only, LTR) are not uppercase labels and stay.

Then run (in `client\src\app`): `grep -rn "letter-spacing: 0\.[0-9]*em;" --include=*.scss .`
Expected: exactly two lines, `identify-step.component.scss` (`0.04em`, the national-ID input) and `car-card.component.scss` (`0.07em`, `.car__pill`).

- [ ] **Step 5: Check alignment and isolation in the browser (Hebrew, then English)**

With the `api` and `client` servers running and signed in as the dev admin (task 5 Step 6.1), in Hebrew:

1. **Shell email:** `document.querySelector('.shell__email').tagName` → `"BDI"`.
2. **Login field:** on `/login` (sign out, check, sign back in): `document.querySelector('#email').dir` → `"ltr"`.
3. **Teacher email** on `/teachers`:
   ```js
   const card = document.querySelector('.card__identity');
   const name = card.querySelector('.card__name').getBoundingClientRect();
   const email = card.querySelector('.card__email');
   const range = document.createRange(); range.selectNodeContents(email);
   Math.round(name.right - range.getBoundingClientRect().right)
   ```
   → `0`: the email text ends at the same right edge as the name (both blocks span the card's identity column). Before this task the value was the column width minus the email's width.
4. **Roster cells** on `/roster` (needs at least one roster row; the dev database has them):
   ```js
   const cell = document.querySelector('.roster__table tbody tr .roster__ltr-cell');
   const text = document.createRange(); text.selectNodeContents(cell);
   [Math.round(cell.getBoundingClientRect().right - text.getBoundingClientRect().right), Math.round(parseFloat(getComputedStyle(cell).paddingRight)), getComputedStyle(cell).direction]
   ```
   → the first two numbers are equal, and the third is `"ltr"`: the national ID's text ends at the cell's padding on the right, like every other column, and its digits still read left to right.
5. **Tracking:** on `/teachers`, `getComputedStyle(document.querySelector('.fleet__section')).letterSpacing` → `"0px"` (or `"normal"`), and the same for `.car__shared` and `.card__chip-tag` if a card shows one.
6. Press `EN` and repeat 3–5 with the left edges instead: `Math.round(range.getBoundingClientRect().left - name.left)` → `0` for the email, and `text.left - cell.left` equal to `paddingLeft` for the ID cell. `.fleet__section`'s `letterSpacing` is a positive px value (`0.6875rem × 0.09` ≈ `0.99px`). Press `עב`.

If a check fails, stop: fix the style, re-run the suite and repeat this step.

- [ ] **Step 6: Run the suite and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec (no spec depends on these styles).

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/features/admin-shell/admin-shell.component.html client/src/app/features/auth/login.component.html client/src/app/features/teachers client/src/app/features/roster/ui/pages/roster/roster.page.scss client/src/app/features/publications client/src/app/features/student-form/ui/components client/src/styles/_tokens.scss
git commit -m "fix(i18n): keep isolated LTR values aligned and drop tracking from Hebrew labels

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
