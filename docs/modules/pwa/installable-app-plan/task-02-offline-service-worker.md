# Task 2: Offline service worker

Part of [Installable DriveCal app](README.md). Read the README's Global Constraints and Decisions 1 to 4 first.

**Files:**
- Create: `client\src\app\core\pwa\service-worker.spec.ts`
- Modify: `client\src\app\core\pwa\pwa-assets.spec.ts` (offline page test)
- Create: `client\public\sw.js`
- Create: `client\public\offline.html`

**Interfaces:**
- Consumes: `publicText(path)` from task 1's `pwa-assets.spec.ts`.
- Produces: `/sw.js`, a classic (non-module) worker script that task 3 registers with `navigator.serviceWorker.register('/sw.js', { scope: '/' })`; `/offline.html`. Task 5 checks both in the browser.

## The worker's behavior

| Event | Does |
|-------|------|
| `install` | `event.waitUntil(...)`: open `OFFLINE_CACHE`, `add(OFFLINE_PAGE)`, then `self.skipWaiting()` |
| `activate` | `event.waitUntil(...)`: `caches.keys()`, `caches.delete` every name but `OFFLINE_CACHE`, then `self.clients.claim()` |
| `fetch`, `event.request.mode === 'navigate'` | `event.respondWith(fetch(event.request)` falling back, on rejection, to `caches.match(OFFLINE_PAGE))` |
| `fetch`, any other mode | returns without calling `respondWith`, so the browser fetches as if there were no worker |

- [ ] **Step 1: Write the failing worker spec**

Create `client\src\app\core\pwa\service-worker.spec.ts`, `describe('offline service worker', ...)`. It runs the real file with fakes:

`startWorker(network: (request: FakeRequest) => Promise<unknown>)` reads `public/sw.js` and calls `new Function('self', 'caches', 'fetch', source)(self, caches, network)`, where:
- `FakeRequest` is `{ mode: string; url: string }`.
- `self` records `addEventListener(type, listener)` in a map, and has `skipWaiting` and `clients.claim` as `vi.fn(() => Promise.resolve())`.
- `caches` is an in-memory `Map<string, Map<string, unknown>>` with `open(name)` (creates the entry; returns `{ add(url), match(url) }`, where `add` stores the constant `OFFLINE_RESPONSE = { page: 'offline' }` under `url`), `match(url)` (searches every cache), `keys()` and `delete(name)`. It exposes the map as `stored` for assertions.
- `startWorker` returns `{ self, stored, dispatch(type, request?) }`. `dispatch` builds an event with `waitUntil(promise)` and `respondWith(promise)` that record their promises, calls the listener, awaits every `waitUntil` promise, and returns `{ responded: boolean, response: Promise<unknown> | undefined }`.

Tests:

```typescript
    it('caches the offline page and takes over as soon as it installs', async () => {
        //given
        const worker = startWorker(() => Promise.resolve({}));

        //when
        await worker.dispatch('install');

        //then
        expect([...worker.stored.keys()]).toEqual(['drivecal-offline-v1']);
        expect(worker.stored.get('drivecal-offline-v1')?.get('/offline.html')).toBe(OFFLINE_RESPONSE);
        expect(worker.self.skipWaiting).toHaveBeenCalled();
    });

    it('answers a navigation from the network while online', async () => {
        //given
        const page = { page: 'students' };
        const worker = startWorker(() => Promise.resolve(page));
        await worker.dispatch('install');

        //when
        const result = await worker.dispatch('fetch', { mode: 'navigate', url: '/students' });

        //then
        expect(result.responded).toBe(true);
        expect(await result.response).toBe(page);
    });

    it('answers a navigation with the offline page when the network fails', async () => {
        //given
        const worker = startWorker(() => Promise.reject(new TypeError('Failed to fetch')));
        await worker.dispatch('install');

        //when
        const result = await worker.dispatch('fetch', { mode: 'navigate', url: '/students' });

        //then
        expect(await result.response).toBe(OFFLINE_RESPONSE);
    });

    it.each([
        { mode: 'cors', url: '/api/publications' },
        { mode: 'same-origin', url: '/i18n/he.json?revision=2026-10-17' },
        { mode: 'no-cors', url: '/main-IUEPGTXJ.js' },
        { mode: 'cors', url: '/api/publications/7f3c/excel' },
    ])('leaves api calls, bundles and translations to the browser ($url)', async request => {
        //given
        const worker = startWorker(() => Promise.reject(new TypeError('Failed to fetch')));

        //when
        const result = await worker.dispatch('fetch', request);

        //then
        expect(result.responded).toBe(false);
    });

    it('keeps only its own cache when it activates', async () => {
        //given
        const worker = startWorker(() => Promise.resolve({}));
        worker.stored.set('drivecal-offline-v0', new Map());
        worker.stored.set('ngsw:/:db:control', new Map());
        await worker.dispatch('install');

        //when
        await worker.dispatch('activate');

        //then
        expect([...worker.stored.keys()]).toEqual(['drivecal-offline-v1']);
        expect(worker.self.clients.claim).toHaveBeenCalled();
    });
```

