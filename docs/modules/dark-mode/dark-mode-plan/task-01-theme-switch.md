# Task 1: The theme switch

Part of [Dark mode](README.md). Read the README's Global Constraints and Decisions 1 to 3 first.

**Files:**
- Create: `client\src\app\core\theme.service.ts`, `client\src\app\core\theme.service.spec.ts`
- Create: `client\src\app\core\theme-boot.spec.ts`
- Modify: `client\src\index.html`
- Modify: `client\src\app\app.ts`, `client\src\app\app.config.ts`, `client\src\app\app.config.spec.ts`, `client\src\app\app.spec.ts` (only to stub `matchMedia`)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces, in `core\theme.service.ts`:
  - `export type ThemeMode = 'light' | 'dark' | 'system';`
  - `ThemeService` (`providedIn: 'root'`) with `readonly mode = signal<ThemeMode>(...)` (public, as `LanguageService.lang` is), `readonly isDark = computed<boolean>(...)`, and `use(mode: ThemeMode): void`.
  - Constants in the file: `STORAGE_KEY = 'drivecal.theme'`, `DARK_CLASS = 'app-dark'`, `DEVICE_QUERY = '(prefers-color-scheme: dark)'`, `STATUS_BAR = { light: '#ffffff', dark: '#1B1D22' }`.
- Task 3 binds `mode()` and calls `use()`. Task 2's colours apply under `html.app-dark`.

- [ ] **Step 1: Write the failing `ThemeService` spec**

Write `theme.service.spec.ts` in the style of `language.service.spec.ts` (zoneless `TestBed`, a `withSavedTheme(saved: string | null)` helper that installs a `Map`-backed `localStorage`, and `startService()` that injects and calls `TestBed.tick()`). Add:
- a `withDevice(dark: boolean)` helper that installs `globalThis.matchMedia` returning a fake `MediaQueryList` (`matches`, `addEventListener('change', ...)`, `removeEventListener`), with a `changeDevice(dark)` that updates `matches` and calls the listeners with `{ matches }`;
- a `beforeEach` that resets `document.documentElement.className`, `style.colorScheme`, and puts one `<meta name="theme-color" content="#ffffff">` in `document.head`.

```typescript
it('starts in System when nothing is saved', ...);
// withSavedTheme(null), withDevice(false) → mode() 'system', isDark() false

it('treats an unknown saved value as System', ...);
// 'Dark' → mode() 'system'

it('restores the saved mode', ...);
// 'dark', device light → mode() 'dark', isDark() true

it('resolves System from the device', ...);
// null, device dark → html.classList contains 'app-dark'; html.style.colorScheme 'dark'

it('saves the picked mode and applies it', ...);
// 'dark' → use('light'); tick → localStorage 'drivecal.theme' === 'light'; no 'app-dark'; colorScheme 'light'

it('follows the device when it changes, in System only', ...);
// null, device light → changeDevice(true); tick → 'app-dark' present
// → use('light'); changeDevice(true) again; tick → 'app-dark' absent

it('colours the status bar like the top bar', ...);
// 'dark' → meta theme-color content '#1B1D22'; use('light') → '#ffffff'
```

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/core/theme.service.spec.ts`
Expected: FAIL, `Cannot find module './theme.service'`.

- [ ] **Step 3: Implement `ThemeService`**

In `core\theme.service.ts`:
- `mode` is initialised from `localStorage.getItem(STORAGE_KEY)`; anything but the three values is `'system'`.
- `prefersDark` is a private signal set from `matchMedia(DEVICE_QUERY).matches` and updated by its `change` listener; remove the listener through `inject(DestroyRef).onDestroy`.
- `isDark = computed(() => mode() === 'dark' || (mode() === 'system' && prefersDark()))`.
- One `effect`: write `mode()` to storage; `document.documentElement.classList.toggle(DARK_CLASS, isDark())`; set `style.colorScheme`; set the `theme-color` meta's `content` from `STATUS_BAR` (skip when the meta is missing).
- `use(mode)` sets the signal.

- [ ] **Step 4: Run it to verify it passes**

Run the spec. Expected: PASS, 7 tests.

- [ ] **Step 5: Write the failing boot spec**

Write `theme-boot.spec.ts`. It reads `src/index.html` with `readFileSync` (as `brand-assets.spec.ts` does), takes the body of the first inline `<script>` in `<head>`, and runs it with `new Function('localStorage', 'matchMedia', body)(fakeStorage, fakeMatchMedia)` against the jsdom `document`. Reset the class, `colorScheme` and the `theme-color` meta before each test.

```typescript
it('turns the page dark before the app boots', ...);
// saved 'dark', device light → 'app-dark' on <html>, colorScheme 'dark', meta '#1B1D22'

