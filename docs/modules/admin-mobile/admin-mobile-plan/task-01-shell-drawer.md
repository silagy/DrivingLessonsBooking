# Task 1 of 7: One 56px bar and a menu drawer below 640px

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires the plan commit only. Steps 6 to 8 need the Browser pane tools (`mcp__Claude_Browser__*`).

**Files:**
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.scss`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Modify: `.claude\rules\client-primeng.md:167`
- Test: `client\src\app\features\admin-shell\admin-shell.component.spec.ts` (new)

**Interfaces:**
- Consumes: `navigationFor(role)` and `NavigationItem` (`domain\navigation.ts`), `AuthService.email()` / `role()` / `logout()`, `LanguageService.isRtl()`, `LanguageToggleComponent` with its `touch` input, `BrandLogoComponent`, `Drawer` from `primeng/drawer`.
- Produces: the `max-width: 639px` breakpoint convention every later task repeats, and these classes, which task 7 measures:
  - `button.shell__menu-button`
  - `.shell-drawer` (the drawer's `styleClass`)
  - `a.shell-drawer__link` / `.shell-drawer__link--active`
  - `.shell-drawer__footer`, `.shell-drawer__email`, `button.shell-drawer__sign-out`
  - `.shell__bar-language` (the bar's language toggle, hidden on the phone)
- Component members: `isDrawerOpen: WritableSignal<boolean>`, `drawerPosition: Signal<'left' | 'right'>`, `openDrawer(): void`, `closeDrawer(): void`, `onDrawerSignOut(): void`.

**Why:** frame 01 and rule 1: "One 56px bar: logo, avatar, menu button. The tab row moves into a side drawer with language toggle, email and sign out - no more half-cut tabs."

## The phone bar, from frame 01

| Piece | Value |
|-------|-------|
| Bar | `block-size: 3.5rem` (56px), one row, `padding-inline: 0.75rem 0.375rem`, `gap: 0.5rem`. Logo at inline-start; avatar then menu button at inline-end |
| Menu button | 44 × 44, round, `pi pi-bars` icon, text severity secondary, `aria-label` `shell.openMenu`, `aria-expanded` from `isDrawerOpen()`, `aria-haspopup="dialog"` |
| Avatar button | today's `.shell__user` with the email and chevron hidden; 44px tap target, the avatar stays 1.875rem |
| Hidden on the phone | `.shell__nav`, `.shell__bar-language`, `.shell__email`, `.shell__chevron` |
| Hidden at ≥ 640px | `.shell__menu-button` (the drawer can't open, so it never shows) |
| Drawer | `inline-size: min(18.75rem, 85vw)` (300px in the design), from inline-start (README decision 4). Header: `<app-brand-logo />` and PrimeNG's close button (`ariaCloseLabel` = `shell.closeMenu`) |
| Drawer links | one per `navigation()` item, `block-size: 3rem`, `border-radius: 10px`, `font-size: 1rem`. Active: background `var(--p-sky-50)`, color `var(--p-sky-700)`, weight 600, and a 3px `var(--app-grad-sky)` bar at inline-start (`::before`, `inset-inline-start: 0`, `inset-block: 0.625rem`) |
| Drawer footer | `margin-block-start: auto`, `border-block-start: 1px solid var(--app-border)`, `padding: 1rem`, `gap: 0.875rem`, column. Rows: language (label `shell.language` + `<app-language-toggle [touch]="true" />`, space-between); email (`<bdi class="shell-drawer__email" dir="ltr">` + avatar); Sign out (`shell.logout`, `color: var(--p-red-600)`, 44px) |
| Main | `.shell__main { padding: 1.25rem 1rem 1.5rem }` |

- [ ] **Step 1: Write the failing spec**

Create `admin-shell.component.spec.ts`. Render `AdminShellComponent` with:
- `TranslocoTestingModule.forRoot({ langs: { en: {} }, ... })`, so labels render as their keys;
- `provideZonelessChangeDetection()`, `provideRouter([{ path: '**', children: [] }])` (so a tapped link navigates without a "Cannot match any routes" error), `providePrimeNG()`;
- `{ provide: AuthService, useValue: { email: signal('admin@local.dev'), role: signal(role), logout: vi.fn() } }`;
- `{ provide: LanguageService, useValue: { lang: signal(lang), locale: signal('en-IL'), isRtl: signal(lang === 'he'), use: vi.fn() } }`.

Copy the `TestBed` setup style from `publications-dashboard.page.spec.ts`. The drawer may render outside the host, so query `document.body`. Reset the body between tests with `TestBed.resetTestingModule()` and `document.body.innerHTML = ''`.

```typescript
it('opens the drawer with every Administrator link', async () => {
    //given
    const fixture = await render(Role.administrator, 'he');

    //when
    await openDrawer(fixture);

    //then
    const links = [...document.body.querySelectorAll<HTMLAnchorElement>('a.shell-drawer__link')];
    expect(links.map((link) => link.getAttribute('href'))).toEqual([
        '/', '/teachers', '/students', '/week-schedules', '/publications', '/publications/history', '/users',
    ]);
    expect(links.map((link) => link.textContent?.trim())).toContain('shell.nav.weeklyPrep');
});

