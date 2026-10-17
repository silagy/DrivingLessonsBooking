# Task 3: Register the worker, hold back the student prompt

Part of [Installable DriveCal app](README.md). Read the README's Global Constraints and Decisions 10 and 12 first.

**Files:**
- Create: `client\src\app\core\pwa\register-service-worker.ts` + `.spec.ts`
- Create: `client\src\app\core\pwa\hold-back-install-prompt.ts` + `.spec.ts`
- Create: `client\src\app\core\pwa\provide-pwa.ts` + `.spec.ts`
- Modify: `client\src\app\app.config.ts` (one provider)

**Interfaces:**
- Consumes: `/sw.js` from task 2; `AppRoutes.studentForm` (`'s'`) from `shared\config\app-routes.ts`.
- Produces:
  - `registerServiceWorker(container: ServiceWorkerContainer | undefined): Promise<void>`: calls `container.register('/sw.js', { scope: '/' })`; resolves without calling anything when `container` is `undefined`; resolves (never rejects) when `register` rejects.
  - `holdBackInstallPromptOnStudentForm(target: Window): void`: adds a `beforeinstallprompt` listener to `target` that calls `event.preventDefault()` when `target.location.pathname` starts with `` `/${AppRoutes.studentForm}/` ``. The path is read when the event fires, not when the listener is added, because the browser fires the event after the app has routed.
  - `providePwa(): EnvironmentProviders`: `provideAppInitializer` that takes `inject(DOCUMENT).defaultView`, and when it isn't null calls `holdBackInstallPromptOnStudentForm(view)` and starts `registerServiceWorker('serviceWorker' in view.navigator ? view.navigator.serviceWorker : undefined)` without awaiting it, so startup never waits for the worker.
  - `appConfig.providers` gains `providePwa()` after `provideHttpClient(...)`.

- [ ] **Step 1: Write the failing specs**

`register-service-worker.spec.ts`, `describe('registerServiceWorker', ...)`:

```typescript
    it('registers the worker for the whole app', async () => {
        //given
        const container = { register: vi.fn().mockResolvedValue({}) };

        //when
        await registerServiceWorker(container as unknown as ServiceWorkerContainer);

        //then
        expect(container.register).toHaveBeenCalledExactlyOnceWith('/sw.js', { scope: '/' });
    });

    it('does nothing in a browser without service workers', async () => {
        //when
        const registering = registerServiceWorker(undefined);

        //then
        await expect(registering).resolves.toBeUndefined();
    });

    it('keeps the app starting when the browser refuses the worker', async () => {
        //given
        const container = { register: vi.fn().mockRejectedValue(new DOMException('insecure', 'SecurityError')) };

        //when
        const registering = registerServiceWorker(container as unknown as ServiceWorkerContainer);

        //then
        await expect(registering).resolves.toBeUndefined();
    });
```

`hold-back-install-prompt.spec.ts`, `describe('holdBackInstallPromptOnStudentForm', ...)`. Helper `promptAt(path: string): Event` does `window.history.pushState({}, '', path)`, dispatches `new Event('beforeinstallprompt', { cancelable: true })` on `window` and returns it. Call `holdBackInstallPromptOnStudentForm(window)` once in `beforeAll`; `afterEach` pushes `'/'`.

```typescript
    it('holds back the install prompt on the student form', () => {
        //when
        const prompt = promptAt('/s/3f9c2a7d');

        //then
        expect(prompt.defaultPrevented).toBe(true);
    });

    it.each(['/', '/login', '/students', '/publications/history'])(
        'leaves the install prompt to the browser outside the student form (%s)',
        path => {
            //when
            const prompt = promptAt(path);

            //then
            expect(prompt.defaultPrevented).toBe(false);
        },
    );
```

`/students` is the case that guards against a `startsWith('/s')` without the slash (Review Focus 3).

`provide-pwa.spec.ts`, `describe('providePwa', ...)`. In `beforeEach` define `navigator.serviceWorker` with `Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { register } })` where `register = vi.fn().mockResolvedValue({})`; in `afterEach` `delete (navigator as { serviceWorker?: unknown }).serviceWorker` and push `'/'`.

```typescript
    it('registers the worker when the app starts', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa()] });

        //when
        await TestBed.inject(ApplicationInitStatus).donePromise;

        //then
        expect(register).toHaveBeenCalledExactlyOnceWith('/sw.js', { scope: '/' });
    });

    it('holds back the install prompt on the student form once the app starts', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa()] });
        await TestBed.inject(ApplicationInitStatus).donePromise;
        window.history.pushState({}, '', '/s/3f9c2a7d');

        //when
        const prompt = new Event('beforeinstallprompt', { cancelable: true });
        window.dispatchEvent(prompt);

        //then
        expect(prompt.defaultPrevented).toBe(true);
    });
```

- [ ] **Step 2: Run the specs to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/pwa/register-service-worker.spec.ts --include src/app/core/pwa/hold-back-install-prompt.spec.ts --include src/app/core/pwa/provide-pwa.spec.ts
```

Expected: FAIL to compile: the three modules don't exist.

- [ ] **Step 3: Implement the three files**

The signatures and behavior in Interfaces, one exported function per file, 2-space indent. Import `AppRoutes` for the student-form segment; no `'s'` literal.

- [ ] **Step 4: Run the specs to verify they pass**

Same command as step 2. Expected: PASS, 10 tests (3 + 5 + 2).

- [ ] **Step 5: Wire it into the app**

Add `providePwa()` to `appConfig.providers` (see Interfaces). `app.config.spec.ts` must still pass: jsdom has no `navigator.serviceWorker`, so the initializer registers nothing there.

- [ ] **Step 6: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS; the build succeeds within the existing budgets.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/core/pwa client/src/app/app.config.ts
git commit -m "feat(pwa): register the service worker and hold back the install prompt on the student form"
```

End the commit message with the attribution line from the session's attribution reminder.
