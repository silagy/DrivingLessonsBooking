# Task 5 of 8: PrimeNG's physical positions mirrored once — toast corner, button icons, date-picker navigation (R1–R3)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–4 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Modify: `client\src\app\app.ts` (whole file below), `client\src\app\app.spec.ts` (two specs)
- Modify: `client\src\styles\_global.scss` (append the PrimeNG RTL overrides)

**Interfaces:**
- Consumes: task 3's `LanguageService.isRtl: Signal<boolean>`.
- Produces: `<p-toast>` sits top-left in Hebrew and top-right in English. In RTL, a PrimeNG button's icon keeps its **logical** side (`iconPos="left"` = before the label in reading order, `iconPos="right"` = after it), and the date picker's previous/next buttons follow the mirrored day grid (previous on the right pointing right, next on the left pointing left). Task 6 appends its popover-arrow rule to the same `_global.scss` block. Task 8 Step 6 re-checks all three on the real screens.

**Why:** README defects R1–R3, roadmap decision 4. PrimeNG 21 deliberately keeps three things physical in RTL:
- `p-toast`'s `position` becomes inline `right: 20px` / `left: 20px`.
- Its button CSS has `.p-button-icon-right:dir(rtl) { order: -1 }` and `.p-button:not(.p-button-vertical) .p-button-icon:not(.p-button-icon-right):dir(rtl) { order: 1 }`, which put a "left" icon on the physical left, after a Hebrew label.
- Its date picker has `.p-datepicker-next-button:dir(rtl) { order: -1 }` / `.p-datepicker-prev-button:dir(rtl) { order: 1 }`, which keep next on the right while the day grid runs right to left.

The app's global styles live in `@layer app`, declared after `@layer primeng` (`app.config.ts`: `cssLayer: { name: 'primeng', order: 'primeng, app' }`), so a rule there beats PrimeNG's regardless of specificity. The overrides use the same `:dir(rtl)` selectors as the rules they undo, so a browser without `:dir()` drops both and gets the logical default (README open item 3).

- [ ] **Step 1: Write the failing toast specs**

In `client\src\app\app.spec.ts`:

1. Change the imports

```ts
import { MessageService } from 'primeng/api';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { App } from './app';
```

to

```ts
import { By } from '@angular/platform-browser';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { App } from './app';
```

2. Directly **after** the closing `});` of `it('should render the router outlet and toast host', …)`, add:

```ts

  it('puts toasts in the top-left corner in Hebrew', async () => {
    //given
    const fixture = TestBed.createComponent(App);

    //when
    await fixture.whenStable();

    //then
    const toast = fixture.debugElement.query(By.directive(Toast)).componentInstance as Toast;
    expect(toast.position).toBe('top-left');
  });

  it('puts toasts in the top-right corner in English', async () => {
    //given
    localStorage.setItem('app_lang', 'en');
    const fixture = TestBed.createComponent(App);

    //when
    await fixture.whenStable();

    //then
    const toast = fixture.debugElement.query(By.directive(Toast)).componentInstance as Toast;
    expect(toast.position).toBe('top-right');
  });
```

   The file's `beforeEach` gives every spec an empty in-memory `localStorage`, so the first spec starts in Hebrew (the default) and the second in the saved English.

