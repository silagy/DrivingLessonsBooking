# Task 4: New version notice

Part of [Installable DriveCal app](README.md). Read the README's Global Constraints and Decisions 15 to 18 first.

An installed app is resumed far more often than it's reloaded, so after a deploy it can keep running the old bundle for days: a lazy screen then fails to load (its old chunk is gone from the new image), or a renamed translation key shows raw. This task tells the User when the server has a newer version and lets them reload into it, without ever reloading on its own.

**Files:**
- Create: `client\src\app\core\pwa\app-page-api.service.ts`
- Create: `client\src\app\core\pwa\new-version.store.ts` + `.spec.ts`
- Create: `client\src\app\core\pwa\check-for-new-version.ts` + `.spec.ts`
- Create: `client\src\app\shared\components\new-version-notice\new-version-notice.component.ts` + `.scss` + `.spec.ts`
- Modify: `client\src\app\core\pwa\provide-pwa.ts` + `.spec.ts` (the `visibilitychange` trigger)
- Modify: `client\src\app\app.config.ts` (`withNavigationErrorHandler`)
- Modify: `client\src\app\app.ts` + `app.spec.ts` (render the notice)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (one key)

**Interfaces:**
- Consumes: `providePwa()` from task 3; `general.refresh` (en "Refresh", he "רענון").
- Produces:
  - `AppPageApiService` (`providedIn: 'root'`): `getAppPage(): Observable<string>`, `HttpClient.get('/index.html', { responseType: 'text' })`. `index.html` is `no-cache`, so the browser revalidates it and gets the deployed copy.
  - `NewVersionStore` (`providedIn: 'root'`):
    - `readonly isReady: Signal<boolean>`, `false` until a check finds a newer version, then `true` for the life of the page.
    - `check(): Promise<void>`: returns at once, with no request, when `isReady()` is already `true` or a check is in flight. Otherwise fetches the app page, parses it with `DOMParser`, and sets ready when the running page and the served page each have a main bundle and they differ. Never rejects: a failed request (offline, server restarting) leaves `isReady` as it was.
    - `reload(): void`: `inject(DOCUMENT).location.reload()`.
    - The main bundle of a document is `document.querySelector('script[src^="main-"]')?.getAttribute('src') ?? null`. A production build writes `<script src="main-IUEPGTXJ.js" type="module">`; `ng serve` writes `main.js`, which doesn't match, so development never shows the notice.
  - `checkForNewVersionAfterNavigationError(): void`: `void inject(NewVersionStore).check()`. Passed to `withNavigationErrorHandler(...)` in `provideRouter`, so a screen that fails to load (a missing chunk) triggers a check.
  - `providePwa()` also adds a `visibilitychange` listener on `view.document` that calls `store.check()` when `view.document.visibilityState === 'visible'` (the `NewVersionStore` is injected in the initializer).
  - `NewVersionNoticeComponent`, selector `app-new-version-notice`, standalone, OnPush. Reads `NewVersionStore.isReady()`; when `true` renders `<div class="new-version" role="status">` with the text of `pwa.newVersionReady` and `<p-button [label]="'general.refresh' | transloco" (onClick)="store.reload()" />`; when `false` renders nothing.
  - `App` renders `<app-new-version-notice />` after `<router-outlet />`, on every surface (admin, sign-in, student form).
  - Translation key `pwa.newVersionReady`: en "A new version of DriveCal is ready.", he "גרסה חדשה של דרייבקל מוכנה.".

- [ ] **Step 1: Write the failing store spec**

`new-version.store.spec.ts`, `describe('NewVersionStore', ...)`. `beforeEach`: `TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()] })`, and append `<script src="main-AAAAAAAA.js" type="module"></script>` to `document.head` (remove it in `afterEach`). Helper `servedPage(main: string): string` returns `` `<!doctype html><html><head></head><body><app-root></app-root><script src="${main}" type="module"></script></body></html>` ``.

```typescript
    it('announces a new version when the server serves another main bundle', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(true);
    });

    it('stays quiet while the server serves the running version', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-AAAAAAAA.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(false);
    });

    it('stays quiet when the server cannot be reached', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').error(new ProgressEvent('error'));

        //then
        await expect(checking).resolves.toBeUndefined();
        expect(store.isReady()).toBe(false);
    });

    it('stays quiet when a page has no fingerprinted main bundle', async () => {
        //given
        document.head.querySelector('script[src^="main-"]')?.setAttribute('src', 'main.js');
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(false);
    });

    it('stops asking the server once it has found a new version', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const http = TestBed.inject(HttpTestingController);
        const first = store.check();
        http.expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await first;

        //when
        await store.check();

        //then
        http.verify();
    });

    it('asks the server once while a check is in flight', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);

        //when
        const checks = [store.check(), store.check()];

        //then
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-AAAAAAAA.js'));
        await Promise.all(checks);
    });
```

`reload()` isn't unit-tested (jsdom can't reload); task 5, step 3 clicks it.

- [ ] **Step 2: Write the failing trigger specs**

`check-for-new-version.spec.ts`, `describe('checkForNewVersionAfterNavigationError', ...)`, with `check = vi.fn().mockResolvedValue(undefined)` and `{ provide: NewVersionStore, useValue: { check } }`:

```typescript
    it('checks for a new version when a screen fails to load', async () => {
        //given
        TestBed.configureTestingModule({
            providers: [
                provideZonelessChangeDetection(),
                { provide: NewVersionStore, useValue: { check } },
                provideRouter(
                    [{ path: 'students', loadComponent: () => Promise.reject(new TypeError('Failed to fetch dynamically imported module')) }],
                    withNavigationErrorHandler(checkForNewVersionAfterNavigationError),
                ),
            ],
        });

        //when
        await TestBed.inject(Router).navigateByUrl('/students').catch(() => undefined);

        //then
        expect(check).toHaveBeenCalledOnce();
    });
```