- [ ] **Step 2: Add the failing offline page test**

In `pwa-assets.spec.ts` (Review Focus 4):

```typescript
    it('offline page stands alone, in hebrew and english', () => {
        //when
        const page = publicText('offline.html');

        //then
        expect(page).toContain('<html lang="he" dir="rtl">');
        expect(page).toContain('<title>דרייבקל</title>');
        expect(page).toContain('אין חיבור לאינטרנט');
        expect(page).toContain('דרייבקל צריך חיבור כדי להציג נתונים עדכניים. בדקו את החיבור לאינטרנט ונסו שוב.');
        expect(page).toContain('נסו שוב');
        expect(page).toContain('lang="en" dir="ltr"');
        expect(page).toContain('No internet connection');
        expect(page).toContain('DriveCal needs a connection to show current data. Check your connection and try again.');
        expect(page).toContain('Try again');
        expect(page).toContain('onclick="location.reload()"');
        expect(page).toContain('cx="20" cy="20" r="19"');
        expect(page).not.toMatch(/<script\s[^>]*src|<link|<img|url\(/);
    });
```

- [ ] **Step 3: Run both specs to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/pwa/service-worker.spec.ts --include src/app/core/pwa/pwa-assets.spec.ts
```

Expected: FAIL: every worker test with `ENOENT` on `public/sw.js`, and the offline page test with `ENOENT` on `public/offline.html`. The four task 1 tests pass.

- [ ] **Step 4: Write `client\public\sw.js`**

The constants `OFFLINE_CACHE = 'drivecal-offline-v1'` and `OFFLINE_PAGE = '/offline.html'`, and the three listeners from the behavior table. Only `self`, `caches` and `fetch` as globals; no `importScripts`, no comments.

- [ ] **Step 5: Write `client\public\offline.html`**

A complete document: `<!doctype html>`, `<html lang="he" dir="rtl">`, `<meta charset="utf-8">`, the same viewport tag as `src\index.html`, `<title>דרייבקל</title>`, one inline `<style>`, and a `<main>` with:
1. The wheel, 64px, inline: the `favicon.svg` geometry with fills `#003293` (disc), `#FFFFFF` (windows), stroke `#7BAEFF` (hub), no dark-mode block.
2. A `<section>` (inherits Hebrew/RTL): `<h1>אין חיבור לאינטרנט</h1>`, the Hebrew sentence from the test, `<button type="button" onclick="location.reload()">נסו שוב</button>`.
3. A `<section lang="en" dir="ltr">` with the English heading, sentence and `Try again` button, the same way.

Styles: `body` background `#FBFBFF`, color `#121418`, font `'Helvetica Neue', Arial, 'Liberation Sans', sans-serif`, margin 0; `main` a centered column, `max-inline-size: 28rem`, `margin-inline: auto`, `padding: 3rem 1rem`, `text-align: center`; `h1` 1.25rem, weight 700; `p` color `#52555C`; `button` background `#0057FF`, color `#FFFFFF`, no border, `border-radius: 6px`, `padding-block: 0.625rem`, `padding-inline: 1.25rem`, font inherited, weight 600; the English section separated by `border-block-start: 1px solid #E6E8F6`, `margin-block-start: 2rem`, `padding-block-start: 2rem`. Logical properties only.

- [ ] **Step 6: Run both specs to verify they pass**

Same command as step 3. Expected: PASS, 8 worker tests (four from the `each`) and 5 asset tests (the punctuation test now also reads `sw.js` and `offline.html`).

- [ ] **Step 7: Look at the offline page**

Open `client\public\offline.html` in the browser pane (`preview_start {url: "file:///V:/DrivingLessonsBooking/client/public/offline.html"}`), wait for `await document.fonts.ready`, and screenshot. Pass: the navy wheel, then the Hebrew block centered and reading right to left, then a divider, then the English block; both buttons blue with white text; no horizontal scroll at `resize_window {preset: "mobile"}`. Reset with `{preset: "desktop"}`.

- [ ] **Step 8: Run the full client suite**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/core/pwa/service-worker.spec.ts client/src/app/core/pwa/pwa-assets.spec.ts client/public/sw.js client/public/offline.html
git commit -m "feat(pwa): service worker that shows an offline page for failed navigations"
```

End the commit message with the attribution line from the session's attribution reminder.
