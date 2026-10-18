# Admin menu order: Weekly prep first, Dashboard last - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An Administrator's menu (top bar and phone drawer) reads Weekly prep, Publications, History, Students, Cars & teachers, Users, Dashboard, and signing in lands every User on Weekly prep.

**Architecture:** Client only. The menu order is one array in `features\admin-shell\domain\navigation.ts`. "Weekly prep (Default)" means `/` stops being the Dashboard: the shell's empty child route redirects to `week-schedules` for every Role. The Dashboard moves to its own Administrator-only route `/dashboard` behind the existing `administratorGuard`. That makes `homeGuard` dead code, so it is deleted.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), Angular Router, Vitest + `RouterTestingHarness`, Transloco.

**Spec:** the user's request (2026-10-18): "1. Weekly Prep (Default) 2. Publications 3. History 4. Students 5. Cars and Teachers 6. Users 7. Dashboard". No issue. Rules: `.claude\rules\client-architecture.md` (Routing), `.claude\rules\client-i18n.md`.

**Branch:** `admin-menu-order`, from `origin/main` at `2cb38fc`. This plan is committed on the branch before task 1. The PR targets `main`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Administrator menu | Dashboard, Cars & teachers, Students, Weekly prep, Publications, History, Users | `client\src\app\features\admin-shell\domain\navigation.ts:34-42` |
| Teacher menu | Weekly prep, Publications, History (unchanged by this plan) | same file, line 43 |
| Dashboard item | `commands: ['/']`, `exact: true` | same file, line 10 |
| `/` | `{ path: '', pathMatch: 'full', component: DashboardComponent, canActivate: [homeGuard] }`. An Administrator stays; a Teacher is sent to `/week-schedules` silently | `features\admin-shell\admin.routes.ts:12`, `core\role.guards.ts:24-28` |
| After sign-in | `router.navigate(['/'])`; unknown URLs `redirectTo: ''` | `features\auth\login.component.ts:51`, `app.routes.ts:21` |
| Labels | `shell.nav.*` already exist in `en.json` / `he.json` ("Cars & teachers" / "רכבים ומורים"). No string changes | `client\public\i18n\*.json` |

## Decisions

| # | Decision |
|---|----------|
| 1 | **"(Default)" = the landing page.** `/` redirects to `/week-schedules` for every Role, so sign-in, the `**` fallback and an old `/` bookmark all land on Weekly prep. |
| 2 | **The Dashboard gets its own route, `/dashboard`** (`AppRoutes.dashboard = 'dashboard'`), with `canMatch: [administratorGuard]` like every other Administrator-only screen. A Teacher who types it gets the same no-access toast and Weekly prep that `/students` gives them. |
| 3 | **`homeGuard` is deleted** with its two tests. Nothing uses it once `/` is a redirect. |
| 4 | **The Dashboard item becomes `exact: false`**, like the other single-route items. `exact: true` existed only because `/` is a prefix of every URL. Publications keeps `exact: true` (History lives under it). |
| 5 | **Only the Administrator menu changes.** The Teacher menu already reads Weekly prep, Publications, History, which matches the first three of the new order. |

## Global Constraints

- No hardcoded user-visible strings; no translation key is added or renamed.
- Route paths come from `AppRoutes`; never inline `'dashboard'` in route or navigation code.
- No comments; test section markers `//given //when //then //expected` only.
- No long dashes or ellipsis characters in source (`client\src\app\core\source-text.spec.ts`).
- 4-space indent in new spec files; edits match the surrounding file's indent (`admin.routes.ts` and `role.guards.ts` use 2).