In `provide-pwa.spec.ts`, provide the same fake store in every test, and add (set `visibilityState` with `Object.defineProperty(document, 'visibilityState', { configurable: true, value })`, and delete it in `afterEach`):

```typescript
    it('checks for a new version when the app comes back to the screen', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa(), { provide: NewVersionStore, useValue: { check } }] });
        await TestBed.inject(ApplicationInitStatus).donePromise;
        Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' });

        //when
        document.dispatchEvent(new Event('visibilitychange'));

        //then
        expect(check).toHaveBeenCalledOnce();
    });

    it('leaves the check alone when the app goes to the background', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa(), { provide: NewVersionStore, useValue: { check } }] });
        await TestBed.inject(ApplicationInitStatus).donePromise;
        Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' });

        //when
        document.dispatchEvent(new Event('visibilitychange'));

        //then
        expect(check).not.toHaveBeenCalled();
    });
```

- [ ] **Step 3: Write the failing notice spec**

`new-version-notice.component.spec.ts`, `describe('NewVersionNoticeComponent', ...)`, set up like `language-toggle.component.spec.ts`: `TranslocoTestingModule` with `en: { pwa: { newVersionReady: 'A new version of DriveCal is ready.' }, general: { refresh: 'Refresh' } }`, and `{ provide: NewVersionStore, useValue: { isReady, reload } }` where `isReady = signal(false)` and `reload = vi.fn()`.

```typescript
    it('shows nothing while the running version is current', async () => {
        //given
        const fixture = await renderNotice();

        //when
        const notice = (fixture.nativeElement as HTMLElement).querySelector('[role=status]');

        //then
        expect(notice).toBeNull();
    });

    it('offers a refresh once a new version is ready', async () => {
        //given
        const fixture = await renderNotice();
        isReady.set(true);
        await fixture.whenStable();

        //when
        const notice = (fixture.nativeElement as HTMLElement).querySelector('[role=status]');

        //then
        expect(notice?.textContent).toContain('A new version of DriveCal is ready.');
        expect(notice?.querySelector('button')?.textContent).toContain('Refresh');
    });

    it('reloads into the new version when the User asks', async () => {
        //given
        const fixture = await renderNotice();
        isReady.set(true);
        await fixture.whenStable();

        //when
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[role=status] button')?.click();

        //then
        expect(reload).toHaveBeenCalledOnce();
    });
```

- [ ] **Step 4: Run the specs to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/pwa/new-version.store.spec.ts --include src/app/core/pwa/check-for-new-version.spec.ts --include src/app/core/pwa/provide-pwa.spec.ts --include src/app/shared/components/new-version-notice/new-version-notice.component.spec.ts
```

Expected: FAIL to compile (`NewVersionStore`, `checkForNewVersionAfterNavigationError` and `NewVersionNoticeComponent` don't exist).

- [ ] **Step 5: Implement the API service, the store and the trigger**

As in Interfaces. Store state per `client-state.md`: private writable signals (`ready`, `checking`), `readonly isReady = this.ready.asReadonly()`, `firstValueFrom(this.api.getAppPage())` inside `try`/`catch`/`finally`. The helper `mainBundleOf(page: Document): string | null` stays unexported in the store file.

In `provide-pwa.ts`, add the `visibilitychange` listener inside the existing initializer. In `app.config.ts`, change the router provider to `provideRouter(routes, withComponentInputBinding(), withNavigationErrorHandler(checkForNewVersionAfterNavigationError))`.

- [ ] **Step 6: Implement the notice and render it**

The component per Interfaces, imports `ButtonModule` and `TranslocoPipe`. Its SCSS, inside `@layer app`: `.new-version` `position: fixed`, `inset-inline: 1rem`, `inset-block-end: 1rem`, `margin-inline: auto`, `max-inline-size: 28rem`, `z-index: 1000`, `display: flex`, `align-items: center`, `justify-content: space-between`, `gap: 1rem`, `padding-block: 0.75rem`, `padding-inline: 1rem`, background `var(--app-bg-card)`, color `var(--app-ink)`, `border: 1px solid var(--app-border)`, `border-radius: var(--app-radius-card)`, `box-shadow: var(--app-shadow-lift)`. No hex.

Add the key `pwa.newVersionReady` to both translation files (a new top-level `pwa` object, placed alphabetically among the top-level keys if the files are sorted, else at the end).

In `app.ts`, import `NewVersionNoticeComponent` and add `<app-new-version-notice />` right after `<router-outlet />`. In `app.spec.ts`, add `provideHttpClient()` and `provideHttpClientTesting()` to the providers (the real store now needs `HttpClient`).

- [ ] **Step 7: Run the specs to verify they pass**

Same command as step 4. Expected: PASS: 6 store tests, 1 navigation-error test, 4 `providePwa` tests (2 from task 3, 2 new), 3 notice tests.

- [ ] **Step 8: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS (`translations.spec.ts` included: both languages have the new key; `app.spec.ts` with the HTTP providers); the build succeeds, and `dist\client\browser\index.html` has a `<script src="main-` followed by 8 capitals or digits and `.js"`, the shape the store reads. If the build writes the script tag differently, stop and adjust `mainBundleOf` and the spec's `servedPage` to the real shape before continuing.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/core/pwa client/src/app/shared/components/new-version-notice client/src/app/app.ts client/src/app/app.spec.ts client/src/app/app.config.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(pwa): tell the User when a new version is ready and offer a refresh"
```

End the commit message with the attribution line from the session's attribution reminder.
