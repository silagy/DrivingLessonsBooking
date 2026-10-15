# DriveCal branding - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of the product rename from "Slotline" to **DriveCal**, with the new logo. Each task has its own file. Execute in order.

**Goal:** The client shows the DriveCal Solid-calendar logo and the two-tone wordmark (Drive**Cal**, דרייב**קל** in Hebrew) in every header, keeps the tagline on the sign-in page, names the browser tab DriveCal / דרייבקל, and uses the Solid calendar as its favicon.

**Architecture:** Client only. `BrandLogoComponent` (already used by the admin shell, the sign-in page and the student-form shell) swaps its Lane-tile SVG for the Solid-calendar SVG: the page and binding rings take `currentColor` (`--app-brand`), and the binding holes and lane line are knocked out in `--app-on-accent`. The wordmark is two spans, the lead in ink and the accent in sky, read from two new keys `brand.nameLead` and `brand.nameAccent`. `brand.appName` stays the single full name for the tab title; a spec makes sure the two halves always spell it. `favicon.svg` and the `favicon.ico` fallback are redrawn as the Solid calendar.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), Transloco, SCSS, Vitest under jsdom. Headless Microsoft Edge renders the favicon PNGs. Layout and colors are checked in the browser pane (Claude Browser tools) against the dev API, because jsdom can't measure layout or apply component styles.

**Spec:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, file `DriveCal Logo Options.html`, **Option B · Solid calendar**, with tokens from `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/colors_and_type.css`. The same file is in the user's export `Driving Lesson Mockup.zip` (byte-identical, 8,923 bytes). Choices made in chat on 2026-10-15: Option B; rename the product to DriveCal everywhere (header wordmark, tab title, favicon, living docs); Hebrew name written as one word, דרייבקל. Rules: `.claude\rules\client-architecture.md`, `client-primeng.md`, `client-i18n.md`.

**Branch:** `drivecal-branding`, from `origin/main` at `945b713`. Commit this plan on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR; auto-merge is off, so merge by hand with `--merge` once it's reviewed and green.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Logo | Lane tile (Slotline Option A): outlined rounded square, three dashes; whole lockup in `--app-brand` | `client\src\app\shared\brand-logo\brand-logo.component.ts` + `.scss` |
| Logo users | Admin shell bar, sign-in top bar (`[showTagline]="true"`), student-form shell bar. All on light backgrounds | `admin-shell.component.html:3`, `login.component.html:3`, `student-shell.component.html:3` |
| Name | `brand.appName` = "Slotline" / "סלוטליין"; `brand.tagline` unchanged by this plan | `client\public\i18n\en.json:3`, `he.json:3` |
| Tab title | `LanguageService` sets it from `brand.appName`; `index.html` hard-codes `<title>סלוטליין</title>` for first paint | `client\src\app\core\language.service.ts`, `client\src\index.html:5` |
| Favicon | `favicon.svg` (Lane tile, sky / white in dark mode) + `favicon.ico` (PNG renders 32/48/256, 8,327 bytes) | `client\public\` |
| Asset specs | `brand-assets.spec.ts` pins the Slotline title and Lane-tile geometry | `client\src\app\core\brand-assets.spec.ts` |
| Tokens | `--app-brand` (`--p-sky-500`, #0057FF), `--app-ink` (`--p-surface-900`, #121418), `--app-on-accent` (`--p-surface-0`, #FFFFFF), `--app-wordmark-tracking` (0 in Hebrew) all exist | `client\src\styles\_tokens.scss` |
| Design snapshot | `Driving Lesson Mockup\` is a tracked copy of the design project; it has no `DriveCal Logo Options.html` | repo root |

## The design, as specified

Option B's mark, verbatim (light background):

```html
<symbol id="dB" viewBox="0 0 40 40">
  <rect x="1" y="5.5" width="38" height="33.5" rx="10" fill="currentColor"></rect>
  <path d="M12.5 1.8v7M27.5 1.8v7" stroke="currentColor" stroke-width="3.4" stroke-linecap="round"></path>
  <circle cx="12.5" cy="9.5" r="1.6" fill="#fff"></circle>
  <circle cx="27.5" cy="9.5" r="1.6" fill="#fff"></circle>
  <path d="M20 15.5v4M20 23.5v4M20 31.5v3" stroke="#fff" stroke-width="3.2" stroke-linecap="round"></path>