it('lists only the Teacher links', async () => {
    //given
    const fixture = await render(Role.teacher, 'he');

    //when
    await openDrawer(fixture);

    //then
    const hrefs = [...document.body.querySelectorAll('a.shell-drawer__link')].map((link) => link.getAttribute('href'));
    expect(hrefs).toEqual(['/week-schedules', '/publications', '/publications/history']);
});

it('closes the drawer when a link is tapped', async () => {
    //given
    const fixture = await render(Role.administrator, 'he');
    await openDrawer(fixture);

    //when
    document.body.querySelector<HTMLAnchorElement>('a.shell-drawer__link')!.click();
    await fixture.whenStable();

    //then
    expect(menuButton(fixture).getAttribute('aria-expanded')).toBe('false');
});

it('signs out from the drawer', async () => {
    //given
    const fixture = await render(Role.administrator, 'he');
    await openDrawer(fixture);

    //when
    document.body.querySelector<HTMLButtonElement>('button.shell-drawer__sign-out')!.click();

    //then
    expect(TestBed.inject(AuthService).logout).toHaveBeenCalledOnce();
});

it('shows the email in the drawer footer', async () => {
    //given
    const fixture = await render(Role.administrator, 'he');

    //when
    await openDrawer(fixture);

    //then
    expect(document.body.querySelector('.shell-drawer__email')?.textContent?.trim()).toBe('admin@local.dev');
});

it('opens from the right in Hebrew', async () => {
    //given
    const fixture = await render(Role.administrator, 'he');

    //when
    await openDrawer(fixture);

    //then
    expect(drawerPosition(fixture)).toBe('right');
});

it('opens from the left in English', async () => {
    //given
    const fixture = await render(Role.administrator, 'en');

    //when
    await openDrawer(fixture);

    //then
    expect(drawerPosition(fixture)).toBe('left');
});
```

Helpers in the spec file:
- `menuButton(fixture)` returns `button.shell__menu-button`.
- `openDrawer(fixture)` clicks it and awaits `fixture.whenStable()`.
- `drawerPosition(fixture)` reads `fixture.debugElement.query(By.directive(Drawer)).componentInstance.position()`.

If PrimeNG's drawer doesn't render its content under jsdom after `whenStable()` (its enter animation can hold it back), don't weaken the tests. Provide `providePrimeNG({ ripple: false })`, and add `provideNoopAnimations()` from `@angular/platform-browser/animations`. If the content still doesn't render, check the Drawer's `visible` instead: `fixture.debugElement.query(By.directive(Drawer)).componentInstance.visible`. Then render the link list and footer in a plain `<ng-template>` you can assert on outside the drawer. Note the choice in the commit message.

- [ ] **Step 2: Run the spec to see it fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/admin-shell/admin-shell.component.spec.ts`
Expected: FAIL; `button.shell__menu-button` is null.

- [ ] **Step 3: Add the translation keys**

In `shell` of both files:

| Key | he | en |
|-----|----|----|
| `shell.openMenu` | `פתיחת תפריט` | `Open menu` |
| `shell.closeMenu` | `סגירת תפריט` | `Close menu` |

- [ ] **Step 4: Build the menu button and the drawer**

In `admin-shell.component.ts`:
- Import `DrawerModule` from `primeng/drawer`.
- Inject `LanguageService`.
- Add the members from Interfaces. `drawerPosition` is `computed(() => (this.language.isRtl() ? 'right' : 'left'))`. `onDrawerSignOut()` calls `closeDrawer()`, then `auth.logout()`.

In `admin-shell.component.html`:
- Give the bar's `<app-language-toggle />` `class="shell__bar-language"`.
- After the `p-menu`, inside `.shell__actions`, add the menu `p-button`. Use `styleClass="shell__menu-button"`, `icon="pi pi-bars"`, `[rounded]="true"`, `text`, `severity="secondary"`, the aria attributes from the table through `[pt]` (the same way `.shell__user` sets them), and `(onClick)="openDrawer()"`.
- After `</header>`, add the `p-drawer`:
  - `[visible]="isDrawerOpen()"`, `(visibleChange)="isDrawerOpen.set($event)"`
  - `[position]="drawerPosition()"`, `styleClass="shell-drawer"`
  - `[ariaCloseLabel]="'shell.closeMenu' | transloco"`
  - an `<ng-template #header>` holding `<app-brand-logo />`
