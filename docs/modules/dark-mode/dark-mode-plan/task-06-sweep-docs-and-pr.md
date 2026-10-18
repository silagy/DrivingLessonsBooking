# Task 6: Every screen in both modes, docs, PR

Part of [Dark mode](README.md). Read the README's Global Constraints and Review Focus first.

**Files:**
- Modify only where the sweep finds a problem (record each fix in the README's As built).
- Modify: `.claude\rules\client-primeng.md`, `CLAUDE.md`, `docs\requirements.md` (§ 8.4)
- Create: `docs\decisions\0009-dark-mode.md`
- Modify: `docs\modules\dark-mode\dark-mode-plan\README.md` (As built)

**Interfaces:**
- Consumes: everything from tasks 1 to 5.
- Produces: the PR.

- [ ] **Step 1: The dark sweep**

In dark, for each of the 6 combinations of Hebrew / English × 360 × 780, 390 × 844 and 1280 × 800, visit:
- the sign-in page;
- `/`, `/teachers`, `/students` (and its no-match state), `/students/import`, `/week-schedules`, `/publications` (every state the dev data has, with the slot requests sheet open), `/publications/history`, `/users`;
- the Student form (every step, as in task 5).

On each page, with the drawer (phone) or user menu (desktop), one ⋮ menu, one dialog, one Select panel and one DatePicker panel opened where the page has them, run after `await document.fonts.ready`:
1. task 4's light-leftover script. Expected `[]`.
2. the contrast script below. Expected `[]`.
3. at phone sizes, the phone measurement from [admin-mobile-plan § The phone measurement](../../admin-mobile/admin-mobile-plan/README.md#the-phone-measurement). Expected as in that plan: no overflow, no clipping. This also covers the Appearance row in the drawer at 360px in English.

```javascript
const lum = (c) => c.match(/[\d.]+/g).slice(0, 3).map(Number).map((v) => v / 255)
    .map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4))
    .reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
const ratio = (a, b) => { const [hi, lo] = [lum(a), lum(b)].sort((x, y) => y - x); return (hi + 0.05) / (lo + 0.05); };
const opaque = (bg) => !/rgba\(.*,\s*0(\.\d+)?\)$/.test(bg);
const backdrop = (el) => {
    for (let e = el; e; e = e.parentElement) {
        const s = getComputedStyle(e);
        if (s.backgroundImage.startsWith('linear-gradient')) return null;
        if (opaque(s.backgroundColor)) return s.backgroundColor;
    }
    return getComputedStyle(document.body).backgroundColor;
};
[...document.querySelectorAll('body *')]
    .filter((el) => el.checkVisibility() && [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent.trim()))
    .map((el) => {
        const s = getComputedStyle(el), bg = backdrop(el);
        if (!bg) return null;
        const px = parseFloat(s.fontSize);
        const large = px >= 24 || (px >= 18.66 && Number(s.fontWeight) >= 700) || el.closest('.p-tag, [class*="__tag"], app-transmission-tag');
        const r = ratio(s.color, bg);
        return r < (large ? 3 : 4.5) ? `${el.tagName.toLowerCase()}.${[...el.classList].join('.')} "${el.textContent.trim().slice(0, 24)}" ${r.toFixed(2)}` : null;
    })
    .filter(Boolean);
```

Text on the Sky gradient is skipped (same as light). Semi-transparent fills are measured against the first opaque ancestor.

Fix a failure in the owning component's token use, or adjust the token in `_tokens.scss` / `app-preset.ts` if the token itself fails; then re-run `dark-tokens.spec.ts`. Record the page, element and ratio before and after under As built. Known risk: white text on `--p-primary-color` (`#3D82FF`, 3.60:1) passes only for icons and large text. List any filled primary control with body-size text.

Record a table: page × combination, with "leftovers / contrast / overflow" all `0` when passing.

- [ ] **Step 2: Check the design frames by eye**

At 390 × 844 Hebrew in dark, take one screenshot per frame and compare with D01 to D08c, and at 1280 with D10. Look for:
- elevation: page `#121418` under cards, bars, menus and sheets at `#1B1D22`; overlays with the deep shadow and a 1px `#2C2F37` border;
- the Sky gradient on primary buttons;
- tags: Administrator sky, Teacher ocean, Active green;
- the new-Student tint `#14213A`;
- the selected slot (`#14213A` with a `#5C95FF` border);
- flags in `#FF8AD8`;
- unchanged layouts.

If screenshots hang, follow the memory note: `await document.fonts.ready` first, else verify through `getComputedStyle`.

- [ ] **Step 3: The light sweep**

In light, the same 6 combinations and pages. Run the phone measurement at phone sizes (expected as before) and compare one screenshot per page at 390 Hebrew and 1280 Hebrew with the same page on `main`. Expected: no difference other than Decision 7's four shifts.

- [ ] **Step 4: System mode and the first paint**

- **System, live:** pick מערכת, then:
  - `resize_window {colorScheme: 'dark'}`: `<html>` gets `app-dark` without a reload, and the page turns dark;
  - `{colorScheme: 'light'}`: it turns light again;
  - pick בהיר, then `{colorScheme: 'dark'}`: it stays light.
- **Reload in dark:** with Dark picked, `navigate` to the current URL. Then:
  - `document.documentElement.classList.contains('app-dark')` is true;
  - `getComputedStyle(document.documentElement).colorScheme` is `dark`;
  - the `theme-color` meta is `#1B1D22`.
- **The production build:** run `ng.js build --project client` and open the built `client\dist\client\browser\index.html` (`angular.json` sets no `outputPath`, so this is the default).
  - The boot `<style>` and `<script>` are in `<head>`, intact, after the `theme-color` meta and before the first stylesheet `<link>`.
  - `html.app-dark` is in the boot style even if critical-CSS inlining dropped `:root.app-dark` from the inlined styles.

- [ ] **Step 5: Excel and emails are unaffected**

Run: `git diff --stat origin/main -- src tests`
Expected: empty output (no backend, Excel or email template change).

- [ ] **Step 6: Run all client suites and the build**

From `client\`: `ng.js test --watch=false`, then `ng.js build --project client`.
Expected: all green, including `theme.service.spec.ts`, `theme-boot.spec.ts`, `dark-tokens.spec.ts`, `no-hardcoded-colours.spec.ts`, `translations.spec.ts` and `source-text.spec.ts`; no new budget warning.

- [ ] **Step 7: Update the docs**

- **`.claude\rules\client-primeng.md`:**
  - The Setup snippet becomes `darkModeSelector: '.app-dark'`.
  - Add a "Dark mode" section: `ThemeService` toggles `app-dark` on `<html>`, and `index.html` sets it before boot. Every colour token has a value in `:root` and `:root.app-dark` (`dark-tokens.spec.ts`). Component SCSS reads `--app-*` and step-less `--p-*` tokens only (`no-hardcoded-colours.spec.ts`). The brand mark's `--brand-mark-*` is the one exception.
  - In Critical Rule 3, name the guard spec.
- **`CLAUDE.md`:** the `core\` line in Project Structure adds "theme (light / dark / system)"; the `shared\` line needs no change.
- **`docs\requirements.md` § 8.4 Localization:** add a bullet: Light, Dark or System (the default) appearance, chosen per device in the admin drawer or user menu; sign-in and the Student form follow the device's saved choice or its system setting.
- **`docs\decisions\0009-dark-mode.md`**, in the format of `0008`: Context (the request, D09), Decision (class-based PrimeNG colour scheme, explicit dark token values, light rendering unchanged, per-device storage with no server-side preference, the boot script), Consequences (two guard specs; every new colour needs a light and a dark token value).
- **README As built:** where the code differs from the tasks and why, the sweep table, and anything not verified in the browser.

- [ ] **Step 8: Commit, push, open the PR**

```bash
git add -A
git commit -m "docs(theme): dark mode as built, rules and ADR 0009"
git push -u origin dark-mode
```

Open the PR to `main` with `gh pr create`. In the body:
- the switch (Light / Dark / System, per device, no flash);
- the dark preset and token set;
- the Appearance control in the drawer and user menu;
- the guard specs;
- the light shifts from Decision 7;
- the sweep table;
- the decisions to look at in review: 5, 6, 7, 8, 10, 12.

End the body with the attribution line from the session. Then follow the app's PR flow (`get_status` / `bind_pr`). Auto-merge stays off; merge by hand with `--merge` once it's reviewed and green.