- [ ] **Step 2: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. Exactly one spec fails: `puts toasts in the top-left corner in Hebrew` (`expected 'top-right' to be 'top-left'`). The English spec passes (PrimeNG's default). Every other spec passes.

- [ ] **Step 3: Bind the toast's corner to the direction**

Replace the whole of `client\src\app\app.ts` with:

```ts
import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: '<router-outlet /><p-toast [position]="toastPosition()" />',
})
export class App {
  private readonly language = inject(LanguageService);

  protected readonly toastPosition = computed(() => (this.language.isRtl() ? 'top-left' : 'top-right'));
}
```

The computed's type is the literal union `'top-left' | 'top-right'`, which `p-toast`'s `position` input accepts, so no PrimeNG type import is needed.

- [ ] **Step 4: Run the specs**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including both toast specs.

- [ ] **Step 5: Mirror the button icons and the date-picker navigation**

Append to the end of `client\src\styles\_global.scss`:

```scss

.p-button-icon-right:dir(rtl) {
  order: 1;
}

.p-button:not(.p-button-vertical) .p-button-icon:not(.p-button-icon-right):dir(rtl) {
  order: 0;
}

.p-datepicker-prev-button:dir(rtl),
.p-datepicker-next-button:dir(rtl) {
  order: 0;
}

.p-datepicker-prev-button:dir(rtl) svg,
.p-datepicker-next-button:dir(rtl) svg {
  transform: scaleX(-1);
}
```

What each rule does in Hebrew:
- Icon order goes back to PrimeNG's LTR values (`order: 1` after the label for `iconPos="right"`, `order: 0` before it otherwise). Flex then lays them out right to left, so the roster's upload icon sits to the right of `העלאת קובץ CSV` (before it, in reading order), and the login arrow sits to the left of `כניסה`. The login page already flips that arrow (`login.component.scss`, `scaleX(-1)` under `[dir='rtl']`), so it points left, the way a Hebrew reader moves forward.
- The date picker's header DOM order is previous, title, next (`primeng-datepicker.mjs`). With `order: 0` the RTL flex row puts previous on the right and next on the left, matching the day grid. Each chevron is mirrored so it points outwards, the way it does in English.

- [ ] **Step 6: Check the result in the browser (Hebrew, then English)**

1. `preview_start {name:"api"}` (the compose Postgres must be up: `docker compose up -d postgres`), then `preview_start {name:"client"}` and open `http://localhost:4200/login`. If the app lands on the dashboard instead, press `התנתקות` (sign out). The language is Hebrew (`document.documentElement.dir` → `"rtl"`); if it is not, press `עב`.
2. **Login arrow**, before signing in. Paste into `javascript_tool`:
   ```js
   const button = document.querySelector('.login__submit button');
   const icon = button.querySelector('.p-button-icon').getBoundingClientRect();
   const label = button.querySelector('.p-button-label').getBoundingClientRect();
   [icon.right <= label.left, getComputedStyle(button.querySelector('.pi-arrow-right')).transform]
   ```
   → `[true, "matrix(-1, 0, 0, 1, 0, 0)"]`: the arrow is left of `כניסה` and points left. Now sign in with the dev admin from `docs\development\running-the-project.md` (local test credentials).
3. **Upload icon** on `/roster`:
   ```js
   const upload = [...document.querySelectorAll('.p-button')].find(button => button.querySelector('.pi-upload'));
   upload.querySelector('.pi-upload').getBoundingClientRect().left >= upload.querySelector('.p-button-label').getBoundingClientRect().right
   ```
   → `true`: the icon is right of the label. Before Step 5 this was `false` (planning audit: icon at x 63, label at x 88).
4. **Date picker** on `/publications`: pick a teacher whose week is a draft, press `פרסום שבוע…`, then **click** (`computer` `left_click`, not a scripted `.click()`) the first date field so the panel opens. If the pane is not drawn on screen the panel can stay at zero size (README environment notes). The computed styles below hold either way:
   ```js
   const prev = document.querySelector('.p-datepicker-prev-button');
   const next = document.querySelector('.p-datepicker-next-button');
   [getComputedStyle(prev).order, getComputedStyle(next).order, getComputedStyle(prev.querySelector('svg')).transform,
    prev.getBoundingClientRect().left > next.getBoundingClientRect().left]
   ```
   → `["0", "0", "matrix(-1, 0, 0, 1, 0, 0)", true]` (the last value is only meaningful when the panel has a size). Press Escape to close the panel.
5. **Toast corner:** still in the publish dialog, press `העתקת קישור`. It raises a toast whether or not the pane may write to the clipboard (`הקישור הועתק.` or `לא ניתן להעתיק את הקישור.`). Then:
   ```js
   const toast = document.querySelector('.p-toast');
   [toast.style.left, toast.style.right]
   ```
   → `["20px", ""]`. Press `ביטול` to close the dialog without publishing.
6. Press `EN` and repeat 3–5, then press `Sign out` and repeat 2 on the English login page. Expected: the upload icon is left of its label, the date picker's previous is on the left (`prev.left < next.left`, `transform` → `"none"`), the toast reads `["", "20px"]`, and the login arrow is right of `Sign in` and not mirrored (`transform` → `"none"`). Press `עב` to return to Hebrew.

If any check fails, stop: fix the rule in `_global.scss`, re-run the suite and repeat this step.

- [ ] **Step 7: Run the suite and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/app.ts client/src/app/app.spec.ts client/src/styles/_global.scss
git commit -m "fix(i18n): mirror PrimeNG's toast corner, button icons and date-picker arrows in Hebrew

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