</symbol>
```

The dark-background variant `dBr` has the same geometry with the colors swapped: page and rings white, holes and lane `#0057FF`.

- Lockup: mark, then a column of name and tagline. The mark is sky (`--sky` #0057FF). The name is ink (#121418) with the accent half in sky: `Drive<b>Cal</b>`, `דרייב<b>קל</b>`, both halves weight 700, no space between them. The tagline is gray-3 (#52555C).
- Name: weight 700, `letter-spacing: -.025em`, `line-height: 1`. In the Hebrew lockup the tracking is `0` and the font is Noto Sans Hebrew.
- Proportions: mark 72 / name 52 / tagline 15 (hero); mark 46 / name 34 / tagline 12.5 with a 12px gap (Hebrew lockup); mark 44 / name 30 (name only). These are the same ratios as the Slotline design, so the header sizes don't change.
- On dark backgrounds the name turns white, the accent turns `#7FA8FF`, and the mark reverses.
- Copy: en "DriveCal" = "Drive" + "Cal", tagline "Weekly lesson demand, in one link"; he "דרייבקל" = "דרייב" + "קל", tagline "ביקוש שבועי לשיעורים, בקישור אחד". The taglines are today's.
- The design notes Option B "holds up at every size", 16px included.

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **One component, not a new one.** All three headers already render `<app-brand-logo />`, so changing it rebrands all of them. The `showName` / `showTagline` inputs and their defaults don't change. |
| 2 | **The mark stays inline SVG, `aria-hidden="true"`.** Colors come from classes in the component SCSS, not SVG attributes: `.brand__page` and `.brand__rings` use `currentColor` (`--app-brand` on `.brand`), `.brand__hole` and `.brand__lane` use `var(--app-on-accent)`. No hex in app code (`client-primeng.md` rule 3). |
| 3 | **Two-tone wordmark from two keys:** `brand.nameLead` and `brand.nameAccent`, rendered as `<span class="brand__name">lead<span class="brand__accent">accent</span></span>` with no whitespace between them. This is a styled logotype, not a sentence, so the "no concatenated fragments" rule (`client-i18n.md`) doesn't apply. The accessible name is the visible text, read as one word. |
| 4 | **`brand.appName` stays the full name** and keeps feeding the tab title through `LanguageService` (no code change there). A spec in `brand-assets.spec.ts` checks, for both languages, that `nameLead + nameAccent === appName`, so the three keys can't drift apart. |
| 5 | **Name color moves from sky to ink** (`--app-ink`), with the accent in `--app-brand`, as designed. The mark keeps `--app-brand`. |
| 6 | **Sizes don't change:** header mark 1.75rem / name 1.2rem / gap 0.5rem; sign-in lockup mark 2.5rem / name 1.625rem / tagline 0.8125rem / gap 0.75rem. Option B uses the same proportions as Option A. |
| 7 | **No dark variant in the component.** Every header is light (`--app-bg-card` / `--app-bg-page`). Only the favicon reverses, under `prefers-color-scheme: dark`, as `dBr`. YAGNI for the in-app white variant. |
| 8 | **`index.html` keeps a Hebrew first-paint title**, now `דרייבקל`. |
| 9 | **Favicon = `favicon.svg` + `favicon.ico`, both redrawn.** The SVG has the exact Option B geometry with its colors in a `<style>` block (sky page / white knockouts; reversed in dark mode). The ICO holds new PNG renders at 32, 48 and 256 px, rendered with headless Edge and packed by the same scratch script the Slotline plan used. |
| 10 | **No backend change.** The email `FromName` is deployment config (`EMAIL_FROM_NAME`, today "בית הספר לנהיגה", no product name); the Excel output has no product name. |
| 11 | **Historical docs keep "Slotline".** `docs\modules\branding\slotline-brand-plan\` describes what was built then. Only living docs (`README.md`, `CLAUDE.md`) get the new name. |
| 12 | **The spec travels with the code.** `DriveCal Logo Options.html` is copied from the user's zip into the tracked `Driving Lesson Mockup\` snapshot, and `CLAUDE.md` points to it. The rest of the snapshot is out of date against the zip, and refreshing it is out of scope. |

## Global Constraints

- Mark geometry exactly as the design: `viewBox="0 0 40 40"`; `rect x="1" y="5.5" width="38" height="33.5" rx="10"`; rings `path d="M12.5 1.8v7M27.5 1.8v7"`, `stroke-width="3.4"`, `stroke-linecap="round"`; holes `circle cx="12.5" cy="9.5" r="1.6"` and `circle cx="27.5" cy="9.5" r="1.6"`; lane `path d="M20 15.5v4M20 23.5v4M20 31.5v3"`, `stroke-width="3.2"`, `stroke-linecap="round"`.
- Names: en `appName "DriveCal"`, `nameLead "Drive"`, `nameAccent "Cal"`; he `appName "דרייבקל"`, `nameLead "דרייב"`, `nameAccent "קל"`. Taglines unchanged.
- Colors reach app CSS only through `var(--app-brand)`, `var(--app-ink)`, `var(--app-on-accent)`. Hex is allowed only in the static `public\favicon.svg` and in the throwaway render page.
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

Dev servers: `preview_start {name:"api"}` and `preview_start {name:"client"}` (`.claude\launch.json`). Sign in with the dev Administrator from `docs\development\running-the-project.md` (Setup B row). Run the API against the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres`.

## Review Focus

1. **A space or line break between "Drive" and "Cal".** Template whitespace or a wrap point between the two spans would show "Drive Cal" or break the word in a narrow header. Expected: the name reads `DriveCal` / `דרייבקל` as one unbroken word. Unit-tested in task 1 (`names the product in two tones, as one word`); measured in task 3, step 2 (`nameLines` is 1).
2. **The knockouts lose their white.** If the hole and lane classes don't get `--app-on-accent`, the SVG default paints them black (holes) or not at all (lane), and the mark becomes a plain blue block. Expected: lane stroke and hole fill `rgb(255, 255, 255)`, page fill `rgb(0, 87, 255)`. jsdom doesn't apply component styles, so this is measured in task 3, step 2.
3. **The wordmark keys drift from the tab title.** Someone edits `appName` and not the halves, or only one language. Expected: `nameLead + nameAccent === appName` in both files. Unit-tested in task 1 (`spells the wordmark halves into the product name in both languages`).
4. **Hebrew lockup mirrors, keeps 0 tracking, and keeps the accent at the end of the word.** In RTL the mark sits at the inline-start (right) of the name, "קל" is the leftmost part of the word, and `letter-spacing` is `0px`. Measured in task 3, step 2.
5. **Browsers that ignore SVG favicons still show the Slotline tile.** They fetch `/favicon.ico`; it must be the new Solid calendar. Checked by eye on the rendered PNG in task 2, step 6, and in the browser in task 3, step 5 (the served ICO is no longer 8,327 bytes).

## Tasks

| # | Task | Deliverable |
|---|------|-------------|
| 1 | [The DriveCal lockup](task-01-drivecal-lockup.md) | `BrandLogoComponent` draws the Solid calendar and the two-tone wordmark; new translation keys; key-consistency spec |
| 2 | [Tab title and favicon](task-02-tab-title-and-favicon.md) | `index.html` title `דרייבקל`, `favicon.svg` + regenerated `favicon.ico`, specs updated |
| 3 | [Verify, docs and PR](task-03-verify-docs-and-pr.md) | Browser measurements for the Review Focus, design file in the snapshot, `README.md` / `CLAUDE.md`, full suites, build, PR |

## Execution

Native execution fits better: three small tasks, and task 3's proof is browser-pane measurement, which needs the session that has the Browser tools.
