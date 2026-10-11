# Slotline branding - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of the product rename from "WeekDrive" to **Slotline**, with the new logo. Each task has its own file. Execute in order.

**Goal:** The client shows the Slotline Lane-tile logo and wordmark (סלוטליין in Hebrew) in every header, carries the tagline on the sign-in page, names the browser tab Slotline / סלוטליין, and uses the Lane tile as its favicon.

**Architecture:** Client only. `BrandLogoComponent` (already used by the admin shell, the sign-in page and the student-form shell) swaps its "W" gradient tile for an inline SVG of the Lane tile, colored by a new `--app-brand` token. It reads the name from the existing `brand.appName` key and gets an opt-in `showTagline` input that the sign-in page turns on. The browser tab title switches from the `shell.title` key to `brand.appName`, so the product name lives in one key per language. The Angular default `favicon.ico` is replaced by `favicon.svg` plus a regenerated `favicon.ico` fallback.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), Transloco, SCSS, Vitest under jsdom. Headless Microsoft Edge renders the favicon PNGs. Layout is verified in the browser pane (Claude Browser tools) against the dev API, because jsdom can't measure layout.

**Spec:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, file `Slotline Logo Options.html`, **Option A · Lane tile**, with tokens from `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/colors_and_type.css`. Choices made in chat on 2026-10-11: Option A; apply it to the header logo + name, the browser tab title, the favicon and the sign-in tagline; write the name as סלוטליין in the Hebrew UI. Rules: `.claude\rules\client-architecture.md`, `client-primeng.md`, `client-i18n.md`.

**Branch:** `slotline-branding`, from `main` at `73c91f1`. Commit this plan on the branch before task 1. The PR targets `main`. The repo has no CI and auto-merge is off: verify locally, merge by hand with `--merge`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Logo | `<span class="brand__mark">W</span>`: a 1.75rem tile filled with `--app-grad-sky`, then the name in ink, 700, 1.05rem | `client\src\app\shared\brand-logo\brand-logo.component.ts` + `.scss` |
| Logo users | Admin shell bar, sign-in top bar, student-form shell bar. None passes `showName` | `admin-shell.component.html:3`, `login.component.html:3`, `student-shell.component.html:3` |
| Name | `brand.appName` = "WeekDrive" in both languages | `client\public\i18n\en.json:3`, `he.json:3` |
| Tab title | `LanguageService` sets the title from `shell.title`: "Driving Lessons Planner" / "מערכת תכנון שיעורי נהיגה". `index.html` hard-codes the Hebrew one for the first paint | `client\src\app\core\language.service.ts:13`, `client\src\index.html:5` |
| Favicon | Angular's default `favicon.ico` (3 BMP entries), no `<link rel="icon">` | `client\public\favicon.ico` |
| Tokens | `--app-grad-sky` is also used by the avatar, buttons and student-form chips, so it stays | `client\src\styles\_tokens.scss` |

## The design, as specified

Option A's mark, verbatim:

```html
<symbol id="mA" viewBox="0 0 40 40">
  <rect x="2" y="2" width="36" height="36" rx="10" fill="none" stroke="currentColor" stroke-width="3"></rect>
  <path d="M20 9v5M20 17.5v5M20 26v5" stroke="currentColor" stroke-width="3" stroke-linecap="round"></path>
</symbol>
```

