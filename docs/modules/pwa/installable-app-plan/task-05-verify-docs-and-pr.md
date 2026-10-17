# Task 5: Verify, docs and PR

Part of [Installable DriveCal app](README.md). Read the README's Review Focus first. Unit tests can't see real response headers, a real worker, a real offline navigation or a real production bundle name, so this task checks them against running servers, with fixed pass conditions.

**Files:**
- Create: `docs\decisions\0008-installable-app-without-offline-cache.md`
- Modify: `docs\development\running-the-project.md` (new section "Installing the app", before "Troubleshooting")
- Modify: `CLAUDE.md` (the `core\` line in Project Structure)
- Modify, only if a check fails: the files from tasks 1 to 4

**Interfaces:**
- Consumes: everything from tasks 1 to 4. DOM hooks: `app-new-version-notice`, `.new-version`, `script[src^="main-"]`.
- Produces: the measured proof for the PR description, and the PR.

- [ ] **Step 1: Build and start Setup A**

From the repo root: `docker compose up -d --build`. Wait until `curl.exe -s http://localhost:8080/api/health` answers `Healthy`.

- [ ] **Step 2: Check the served headers (Review Focus 1, 2)**

```powershell
foreach ($path in '/manifest.webmanifest', '/sw.js', '/offline.html', '/index.html', '/icons/icon-512.png', '/icons/icon-maskable-512.png', '/apple-touch-icon.png') {
  $r = Invoke-WebRequest -UseBasicParsing -Method Head "http://localhost:8080$path"
  "{0} {1} {2} {3}" -f $path, $r.StatusCode, $r.Headers['Content-Type'], $r.Headers['Cache-Control']
}
```

Pass, every line `200` and `no-cache`, with content types: `application/manifest+json`, `text/javascript` (or `application/javascript`), `text/html`, `text/html`, `image/png`, `image/png`, `image/png`.
If the manifest line shows `text/html`, the SPA fallback answered: add `ContentTypeProvider` with an explicit `.webmanifest` mapping to `StaticFileCaching.Options`, with a test in `StaticFileCachingTest`, rebuild and check again.

- [ ] **Step 3: The new version notice on the production build (Review Focus 6)**

This needs the fingerprinted bundle, so it runs on Setup A, not `ng serve`. `preview_start {url: "http://localhost:8080/login"}`, wait for `await document.fonts.ready`, then simulate "this page runs an older deploy" by renaming the running page's main bundle, and bring the page back to the screen:

```javascript
await document.fonts.ready;
const before = document.querySelector('app-new-version-notice .new-version') !== null;
document.querySelector('script[src^="main-"]').setAttribute('src', 'main-00000000.js');
document.dispatchEvent(new Event('visibilitychange'));
await new Promise(resolve => setTimeout(resolve, 1000));
const notice = document.querySelector('app-new-version-notice .new-version');
({ before, shown: notice !== null, role: notice?.getAttribute('role'), text: notice?.textContent.trim() })
```

Pass: `before` `false`, `shown` `true`, `role` `status`, `text` contains `גרסה חדשה של דרייבקל מוכנה.` and `רענון`. Screenshot it at `resize_window {preset: "mobile"}` and at desktop size for the PR; the notice sits at the bottom, centered, and doesn't cause horizontal scroll.
Then click `רענון` and run `document.querySelector('app-new-version-notice .new-version') === null` after the reload. Pass: `true`.
Reset with `resize_window {preset: "desktop"}`, then `docker compose stop app` (keep Postgres running for Setup B).

- [ ] **Step 4: Check the worker in the browser pane**

`preview_start {name:"api"}`, `preview_start {name:"client"}`, sign in at `http://localhost:4200/login` with the dev Administrator (Setup B row of `running-the-project.md`), open `/students`, reload once, then run with `javascript_tool`:

```javascript
const registration = await navigator.serviceWorker.ready;
const manifest = await (await fetch(document.querySelector('link[rel=manifest]').href)).json();
({
  scope: registration.scope,
  script: registration.active.scriptURL,
  controlled: navigator.serviceWorker.controller !== null,
  caches: await caches.keys(),
  offlinePageCached: (await caches.match('/offline.html')) !== undefined,
  manifestName: manifest.name,
  icons: await Promise.all(manifest.icons.map(async icon => (await fetch(icon.src)).status)),
})
```

Pass: `scope` `http://localhost:4200/`, `script` `http://localhost:4200/sw.js`, `controlled` `true`, `caches` `['drivecal-offline-v1']`, `offlinePageCached` `true`, `manifestName` `דרייבקל`, `icons` `[200, 200, 200]`.

Then use the app for a minute (Students, Publications, switch to English and back, hide and show the pane) and check `read_console_messages {onlyErrors: true}` is empty, the screens behave as before, and no new version notice appears (`ng serve` has no fingerprinted bundle).

- [ ] **Step 5: Go offline (Review Focus 4)**

`preview_stop` the **client** server, then `navigate` to `http://localhost:4200/students` and `get_page_text`.
Pass: the text contains `אין חיבור לאינטרנט` and `No internet connection`; screenshot it for the PR.
Then `preview_start {name:"client"}` again, click `נסו שוב` and `get_page_text`. Pass: the Students screen is back, still signed in.

- [ ] **Step 6: The install prompt on the student form (Review Focus 3)**

On `/s/not-a-real-token`, then on `/students`, run:

```javascript
const prompt = new Event('beforeinstallprompt', { cancelable: true });
dispatchEvent(prompt);
prompt.defaultPrevented
```

Pass: `true` on the student form, `false` on `/students`.

- [ ] **Step 7: Write the docs**

`docs\decisions\0008-installable-app-without-offline-cache.md`, in the format of ADR 0005 (Status Accepted, today's date):
- **Context:** Users want DriveCal as an installed app; Chrome's install prompt and address-bar icon need a service worker with a `fetch` handler (cite the Chrome post from the plan's Spec); DriveCal is online-only; PR #128's stale-translation bug; an installed app is resumed more than reloaded.
- **Decision:** README Decisions 1 to 4, 10 and 15 to 18, in prose; name the files.
- **Alternatives:** `@angular/service-worker` (rejected: caches the app shell and translations, serves the old version until an update check, needs an update UI anyway); no service worker (rejected: menu-only install, no prompt or icon, the browser's offline error in the installed window); Workbox (rejected: a dependency for one cached page); reloading on its own when a new version is found (rejected: loses a half-filled form or dialog).
- **Consequences:** bump `OFFLINE_CACHE` when `offline.html` changes; the offline page is the one place with hardcoded user-visible text; the notice depends on the `main-XXXXXXXX.js` name the Angular build writes; no push notifications or background sync without revisiting this.

`running-the-project.md`, section `## Installing the app`: Chrome or Edge on Windows/macOS (the install icon at the end of the address bar, or menu → "Install DriveCal"); Chrome on Android (the install banner, or menu → "Install app"); Safari on iPhone/iPad (Share → "Add to Home Screen"). Then: the app loads the latest deploy whenever it starts, because only the offline page is cached; an app left open shows "A new version of DriveCal is ready" with Refresh when it comes back to the screen after a deploy; installing works on `https://drivecal.co.il` and on `http://localhost` only; the student form doesn't offer it.

`CLAUDE.md`: in Project Structure, the `core\` line becomes `singletons: auth, role guards, language, toast/clipboard/download services, signed-in user, PWA (service worker, new version check)`.

- [ ] **Step 8: Run every suite and the build**

```powershell
dotnet test
cd client
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS; the build succeeds.

- [ ] **Step 9: Commit the docs and open the PR**

```bash
git add docs/decisions/0008-installable-app-without-offline-cache.md docs/development/running-the-project.md CLAUDE.md
git commit -m "docs(pwa): ADR 0008 and how to install the app"
git push -u origin pwa-installable
gh pr create --base main --title "Installable DriveCal app (PWA)" --body-file <scratchpad>\pr-body.md
```

Commit message and PR body end with the attribution lines from the session's attribution reminder. The PR body: what the User gets, Decisions 1, 2, 10, 11 and 15 in one line each, the step 2 header table, the step 3 notice screenshots, the step 4 values, the step 5 screenshot, and a test plan whose last, unchecked item is the acceptance after deploy (step 10). Bind the PR with the ccd_pr tools; CI runs `ci.yml`. Merge by hand with `--merge` once reviewed and green.

- [ ] **Step 10: Acceptance after deploy (by the user)**

After the merge deploys to `https://drivecal.co.il`, ask the user to install it on a phone and a desktop and confirm: the install offer appears; the icon and the name `דרייבקל` on the home screen or in the app list; the app opens in its own window at sign-in or the admin shell; in airplane mode, opening it shows the offline page; an Excel download still works from the installed window. At the next deploy after that, with the installed app left open in the background: bringing it back shows the new version notice, and Refresh loads the new version. Record anything that fails as a new issue.