**Commands** (from `client\`, PowerShell, local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

If `client\node_modules` is missing: `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

## Review Focus

1. **An Administrator signs in or opens `/`** (or an old bookmark, or an unknown URL). Expected: Weekly prep opens and its menu link is highlighted, not the Dashboard. Pinned by task 1's `lands every signed-in User on weekly prep` and task 2's browser check.
2. **A Teacher types `/dashboard`.** Expected: Weekly prep with the no-access toast, and the Dashboard chunk is never matched. Pinned by task 1's `sends a Teacher from the dashboard to weekly prep with the no-access toast`.
3. **The phone drawer lists the same order as the bar.** Expected: the drawer hrefs follow the new order. Pinned by task 1's updated `opens the drawer with every Administrator link`.
4. **Hebrew (RTL).** Expected: Weekly prep sits at the inline-start (right) edge of the bar and Dashboard at the end. Task 2's browser check.
5. **The Dashboard is still reachable** from its menu link. Expected: `/dashboard` renders the dashboard for an Administrator, with its link highlighted. Pinned by task 1's `opens the dashboard for an Administrator` and task 2's browser check.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\shared\config\app-routes.ts` | add `dashboard: 'dashboard'` | 1 |
| `client\src\app\features\admin-shell\domain\navigation.ts` | new Administrator order; Dashboard `commands: ['/', AppRoutes.dashboard]`, `exact: false` | 1 |
| `client\src\app\features\admin-shell\domain\navigation.spec.ts` | new order, commands, exact list | 1 |
| `client\src\app\features\admin-shell\admin.routes.ts` | `''` redirects to `week-schedules`; `dashboard` route with `administratorGuard` | 1 |
| `client\src\app\features\admin-shell\admin.routes.spec.ts` | **new**: landing and Dashboard access | 1 |
| `client\src\app\features\admin-shell\admin-shell.component.spec.ts` | drawer href order | 1 |
| `client\src\app\core\role.guards.ts` / `.spec.ts` | delete `homeGuard` and its `describe` | 1 |
| `.claude\rules\client-architecture.md` | lines 40 and 241: drop `homeGuard`, describe the redirect and `/dashboard` | 1 |

---

### Task 1: New menu order, Weekly prep as the landing page, Dashboard at `/dashboard`

**Files:**
- Modify: `client\src\app\shared\config\app-routes.ts`
- Modify: `client\src\app\features\admin-shell\domain\navigation.ts:10,34-42`
- Modify: `client\src\app\features\admin-shell\admin.routes.ts:2,12`
- Modify: `client\src\app\core\role.guards.ts:24-28`
- Modify: `.claude\rules\client-architecture.md:40,241`
- Test: `client\src\app\features\admin-shell\domain\navigation.spec.ts`
- Test: `client\src\app\features\admin-shell\admin-shell.component.spec.ts:92-100`
- Test: `client\src\app\core\role.guards.spec.ts:39-41,81-104`
- Create: `client\src\app\features\admin-shell\admin.routes.spec.ts`

**Interfaces:**
- Produces: `AppRoutes.dashboard === 'dashboard'`; `navigationFor(Role.administrator)` in the new order; `ADMIN_ROUTES` shell children start with `{ path: '', pathMatch: 'full', redirectTo: AppRoutes.weekSchedules }` and include `{ path: AppRoutes.dashboard, canMatch: [administratorGuard], component: DashboardComponent }`.
- Removes: `homeGuard` from `core\role.guards.ts`.

- [ ] **Step 1: Update `navigation.spec.ts` to the new order**

```typescript
it('gives an Administrator every screen, weekly prep first and the dashboard last', () => {
    //expected
    expect(labelsOf(Role.administrator)).toEqual([
        'shell.nav.weeklyPrep',
        'shell.nav.publications',
        'shell.nav.history',
        'shell.nav.students',
        'shell.nav.teachers',
        'shell.nav.users',
        'shell.nav.dashboard',
    ]);
});
```

In `links each screen to its route`: `expect(commandsByLabel.get('shell.nav.dashboard')).toEqual(['/', 'dashboard']);`. Rename `highlights the dashboard and publications only on their exact route` to `highlights publications only on its exact route` and expect `exactLabels` to equal `['shell.nav.publications']`. The Teacher, no-Role and Roster-import tests stay as they are.

- [ ] **Step 2: Update the drawer test in `admin-shell.component.spec.ts`**

The expected hrefs in `opens the drawer with every Administrator link` become:

```typescript
['/week-schedules', '/publications', '/publications/history', '/students', '/teachers', '/users', '/dashboard']
```

- [ ] **Step 3: Write `admin.routes.spec.ts`**

Build the test routes from the real `ADMIN_ROUTES` through a local `withStubPages(routes: Routes): Routes` helper: the shell's `component` and every child's `component` / `loadChildren` are replaced by one `StubPage` (`@Component({ imports: [RouterOutlet], template: '<router-outlet />' })`); `path`, `pathMatch`, `redirectTo` and `canMatch` are kept. Provide `provideZonelessChangeDetection()`, `provideRouter(withStubPages(ADMIN_ROUTES))`, an `AuthService` stub `{ isAuthenticated: signal(true), isAdministrator: signal(<per test>) }` and a `ToastService` stub `{ info: vi.fn().mockResolvedValue(undefined) }` (same shape as `role.guards.spec.ts`'s `setUp`). Navigate with `RouterTestingHarness.create()` + `harness.navigateByUrl(url)` and read `TestBed.inject(Router).url`.

```typescript
it('lands every signed-in User on weekly prep', ...)
    // for isAdministrator true and false ([true, false].forEach or it.each):
    // navigateByUrl('/') -> router.url === '/week-schedules'

it('opens the dashboard for an Administrator', ...)
    // isAdministrator true; navigateByUrl('/dashboard') -> router.url === '/dashboard'; info not called

it('sends a Teacher from the dashboard to weekly prep with the no-access toast', ...)
    // isAdministrator false; navigateByUrl('/dashboard') -> router.url === '/week-schedules'
    // info called with ('access.refusedTitle', { key: 'access.refusedDetail' })
```

- [ ] **Step 4: Run the three specs and see them fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/admin-shell/**/*.spec.ts`
Expected: FAIL. The navigation spec and the drawer test fail on the order and `['/', 'dashboard']`, and `admin.routes.spec.ts` fails because `/` stays on `/` and `/dashboard` doesn't match.

- [ ] **Step 5: Implement**

- `app-routes.ts`: add `dashboard: 'dashboard'` after `login`.
- `navigation.ts`: `DASHBOARD = { labelKey: 'shell.nav.dashboard', commands: ['/', AppRoutes.dashboard], exact: false }`; `ADMINISTRATOR_NAVIGATION = [WEEKLY_PREP, PUBLICATIONS, HISTORY, STUDENTS, TEACHERS, USERS, DASHBOARD]`. Reorder the constant declarations to match the array.
- `admin.routes.ts`: replace line 12 with `{ path: '', pathMatch: 'full', redirectTo: AppRoutes.weekSchedules }`, and add `{ path: AppRoutes.dashboard, canMatch: [administratorGuard], component: DashboardComponent }` as the last child, after `users`. Drop `homeGuard` from the import.
- `role.guards.ts`: delete `homeGuard` and the now-unused `CanActivateFn` import.
- `role.guards.spec.ts`: delete `runHomeGuard` and the `describe('homeGuard', ...)` block, and the `ActivatedRouteSnapshot` / `RouterStateSnapshot` imports.
- `.claude\rules\client-architecture.md` line 40: `role.guards.ts         administratorGuard (#89)`. Line 241: replace the `homeGuard` sentence with "`/` redirects to Weekly prep for every Role, and the dashboard is the Administrator-only `/dashboard`".

- [ ] **Step 6: Run the whole client suite**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false`
Expected: all pass, with no reference to `homeGuard` left (`rg homeGuard client .claude` finds nothing).

- [ ] **Step 7: Commit**

```bash
git add client/src/app .claude/rules/client-architecture.md
git commit -m "feat(shell): Weekly prep first and the landing page; Dashboard last at /dashboard"
```

---

### Task 2: Build, check in the browser, PR

**Files:** none changed unless the check finds a defect (fix it in task 1's files and recommit).

- [ ] **Step 1: Build**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client`
Expected: build succeeds with no new warnings.

- [ ] **Step 2: Browser check (Review Focus 1, 4, 5)**

`docker stop dl-postgres`, `docker compose up -d postgres`, then `preview_start {name: "api"}` and `preview_start {name: "client"}`. Sign in with the dev Administrator from `appsettings.Development.json`. Before measuring, `await document.fonts.ready`; prefer `javascript_tool` over screenshots (memory).
- After sign-in, `location.pathname === '/week-schedules'` and `.shell__nav-link--active` text is "הכנת שבוע".
- `[...document.querySelectorAll('.shell__nav-link')].map(a => a.textContent.trim())` equals `['הכנת שבוע', 'פרסומים', 'היסטוריה', 'תלמידים', 'רכבים ומורים', 'משתמשים', 'לוח בקרה']`, and the first link's `getBoundingClientRect().right` is greater than the last link's (RTL: Weekly prep at the right).
- Switch to English: the labels read Weekly prep, Publications, History, Students, Cars & teachers, Users, Dashboard, and the first link is at the left.
- Click Dashboard: `/dashboard` renders `app-dashboard` and its link is active.
- Resize to 390px wide, open the drawer: the same order.

- [ ] **Step 3: Push and open the PR to `main`** (ask the user before pushing)

```bash
git push -u origin admin-menu-order
gh pr create --base main --title "Admin menu: Weekly prep first and default, Dashboard last" --body-file <scratchpad body>
```

The body lists the new order, the `/` redirect, `/dashboard`, the removed `homeGuard`, and the test and browser evidence, ending with the attribution line.
