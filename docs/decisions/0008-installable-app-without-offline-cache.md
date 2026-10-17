# ADR 0008: Installable App Without an Offline Cache

**Status:** Accepted
**Date:** 17 October 2026

## Context

Administrators and Teachers want DriveCal as an app they install on a phone, tablet or desktop and open from the home screen or the app list, in its own window. Browsers install a site that has a web app manifest. Chrome also offers its install prompt and its address-bar install icon only when a service worker with a `fetch` handler controls the page; the "Install app" menu item no longer needs one (Chrome 108 on mobile, 112 on desktop; see [Revisiting Chrome's installability criteria](https://developer.chrome.com/blog/update-install-criteria)).

DriveCal is online-only: every screen shows live Week Schedules, Submissions and Publications, and nothing useful can be done offline. It also has just paid for stale caching once: before PR #128, phones kept an old `he.json` after a deploy and showed raw translation keys. An installed app makes staleness more likely, because it is resumed far more often than it is reloaded.

## Decision

Make the app installable, keep it online-only, and cache nothing but an offline page.

- `client\public\manifest.webmanifest` names the app in Hebrew (`דרייבקל`, `lang: he`, `dir: rtl`), sets `id`, `start_url` and `scope` to `/`, `display: standalone`, a white `theme_color` and the `--app-bg-page` `background_color`, and lists 192 and 512 icons plus a maskable 512, rendered from the Option E mark. `index.html` links it, with `theme-color`, `apple-touch-icon.png` and `apple-mobile-web-app-title` for iOS.
- `client\public\sw.js` is a hand-written worker with one job. On install it caches `/offline.html` in `drivecal-offline-v1` and takes over at once (`skipWaiting`, `clients.claim`). On activate it deletes every other cache. On `fetch` it handles only page navigations, and only falls back to the cached offline page when the network fails. API calls, bundles, fonts, translations and Excel downloads never pass through it.
- `client\public\offline.html` is bilingual static HTML (Hebrew first, then English), with the wheel and styles inline, because no bundle or translation file can load offline.
- `core\pwa\` registers the worker at startup (a failure is swallowed: the app runs without it) and cancels `beforeinstallprompt` on the student form (`/s/...`), because the installed app opens at sign-in, which is no use to a Student.
- `NewVersionStore` handles an app left open across a deploy. When the app comes back to the screen, or a screen fails to load, it compares the running page's `main-XXXXXXXX.js` with the one in a freshly fetched `/index.html`. If they differ, `app-new-version-notice` says a new version is ready and offers Refresh and "Not now". "Not now" hides it until the app next comes back to the screen, so it never blocks a sticky footer button (the student form's Submit, the Weekly prep dock) for good. It never reloads on its own. The notice sits inside a `role="status"` region that is always in the page, so screen readers announce it.
- Every new public file is unfingerprinted, so `StaticFileCaching` serves it `no-cache`.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| `@angular/service-worker` (`ngsw`) | Caches the app shell, bundles and translations and serves the previous version until a background update check completes; needs an update UI anyway. The stale-deploy failure PR #128 fixed, made permanent |
| No service worker, manifest only | Installs from the browser menu, but no install prompt or address-bar icon in Chrome, and the installed window shows the browser's own offline error |
| Workbox | A dependency and a build step for one cached page |
| Reload on its own when a new version is found | Loses a half-filled form or an open dialog |

## Consequences

- When `offline.html` changes, bump `OFFLINE_CACHE` in `sw.js` in the same commit, or installed apps keep the old offline page.
- `offline.html` is the one place with hardcoded user-visible text, in both languages.
- The new version check depends on the `main-XXXXXXXX.js` name the Angular build writes into `index.html`; `ng serve` writes `main.js`, so development never shows the notice.
- The notice's button is a native `<button>` styled with the primary tokens: it renders in the root component on every page, and PrimeNG's `Button` there would add about 70 kB to the initial bundle.
- Push notifications, background sync or real offline work would need this decision revisited.