- Lockup: mark, then a column of name and tagline. The whole lockup is `color: var(--sky)` (#0057FF), so the mark **and** the name are sky blue. The tagline is gray-3 (#52555C).
- Name: weight 700, `letter-spacing: -.025em`, `line-height: 1`. In the Hebrew lockup the tracking is `0`.
- Proportions: mark 72 / name 52 / tagline 15 (hero); mark 46 / name 34 / tagline 12.5 with a 12px gap (Hebrew lockup); mark 44 / name 30 (name only).
- On dark backgrounds the mark and name turn white.
- Hebrew copy: name "סלוטליין", tagline "ביקוש שבועי לשיעורים, בקישור אחד". English: "Slotline", "Weekly lesson demand, in one link".
- The design notes that Option A's outline thins out at favicon size. Option A was chosen anyway, so the favicon keeps the geometry unchanged.

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **One component, not a new one.** All three headers already render `<app-brand-logo />`, so changing it rebrands all of them. The tagline is an opt-in `showTagline` input (default `false`), used by the sign-in page only. |
| 2 | **The mark is inline SVG with `currentColor`**, not an `<img>`. It inherits `--app-brand`, needs no extra request, and takes the white dark variant later by changing one color. It is `aria-hidden="true"`; the accessible name comes from the visible wordmark. |
| 3 | **`--app-brand: var(--p-sky-500)`** is a new alias in `_tokens.scss` (`client-primeng.md` rule 3: no hex in app CSS). `--p-sky-500` is `#0057FF` in `app-preset.ts`, the design's `--sky`. |
| 4 | **The wordmark is sky blue, as designed** (today's name is ink). |
| 5 | **Header sizes follow the design's name-only ratio (name ≈ 0.68 × mark):** mark stays 1.75rem (28px), name 1.2rem (19px), gap 0.5rem. **Sign-in lockup:** mark 2.5rem, name 1.625rem, tagline 0.8125rem, gap 0.75rem (the Hebrew lockup's 46/34/12.5 scaled to fit the sign-in top bar). |
| 6 | **Hebrew tracking is 0** through a new `--app-wordmark-tracking` token, reset under `:root[lang='he']` the same way `--app-caps-tracking` already is. Negative tracking crowds Hebrew letters. |
| 7 | **One key for the name.** The tab title reads `brand.appName`; `shell.title` is deleted from both files. A second key would only hold the same word. |
| 8 | **`index.html` keeps a Hebrew first-paint title** (`סלוטליין`), because Hebrew is the default language. `LanguageService` replaces it with the active language's name, as it does today. |
| 9 | **Favicon = `favicon.svg` + `favicon.ico`.** The SVG has the exact Lane-tile geometry, sky on light and white under `prefers-color-scheme: dark` (the design's dark variant). The ICO holds PNG renders at 32, 48 and 256 px for browsers that ignore SVG favicons. The old Angular `favicon.ico` must go, or those browsers keep showing the Angular logo. |
| 10 | **No backend change.** The email `FromName` is deployment config (`EMAIL_FROM_NAME`), not code; the Excel output has no product name. |
| 11 | **Historical docs keep "WeekDrive".** Older plans under `docs\modules\` describe what was built at the time. Only living docs (`README.md`, `CLAUDE.md`) get the new name. |

## Global Constraints

- Mark geometry exactly as the design: `viewBox="0 0 40 40"`; `rect x="2" y="2" width="36" height="36" rx="10"`, `fill="none"`, `stroke-width="3"`; `path d="M20 9v5M20 17.5v5M20 26v5"`, `stroke-width="3"`, `stroke-linecap="round"`.
- Names: en `"Slotline"`, he `"סלוטליין"`. Taglines: en `"Weekly lesson demand, in one link"`, he `"ביקוש שבועי לשיעורים, בקישור אחד"`.
- Brand color `#0057FF` reaches app CSS only through `var(--app-brand)` (alias of `--p-sky-500`). Hex is allowed only in the static `public\favicon.svg` and in the throwaway render page.
- Standalone, `ChangeDetectionStrategy.OnPush`, signal inputs; no new dependency.
- Logical CSS properties only; no `!important` (`client-i18n.md` rule 3, `client-primeng.md` rule 4).
- `en.json` and `he.json` change together (`client-i18n.md` rule 4); keys camelCase.
- No code comments (`CLAUDE.md` rule 3); test markers `//given //when //then` only.
- No en dash, em dash or ellipsis character in `client\src` sources or in translations (`core\source-text.spec.ts`, `core\translations.spec.ts`).
- Component files indent 2 spaces; spec files indent 4 spaces (match the neighbors).

**Commands** (from `client\` in PowerShell, using the local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include <spec path>
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Dev servers: `preview_start {name:"api"}` and `preview_start {name:"client"}` (`.claude\launch.json`). Sign in with the dev Administrator from `docs\development\running-the-project.md` (Setup B row).

## Review Focus

1. **The admin bar at 1280px, Hebrew, with a long email.** #106 tuned the single-row bar so all 7 links fit at 1280px. A bigger, bluer wordmark must not make the nav scroll. Expected: `nav.scrollWidth <= nav.clientWidth`. Measured in task 3, step 3.
2. **Sign-in at 375×667 in both languages.** The tagline is longer than the space beside the language toggle. Expected: it wraps inside its column, the page never scrolls sideways, and the top bar doesn't overlap the card. Measured in task 3, step 4.
3. **Switching language live.** Wordmark, tagline and tab title all switch between Slotline and סלוטליין without a reload. Unit-tested in task 2 (`names the browser tab in the selected language`); seen in the browser in task 3, step 2.
4. **Hebrew lockup mirrors and keeps 0 tracking.** In RTL the mark sits at the inline-start (right) of the name, and the Hebrew name's `letter-spacing` is `0px`. Measured in task 3, step 2.
5. **Browsers that ignore SVG favicons.** They fetch `/favicon.ico`; it must be the Lane tile, not Angular's logo. Unit-tested in task 2 (`ships an ico fallback that embeds png renders`).

## Tasks

| # | Task | Deliverable |
|---|------|-------------|
| 1 | [The Slotline lockup](task-01-slotline-lockup.md) | `BrandLogoComponent` draws the Lane tile and wordmark, with an opt-in tagline used on sign-in; new translations and tokens |
| 2 | [Tab title and favicon](task-02-tab-title-and-favicon.md) | Tab title from `brand.appName`, `shell.title` removed, `favicon.svg` + regenerated `favicon.ico`, `index.html` links |
| 3 | [Verify, docs and PR](task-03-verify-docs-and-pr.md) | Browser measurements for the Review Focus, `README.md` / `CLAUDE.md` name, full suites, build, PR |

## Execution

Native execution fits better: three small tasks, and task 3's proof is browser-pane measurement, which needs the session that has the Browser tools.
