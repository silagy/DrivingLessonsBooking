# Installable DriveCal app (PWA) - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of making DriveCal installable as an app on phones, tablets and desktops. Each task has its own file. Execute in order.

**Goal:** An Administrator or Teacher can install DriveCal from the browser (Chrome and Edge on Android, Windows and macOS; Safari's Add to Home Screen on iPhone and iPad), and it opens in its own window with the DriveCal icon and name. It loads the latest deploy whenever it starts, and an app left open across a deploy tells the User a new version is ready and offers to reload into it.

**Architecture:** Client assets plus a few lines of wiring. A static `manifest.webmanifest` in `client\public\` names the app, sets `display: standalone` and lists PNG icons rendered from the Option E mark. A small hand-written service worker (`public\sw.js`) has one job: when a page navigation fails because the device is offline, it answers with a cached, self-contained `offline.html`. It caches nothing else, so it can never serve a stale bundle or stale translations (the bug fixed in PR #128). `core\pwa\` registers the worker at startup and holds back the browser's install prompt on the student form, because the installed app opens at the sign-in page. A `NewVersionStore` in `core\pwa\` compares the running page's `main-XXXXXXXX.js` with the one in the served `index.html` whenever the app comes back to the screen or a screen fails to load, and a notice in the app root offers Refresh when they differ. The backend needs no code: `StaticFileCaching` already gives the new unfingerprinted files `Cache-Control: no-cache` (tests pin it), and ASP.NET Core already maps `.webmanifest` to `application/manifest+json` (task 5 checks it on the real container).

**Tech Stack:** Angular 21 (standalone, zoneless, signals), plain JavaScript service worker, Vitest under jsdom, MSTest + Shouldly + FakeItEasy, headless Microsoft Edge for icon renders, Docker Compose (Setup A) for header checks, the browser pane for the service worker checks.

**Spec:** The user's request in chat on 2026-10-17: "turn it to PWA so I can install it as application", in a dedicated branch from `main`, plan under `docs\modules\{module}\`. Install rules from Chrome's [Revisiting Chrome's installability criteria](https://developer.chrome.com/blog/update-install-criteria): the menu's "Install app" no longer needs a service worker (Chrome 108 mobile, 112 desktop), but the automatic install prompt and omnibox install icon still need a service worker with a `fetch` handler. Brand rules: `CLAUDE.md` (Option E, wheel and calendar) and `docs\modules\branding\drivecal-option-e-plan\README.md`. Rules: `.claude\rules\client-architecture.md`, `client-state.md`, `client-primeng.md`, `client-i18n.md`, `code-style.md`, `domain-testing.md`.

**Branch:** `pwa-installable`, from `origin/main` at `aad16ea`. Commit this plan on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR; auto-merge is off, so merge by hand with `--merge` once it's reviewed and green.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Head tags | `<title>דרייבקל</title>`, viewport, `favicon.ico` (16/32 wheel, 48/256 app tile) and `favicon.svg` (wheel) | `client\src\index.html` |
| Public assets | `favicon.ico`, `favicon.svg`, `i18n\{en,he}.json`; copied as-is by the `assets` glob in `angular.json` | `client\public\` |
| Manifest, service worker, app icons | none | |
| Static caching | Unfingerprinted files get `Cache-Control: no-cache`, files matching `-[A-Z0-9]{8}\.[a-z0-9]+$` get a year | `src\DrivingLessons.Presentation.Web\StaticFiles\StaticFileCaching.cs`, `tests\DrivingLessons.Application.Test\StaticFiles\StaticFileCachingTest.cs` |
| Unknown file types | `UseStaticFiles` refuses unknown extensions, and `MapFallbackToFile("index.html")` then answers with the app page; a manifest served that way would silently fail | `Program.cs` |
| Startup | `bootstrapApplication(App, appConfig)`; `appConfig.providers` has no initializers | `client\src\main.ts`, `client\src\app\app.config.ts` |
| Routes | `/login`, `/s/:token` (the student form, anonymous), everything else under the admin shell behind `authGuard` | `client\src\app\app.routes.ts`, `shared\config\app-routes.ts` (`studentForm: 's'`) |
| Session | Token and language in `localStorage` (`TOKEN_KEY`, `app_lang`) | `core\auth.service.ts`, `core\language.service.ts` |
| Hosting | Caddy terminates HTTPS for `drivecal.co.il` and proxies everything to Kestrel | `Caddyfile` |
| Brand colors | Navy `#003293` (`--p-sky-700`), blue `#0057FF` (`--p-sky-500`), hub `#7BAEFF` (`--p-sky-300`), white `#ffffff` (`--app-bg-card`), page `#FBFBFF` (`--app-bg-page`), ink `#121418` (`--p-surface-900`), gray `#52555C` (`--p-surface-500`) | `client\src\app\theme\app-preset.ts` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Installable, online-only. No Angular service worker (`@angular/service-worker`).** DriveCal shows live data (Week Schedules, Submissions, Publications) and is useless offline. `ngsw` would cache `index.html`, the bundles and the translations, and serve the previous version until an update check completes, which is the stale-deploy failure PR #128 just fixed. |
| 2 | **A hand-written service worker with one `fetch` job: an offline page for failed navigations.** It is needed for Chrome's automatic install prompt and omnibox install icon (Spec), and it gives the installed app a real "no connection" screen instead of the browser's dinosaur. It intercepts only `request.mode === 'navigate'`; API calls, bundles, fonts, translations and Excel downloads never go through `respondWith`. Its only cache, `drivecal-offline-v1`, holds `/offline.html`. |
| 3 | **The worker installs and takes over at once** (`skipWaiting()` on install, `clients.claim()` on activate), and on activate deletes every cache but its own. It's served with `no-cache` (not fingerprinted), so a deploy that changes `sw.js` reaches users on their next navigation. **When `offline.html` changes, bump the version in `OFFLINE_CACHE`** in the same commit, so the new page is fetched. |
| 4 | **`offline.html` is bilingual static HTML, not an Angular page.** It must render with no network, so it can't load the bundle or `i18n\*.json`. It carries Hebrew first (`lang="he" dir="rtl"`) then English (`lang="en" dir="ltr"`), the wheel inline as SVG, inline styles, and one "try again" button that reloads. This is the one exception to `client-i18n.md` rule 1 (translation keys), because Transloco isn't available offline. The Hebrew copy reuses the app's own wording (`studentForm.loadFailed`). |
| 5 | **The manifest is Hebrew, like the default language and the tab title.** `name` and `short_name` are `דרייבקל`, `description` is the Hebrew tagline, `lang: "he"`, `dir: "rtl"`. The installed app's language still follows the in-app toggle, as in the browser. |
| 6 | **`id`, `start_url` and `scope` are all `/`.** `/` goes through `authGuard`: a signed-in User lands on the admin shell, anyone else on `/login`. |
| 7 | **`theme_color` `#ffffff`** (`--app-bg-card`, the admin and student bars), so the desktop title bar and the Android status bar read as part of the white header. **`background_color` `#FBFBFF`** (`--app-bg-page`) for the Android splash screen. Hex is allowed here because the manifest is a static public file, like `favicon.svg`. |
| 8 | **Icons, all rendered from the Option E mark with the branding plan's tile page:** `icons\icon-192.png` and `icons\icon-512.png` (`purpose: "any"`, the design's app tile: white, `border-radius: 22%`, transparent outside, mark at 50/64 of the side); `icons\icon-maskable-512.png` (`purpose: "maskable"`, full-bleed white square, mark at 60% so the whole mark sits inside the 80% safe circle); `apple-touch-icon.png` (180, full-bleed white square, mark at 50/64; iOS rounds the corners itself and turns transparency black). |
| 9 | **iOS gets `<link rel="apple-touch-icon">` and `<meta name="apple-mobile-web-app-title" content="דרייבקל">`.** Safari reads `display: standalone` from the manifest. No `viewport-fit=cover` and no status bar style meta: the app keeps clear of the notch and the status bar as it does in Safari today. |
| 10 | **The student form never offers installing.** The installed app opens at `/` (sign-in), which is useless to a Student whose link changes every week. A `beforeinstallprompt` listener added at startup calls `preventDefault()` when the path starts with `/s/`. Students can still install from the browser menu; nothing stops that, and nothing needs to. |
| 11 | **No in-app "Install" button.** Browsers already offer installing (Chrome's omnibox icon and menu, Android's prompt, Safari's Share sheet), and iOS has no install API. `docs\development\running-the-project.md` gets a short "Installing the app" section. Revisit if Users don't find it. |
| 12 | **The worker is registered in every environment, including `ng serve`.** It only handles failed navigations, so it can't hide a code change during development. Registration failure is swallowed: the app must run without a worker. |
| 13 | **Backend: tests only.** New `DataRow`s pin `no-cache` for the new files. The `.webmanifest` content type is checked against the real container in task 5 (Setup A), because `UseStaticFiles` uses the framework's default provider. |
| 14 | **The decision is recorded as ADR 0008** (`docs\decisions\0008-installable-app-without-offline-cache.md`), because "why is there a service worker that caches nothing" is the question the next reader will ask. |
| 15 | **After a deploy, tell the User; never reload on its own.** An app left open runs the old bundle until it reloads, and resuming it doesn't reload it. A notice (`app-new-version-notice`, rendered once in `App`) says "A new version of DriveCal is ready." with a Refresh button. Reloading on its own could lose a half-filled form or an open dialog. It's not a `ToastService` toast: those are text-only and time out, and this must stay until the User acts. |
| 16 | **A new version is detected by the main bundle's name.** The production build fingerprints it (`main-IUEPGTXJ.js`); `NewVersionStore` compares the running page's `script[src^="main-"]` with the one in a freshly fetched `/index.html` (`no-cache`, so usually a cheap 304). No version endpoint, no backend change. `ng serve` writes `main.js`, which never matches, so development never shows the notice. |
| 17 | **It checks when the app comes back to the screen (`visibilitychange` to `visible`) and when a screen fails to load (`withNavigationErrorHandler`)**, the two moments a stale bundle shows. No timer. A failed check (offline) is silent; once a new version is found it stops checking. |
| 18 | **The notice shows on every surface**, sign-in and the student form included: a Student with the form open across a deploy runs the old bundle too. One new key, `pwa.newVersionReady`; the button reuses `general.refresh`. |

## Global Constraints

- New public files exactly at: `client\public\manifest.webmanifest`, `client\public\sw.js`, `client\public\offline.html`, `client\public\apple-touch-icon.png`, `client\public\icons\icon-192.png`, `client\public\icons\icon-512.png`, `client\public\icons\icon-maskable-512.png`. The worker is registered as `/sw.js` with `{ scope: '/' }`.
- `sw.js` uses only the globals `self`, `caches` and `fetch` (task 2's spec runs it with fakes for exactly these). Constants: `OFFLINE_CACHE = 'drivecal-offline-v1'`, `OFFLINE_PAGE = '/offline.html'`.
- `offline.html` is self-contained: no `<script src>`, no `<link>`, no `<img>`, no `url(` in styles. Fonts come from the system stack `'Helvetica Neue', Arial, 'Liberation Sans', sans-serif`.
- Mark geometry and colors exactly as `docs\modules\branding\drivecal-option-e-plan\README.md` § Global Constraints.
- No en dash, em dash or ellipsis character in `client\src` sources, `client\public\` files or translations (`core\source-text.spec.ts` covers `src` only; task 1's spec covers the new public text files).
- Standalone, OnPush, signals; no `!important`; logical CSS properties (also in `offline.html`).
- No code comments, in TypeScript, C#, `sw.js` and `offline.html` alike (`CLAUDE.md` rule 3); test markers `//given //when //then` only.
- TypeScript component files indent 2 spaces; spec files 4 spaces; `sw.js` 2 spaces; C# per `code-style.md`.
- No new npm or NuGet dependency.

**Commands** (from `client\` in PowerShell, using the local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include <spec path>
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Backend, from the repo root:

```powershell
dotnet test tests\DrivingLessons.Application.Test --filter "FullyQualifiedName~StaticFileCachingTest"
dotnet test
```

Dev servers: `preview_start {name:"api"}` and `preview_start {name:"client"}` (`.claude\launch.json`). Sign in with the dev Administrator from `docs\development\running-the-project.md` (Setup B row). Run the API against the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres`.

## Review Focus

1. **The manifest is answered with `index.html`.** If the server doesn't know `.webmanifest`, `UseStaticFiles` skips it and the SPA fallback returns the app page with status 200, so Chrome reports "manifest is not valid JSON" and nothing installs. Expected: `GET /manifest.webmanifest` on the real container returns `Content-Type: application/manifest+json` and `Cache-Control: no-cache`. Checked in task 5, step 2 with `Invoke-WebRequest -Method Head`.
2. **A deploy doesn't reach the installed app.** An installed app has no address bar to force a reload, so anything cached longer than the bundle breaks it. Expected: `index.html`, `sw.js`, `offline.html`, the manifest and the icons are `no-cache`; the worker never answers anything but a failed navigation. Unit-tested in task 1 (`A_File_Without_A_Fingerprint_Is_Revalidated` rows) and task 2 (`leaves api calls, bundles and translations to the browser`, `answers a navigation from the network while online`); headers checked in task 5, step 2.
3. **A Student is pushed to install the sign-in page.** Chrome on Android shows an install banner once the criteria are met. Expected: on `/s/...` the browser's `beforeinstallprompt` is cancelled; on admin pages it isn't. Unit-tested in task 3 (`holds back the install prompt on the student form`, `leaves the install prompt to the browser outside the student form`, including `/students`, which also starts with `/s`); checked in the browser in task 5, step 6.
4. **Offline shows a broken or untranslated page.** The offline page can't load anything. Expected: it renders Hebrew and English with the wheel, references no other file, and a navigation that fails offline gets it. Unit-tested in task 2 (`offline page stands alone, in hebrew and english`, `answers a navigation with the offline page when the network fails`); seen in the browser pane in task 5, step 5 with the client server stopped.
5. **The icon is cropped, black-cornered or blurry.** Android crops maskable icons to a circle, iOS turns transparent pixels black. Expected: every PNG has the declared size, the maskable and Apple icons are full-bleed white, and the whole mark is inside the safe circle. Sizes unit-tested in task 1 (`renders every icon at the size it declares`); looked at by eye in task 1, step 6.
6. **An app left open runs yesterday's code.** After a deploy, a resumed app can't open a lazy screen (its chunk is gone from the new image) or shows raw translation keys. Expected: coming back to the screen, or a screen failing to load, shows the new version notice; Refresh loads the new version; nothing shows while the versions match, offline, or in development. Unit-tested in task 4 (`announces a new version when the server serves another main bundle`, `stays quiet while the server serves the running version`, `stays quiet when the server cannot be reached`, `stays quiet when a page has no fingerprinted main bundle`, `checks for a new version when a screen fails to load`, `checks for a new version when the app comes back to the screen`); seen on the production build in task 5, step 3.

## Tasks

| # | Task | Deliverable |
|---|------|-------------|
| 1 | [Manifest and icons](task-01-manifest-and-icons.md) | `manifest.webmanifest`, four PNG icons, the head tags in `index.html`, the asset spec, backend `no-cache` rows |
| 2 | [Offline service worker](task-02-offline-service-worker.md) | `sw.js` and `offline.html`, with a spec that runs the worker against fakes |
| 3 | [Register the worker, hold back the student prompt](task-03-register-and-hold-back-prompt.md) | `core\pwa\` registration and prompt suppression, wired into `appConfig` |
| 4 | [New version notice](task-04-new-version-notice.md) | `NewVersionStore`, its two triggers, the notice in the app root, one translation key |
| 5 | [Verify, docs and PR](task-05-verify-docs-and-pr.md) | Header checks and the notice on Setup A, worker and offline checks in the browser pane, ADR 0008, install instructions, PR |

## Execution

Native execution fits better: five small tasks that build on each other's files (`provide-pwa.ts` grows in tasks 3 and 4), and task 5's proof needs Docker and the browser pane in the same session.