- The drawer's content:
  - `nav.shell-drawer__nav`, with one `a.shell-drawer__link` per `navigation()` item. Each link has `[routerLink]`, `routerLinkActive="shell-drawer__link--active"`, `[routerLinkActiveOptions]`, the transloco label, and `(click)="closeDrawer()"`.
  - `div.shell-drawer__footer` with the three rows from the table. Sign out is a `p-button` with `styleClass="shell-drawer__sign-out"`, `text`, the label `shell.logout`, and `(onClick)="onDrawerSignOut()"`.

- [ ] **Step 5: Run the spec to see it pass**

Run the step 2 command.
Expected: PASS, 7 tests.

- [ ] **Step 6: Style the phone bar and the drawer**

In `admin-shell.component.scss`:
1. Add `.shell__menu-button` hidden by default: `:host ::ng-deep .shell__menu-button.p-button { display: none; }`.
2. At the end of the file, after the `max-width: 1279px` block (it must come later, so it wins), add `@media screen and (max-width: 639px)` with the bar, hidden pieces, menu button, avatar button and main values from the table.
3. Add the drawer rules outside the media query, anchored to the overlay class: `::ng-deep .p-drawer.shell-drawer`, `::ng-deep .shell-drawer .shell-drawer__link`, and so on. The drawer only opens on the phone, so they need no breakpoint. Use `var(--app-*)` and `var(--p-*)` tokens only.

`ng serve` reloads styles on save.

- [ ] **Step 7: Measure the phone bar (GREEN)**

1. Start the app and sign in (README, Commands). `resize_window {width: 390, height: 844}`, then reload.
2. On `/publications/history`, in Hebrew, run the README's phone measurement, plus:

```javascript
const rect = (selector) => document.querySelector(selector)?.getBoundingClientRect();
const bar = rect('.shell__bar');
const menu = rect('button.shell__menu-button');
const logo = rect('app-brand-logo');
({
    barHeight: Math.round(bar.height),
    navShown: getComputedStyle(document.querySelector('.shell__nav')).display !== 'none',
    menuSize: [Math.round(menu.width), Math.round(menu.height)],
    menuAtInlineEnd: document.documentElement.dir === 'rtl' ? menu.left < logo.left : menu.right > logo.right,
    emailShown: getComputedStyle(document.querySelector('.shell__email')).display !== 'none',
    mainPaddingInline: getComputedStyle(document.querySelector('.shell__main')).paddingInlineStart,
});
```

Expected: `pageOverflows: false`, `barHeight: 56`, `navShown: false`, `menuSize: [44, 44]`, `menuAtInlineEnd: true`, `emailShown: false`, `mainPaddingInline: "16px"`.

3. Click the menu button. Check that the drawer covers the inline-start side, 300px wide (`document.querySelector('.p-drawer.shell-drawer').getBoundingClientRect()`: `right` equals `innerWidth` in Hebrew), and that the History link is active.
4. Tap Cars & teachers in the drawer. Expected: the page changes and the drawer closes.
5. Open the drawer again and press Escape (`computer {action: "key", text: "Escape"}`). Expected: it closes.
6. Switch to English (the drawer's language toggle works too) and repeat 2 to 4. Expected: `menuAtInlineEnd: true`, and the drawer's `left` is 0.
7. Tap the avatar. Expected: the user menu opens inside the viewport with Change my password and Sign out.

Take one screenshot of the open drawer in Hebrew after `await document.fonts.ready`. Skip it if it hangs (memory).

- [ ] **Step 8: Desktop and tablet are unchanged**

At 1280 × 900 and 768 × 1024, reload and run the measurement from #106 task 1 (`docs\modules\auth\us-106-admin-header-tablet-plan\task-01-two-row-tablet-header.md`). Expected: as #106 recorded. At 1280: `barHeight: 62`, one row. At 768: two rows, `navOverflows: false`. At both, `pageOverflows: false`, and `button.shell__menu-button` has `display: none`. Then `resize_window {preset: "desktop"}`.

- [ ] **Step 9: Record the breakpoint in the rules**

In `.claude\rules\client-primeng.md`, extend the layout-breakpoint line (167) with: `, and the admin phone layout at max-width: 639px (Admin Mobile design, "What changes below 640px"; see docs\modules\admin-mobile\admin-mobile-plan)`.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/admin-shell client/public/i18n/he.json client/public/i18n/en.json .claude/rules/client-primeng.md
git commit -m "feat(admin-mobile): one 56px bar with a menu drawer below 640px"
```

End the commit message with the attribution trailer from the session's instructions.