it('follows the device when System or nothing is saved', ...);
// saved 'system' and saved null, device dark → 'app-dark'

it('stays light when Light is saved, even on a dark device', ...);
// saved 'light', device dark → no 'app-dark', colorScheme 'light', meta '#ffffff'

it('starts in System when storage throws', ...);
// getItem throws, device dark → runs without throwing; 'app-dark'

it('runs in the head, after the theme-color meta', ...);
// page.indexOf('name="theme-color"') < page.indexOf('<script>') < page.indexOf('</head>')

it('darkens the page background before the styles load', ...);
// page contains a <style> in <head> whose text matches /html\.app-dark\s*\{[^}]*color-scheme:\s*dark/
```

- [ ] **Step 6: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/core/theme-boot.spec.ts`
Expected: FAIL, no `<script>` in `index.html`.

- [ ] **Step 7: Add the boot script and style to `index.html`**

Right after `<meta name="theme-color" content="#ffffff">`, the exact code (Decision 3; it must agree with `ThemeService`):

```html
  <style>html.app-dark { color-scheme: dark; background: #121418; }</style>
  <script>
    (function () {
      var saved = null;
      try { saved = localStorage.getItem('drivecal.theme'); } catch (e) {}
      var dark = saved === 'dark' || (saved !== 'light' && matchMedia('(prefers-color-scheme: dark)').matches);
      var root = document.documentElement;
      root.classList.toggle('app-dark', dark);
      root.style.colorScheme = dark ? 'dark' : 'light';
      var bar = document.querySelector('meta[name="theme-color"]');
      if (bar) { bar.setAttribute('content', dark ? '#1B1D22' : '#ffffff'); }
    })();
  </script>
```

- [ ] **Step 8: Run the boot spec and `brand-assets.spec.ts`**

Expected: PASS. `brand-assets.spec.ts` still finds the title and favicon links.

- [ ] **Step 9: Switch PrimeNG to the class and start the service at boot**

- **Test first:** in `app.config.spec.ts`, add `it('switches PrimeNG to dark under the app-dark class', ...)`: with the same providers as the existing test, `TestBed.inject(PrimeNG).theme()?.options?.darkModeSelector` is `'.app-dark'`. Run it: FAIL (`false`).
- In `app.config.ts`, set `darkModeSelector: '.app-dark'`. Run it: PASS.
- In `app.ts`, add `private readonly theme = inject(ThemeService);` beside `language`.
- In `app.spec.ts`, stub `globalThis.matchMedia` (a fake with `matches: false`, `addEventListener` and `removeEventListener`) where it already stubs storage, if it does; otherwise in a `beforeEach`.

- [ ] **Step 10: Run all client specs and the build**

Run: `ng.js test --watch=false`, then `ng.js build --project client`.
Expected: all green; no budget warning. Nothing looks different in light yet. In the browser pane, `localStorage.setItem('drivecal.theme','dark')` + reload puts `app-dark` on `<html>` and turns PrimeNG components dark; the app's own surfaces stay light until task 2.

- [ ] **Step 11: Commit**

```bash
git add client/src/index.html client/src/app/core client/src/app/app.ts client/src/app/app.config.ts client/src/app/app.config.spec.ts client/src/app/app.spec.ts
git commit -m "feat(theme): light, dark and system modes, applied before the app boots"
```
