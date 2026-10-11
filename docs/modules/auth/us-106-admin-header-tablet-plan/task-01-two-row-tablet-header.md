# Task 1 of 2: A two-row header at tablet width, measured before and after

> Part of [#106: The admin header fits at tablet width](README.md). Work on branch `106-admin-header-tablet`. Requires the plan commit only. Needs the Browser pane tools (`mcp__Claude_Browser__*`).

**Files:**
- Modify: `client\src\app\features\admin-shell\admin-shell.component.scss`

**Interfaces:**
- Consumes: the existing classes in `admin-shell.component.html`: `.shell__bar`, `.shell__nav`, `.shell__nav-link`, `.shell__actions`, `.shell__user`, `.shell__email`, `app-brand-logo`, and the user menu `.p-menu.shell-user-menu` with `.shell-user-menu__email`.
- Produces: no new class or API. Task 2 relies only on the commit.

**Why:** #106 Expected: "At 768px and wider there's no horizontal page scroll (`scrollWidth <= innerWidth`) in Hebrew (RTL) and in English, and the nav and the user menu stay reachable." Vitest runs under jsdom, which can't lay out CSS, so the test cycle here is a measurement in the real browser. Step 2 is the failing measurement, step 5 the passing one.

## The measurement

Steps 2 and 5 run this in the page with `javascript_tool`. It clicks every nav link in turn and measures each page:

```javascript
await document.fonts.ready;
const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const rtl = document.documentElement.dir === 'rtl';
const box = (element) => element.getBoundingClientRect();
const measure = () => {
    const bar = document.querySelector('.shell__bar');
    const nav = document.querySelector('.shell__nav');
    const links = [...document.querySelectorAll('.shell__nav-link')];
    const first = box(links[0]);
    const last = box(links[links.length - 1]);
    const navBox = box(nav);
    const logo = box(document.querySelector('app-brand-logo'));
    const actions = box(document.querySelector('.shell__actions'));
    const email = document.querySelector('.shell__email');
    return {
        page: location.pathname,
        innerWidth,
        pageScrollWidth: document.documentElement.scrollWidth,
        pageOverflows: document.documentElement.scrollWidth > innerWidth,
        barHeight: Math.round(box(bar).height),
        navOverflows: nav.scrollWidth > nav.clientWidth,
        lastLinkInsideNav: last.left >= navBox.left - 1 && last.right <= navBox.right + 1,
        firstLinkAtInlineStart: rtl ? first.right > last.right : first.left < last.left,
        logoAtInlineStart: rtl ? logo.right > actions.right : logo.left < actions.left,
        emailShown: email !== null && getComputedStyle(email).display !== 'none',
    };
};
const results = [];
for (const link of [...document.querySelectorAll('.shell__nav-link')]) {
    link.click();
    await wait(400);
    results.push(measure());
}
results;
```

- [ ] **Step 1: Start the app and sign in**

1. Switch to the compose Postgres: `docker stop dl-postgres`, then `docker compose up -d postgres`.
2. Start the dev servers: `preview_start {name: "api"}`, then `preview_start {name: "client"}`.
3. Open `http://localhost:4200` and sign in with the dev Administrator: `Admin:Email` and `Admin:Password` in `src\DrivingLessons.Presentation.Web\appsettings.Development.json`.
4. Make sure the UI is in Hebrew. If it isn't, click the toggle button with `lang="he"` (`.lang__opt[lang="he"]`).

- [ ] **Step 2: Measure the bug (RED)**

1. `resize_window {width: 768, height: 1024}`, then reload the page.
2. Run the measurement.

Expected, on every page: `pageOverflows: true`, with `pageScrollWidth` about 1022 (the issue's number) and `barHeight: 62`. Keep the numbers for the PR. If `pageOverflows` is already `false`, stop: the bug isn't reproducing, so say so and don't change the SCSS.

- [ ] **Step 3: Change the SCSS**

In `admin-shell.component.scss`:

1. In `.shell__bar`, replace `height: 3.875rem;` with `block-size: 3.875rem;`. It's the same size, written as the logical property that step 3.5 overrides.

2. Replace the `.shell__nav` rule with:

```scss
.shell__nav {
  display: flex;
  gap: 1.625rem;
  align-self: stretch;
  min-inline-size: 0;
  overflow-x: auto;
  scrollbar-width: thin;
}
```

3. In `.shell__nav-link`, add these two declarations after `text-decoration: none;`:

```scss
  flex-shrink: 0;
  white-space: nowrap;
```

4. Replace the `.shell__email` rule with:

```scss
.shell__email {
  display: inline-block;
  max-inline-size: 12rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.8rem;
  color: var(--app-text-secondary);
}
```

5. At the end of the file, add:

```scss
@media screen and (max-width: 992px) {
  .shell__bar {
    block-size: auto;
    flex-wrap: wrap;
    column-gap: 1rem;
    row-gap: 0;
    padding-inline: 1rem;
    padding-block-start: 0.75rem;
  }

  .shell__nav {
    order: 1;
    flex-basis: 100%;
    block-size: 2.75rem;
    gap: 1.25rem;
  }
}
```

What each change does:
- At ≤ 992px, the nav moves to its own full-width second row (`order: 1; flex-basis: 100%`). The logo and the actions share the first row, and `.shell__actions` keeps `margin-inline-start: auto`, so it sits at inline-end.
- At every width, the nav can shrink instead of pushing the page wider. The links keep their size and scroll inside the nav if they ever don't fit.
- The email can't grow past 12rem.
- `overflow-x` is the one physical-axis property (README decision 6).

`ng serve` reloads the styles on save. If the page doesn't update, reload it.

- [ ] **Step 4: Spot-check the look at 768px**

At 768×1024 in Hebrew, check:
- The bar is two rows: the logo at the right, the toggle and avatar with the email at the left, then the seven links in one row starting at the right.
- The active link's underline sits on the bar's bottom border.

Take one screenshot (`computer {action: "screenshot"}`) after `await document.fonts.ready`. If it hangs, skip it (memory note) and rely on step 5's numbers.

- [ ] **Step 5: Measure every width in both languages (GREEN)**

For each width below: `resize_window {width: <w>, height: 1024}`, reload, run the measurement. Do it in Hebrew, then click `.lang__opt[lang="en"]` and repeat every width in English.

| Width | Expected on every page |
|-------|------------------------|
| 768, 820, 992 | `pageOverflows: false`, `barHeight` about 94 to 110 (two rows), `navOverflows: false`, `lastLinkInsideNav: true`, `firstLinkAtInlineStart: true`, `logoAtInlineStart: true`, `emailShown: true` |
| 993, 1024, 1100, 1280 | `pageOverflows: false`, `barHeight: 62` (one row), `logoAtInlineStart: true`, `navOverflows: false`, `lastLinkInsideNav: true` |

- **If `pageOverflows` is `true` anywhere:** the change isn't working. Re-read step 3, check the styles reloaded (`getComputedStyle(document.querySelector('.shell__nav')).minInlineSize` should be `0px`), and fix before going on.
- **If `navOverflows` is `true` at 993 or 1024 in either language** (README decision 4), add this block after the `max-width: 992px` block:

```scss
@media screen and (min-width: 993px) and (max-width: 1199px) {
  .shell__email {
    display: none;
  }
}
```

  Then re-measure 993, 1024 and 1100 in both languages. Expected: `navOverflows: false`, and `emailShown: false` at those widths. At 1280, `emailShown: true` again.
- **If `navOverflows` is still `true` at 993 after the fallback:** the page still doesn't scroll, because the nav scrolls inside the bar behind its thin scrollbar. Record the numbers for the PR description and don't add more rules.

Keep the full table of results (width × language × page) for the PR.

- [ ] **Step 6: A long sign-in email (Review Focus 1)**

At 768 and at 1280, in Hebrew, run:

```javascript
const email = document.querySelector('.shell__email');
email.textContent = 'a.very.long.sign-in.address.for.testing@driving-school.example.co.il';
await new Promise((resolve) => setTimeout(resolve, 100));
({
    pageOverflows: document.documentElement.scrollWidth > innerWidth,
    emailTruncated: email.scrollWidth > email.clientWidth,
    emailWidth: Math.round(email.getBoundingClientRect().width),
});
```

Expected: `pageOverflows: false`, `emailTruncated: true`, and `emailWidth` no more than 192 (12rem). At 1280, if the step 5 fallback added the 993-1199px rule, the email still shows at 1280. The text edit only lives in this page, so reload afterwards to restore the real email.

- [ ] **Step 7: The user menu at tablet width (Review Focus 4)**

At 768×1024, in Hebrew and then in English, run:

```javascript
document.querySelector('.shell__user').click();
await new Promise((resolve) => setTimeout(resolve, 300));
const menu = document.querySelector('.p-menu.shell-user-menu');
const rect = menu.getBoundingClientRect();
({
    open: menu !== null,
    insideViewport: rect.left >= 0 && rect.right <= innerWidth,
    email: menu.querySelector('.shell-user-menu__email')?.textContent?.trim(),
    pageOverflows: document.documentElement.scrollWidth > innerWidth,
});
```

Expected: `open: true`, `insideViewport: true`, the dev Administrator's email, and `pageOverflows: false`. Close the menu with `computer {action: "key", text: "Escape"}`.

- [ ] **Step 8: A three-link nav (Review Focus 5)**

At 768×1024 in Hebrew, simulate a Teacher-role User's nav. The links come from `navigationFor(Role.teacher)`: weeklyPrep, publications, history.

```javascript
const links = [...document.querySelectorAll('.shell__nav-link')];
links.filter((_, index) => ![3, 4, 5].includes(index)).forEach((link) => link.style.display = 'none');
await new Promise((resolve) => setTimeout(resolve, 100));
const nav = document.querySelector('.shell__nav').getBoundingClientRect();
const shown = links.filter((link) => link.style.display !== 'none').map((link) => link.getBoundingClientRect());
({
    navRowWidth: Math.round(nav.width),
    firstAtInlineStart: Math.round(Math.abs(nav.right - shown[0].right)) <= 1,
    pageOverflows: document.documentElement.scrollWidth > innerWidth,
});
```

Expected: `firstAtInlineStart: true` (in RTL the first visible link touches the nav's right edge), `pageOverflows: false`, and `navRowWidth` about 736. Reload to restore the links.

- [ ] **Step 9: Clean up**

`resize_window {preset: "desktop"}`, switch the UI back to Hebrew, then stop both servers (`preview_stop` for each `serverId`).

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/admin-shell/admin-shell.component.scss
git commit -m "fix(auth): the admin header wraps to two rows at tablet width and never widens the page (#106)"
```

End the commit message with the attribution trailer from the session's instructions.
