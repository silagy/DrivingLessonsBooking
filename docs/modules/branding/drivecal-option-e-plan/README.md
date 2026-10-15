# DriveCal logo, Option E - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of the switch from the Option B Solid-calendar logo to **Option E, Wheel and calendar**, with the Option E wordmark. Each task has its own file. Execute in order.

**Goal:** Every header shows the Option E mark (a calendar with a checked day behind a steering wheel) and the Option E wordmark (navy "Drive" + sky "Cal", weight 800, italic in English; Rubik 800, upright, in Hebrew), and the favicon becomes the plain wheel, as the design asks for sizes under 24px.

**Architecture:** Client only. `BrandLogoComponent` (used by the admin shell, the sign-in page and the student-form shell) swaps its Solid-calendar SVG for the Option E SVG. The wheel is a nested `<svg>` that keeps the design's own 40-unit geometry. A halo circle in the backdrop color separates the wheel from the calendar; the halo and the wheel's windows read the CSS custom property `--brand-logo-backdrop` (default `--app-bg-card`), which the sign-in page sets to `--app-bg-page`. New global tokens carry the navy (`--app-brand-deep`), the hub blue (`--app-brand-soft`) and the wordmark font and style. Rubik's Hebrew subset is self-hosted from `@fontsource-variable/rubik`, the same way Noto Sans Hebrew is. `favicon.svg` becomes the wheel; `favicon.ico` holds the wheel at 16/32 and the full mark on a white app tile at 48/256.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), Transloco, SCSS, Vitest under jsdom, Fontsource. Headless Microsoft Edge renders the favicon PNGs. Layout, colors and fonts are checked in the browser pane (Claude Browser tools) against the dev API, because jsdom can't measure layout, apply component styles or load fonts.

**Spec:** Claude Design file `DriveCal Logo Options v2.html`, frame **Option E · Wheel and calendar**, from the user's export `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` (11,714 bytes). Its Hebrew-font frame says Rubik 800 is the chosen Hebrew wordmark font. Choices made in chat on 2026-10-15: Option E; adopt the full lockup, including the restyled wordmark and the Rubik Hebrew font; implement on a new branch from `main`. Rules: `.claude\rules\client-architecture.md`, `client-primeng.md`, `client-i18n.md`.

**Branch:** `drivecal-logo-option-e`, from `origin/main` at `21fc646`. Commit this plan on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR; auto-merge is off, so merge by hand with `--merge` once it's reviewed and green.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Logo | Option B Solid calendar: one 40-unit SVG, page + rings in `currentColor` (`--app-brand`), holes + lane in `--app-on-accent` | `client\src\app\shared\brand-logo\brand-logo.component.ts` + `.scss` |
| Logo users | Admin shell bar (background `--app-bg-card`), sign-in top bar with `[showTagline]="true"` (sits on `.login`, background `--app-bg-page`), student-form shell bar (background `--app-bg-card`) | `admin-shell.component.html:3`, `login.component.html:3`, `student-shell.component.html:3` |
| Wordmark | Two spans from `brand.nameLead` + `brand.nameAccent`; ink + sky, weight 700, upright, `--app-font-display` (Noto Sans Hebrew for Hebrew glyphs), tracking `--app-wordmark-tracking` (-0.025em, 0 in Hebrew) | same component; `client\src\styles\_tokens.scss` |
| Sizes | Header mark 1.75rem, name 1.2rem, gap 0.5rem; sign-in lockup mark 2.5rem, name 1.625rem, tagline 0.8125rem, gap 0.75rem | `brand-logo.component.scss` |
| Fonts | Only Noto Sans Hebrew, self-hosted from `@fontsource-variable/noto-sans-hebrew` with an `@font-face` in `_global.scss` | `client\src\styles\_global.scss:1-9`, `client\package.json:21` |
| Palette | `--p-sky-700` is `#003293` (the design's navy), `--p-sky-300` is `#7BAEFF` (the design's hub blue); no `--app-*` alias for either yet | `client\src\app\theme\app-preset.ts:9-17` |
| Favicon | `favicon.svg` (Solid calendar, reversed in dark mode) + `favicon.ico` (PNG renders 32/48/256, 6,885 bytes) | `client\public\` |
| Asset specs | `brand-assets.spec.ts` pins the title, favicon links, the Solid-calendar SVG favicon, ICO sizes 32/48/256 and the wordmark keys | `client\src\app\core\brand-assets.spec.ts` |
| Design snapshot | `Driving Lesson Mockup\` has `DriveCal Logo Options.html` (v1, Option B); no v2 | repo root |

## The design, as specified

Option E's mark, verbatim from the v2 file (light background, `--disc:#003293`, `--win:#fff`, `--halo` defaults to `#fff`):

```html
<symbol id="wheel" viewBox="0 0 40 40">
  <circle cx="20" cy="20" r="19" fill="var(--disc)"></circle>
  <path d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z" fill="var(--win)"></path>
  <path d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z" fill="var(--win)"></path>
  <path d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z" fill="var(--win)"></path>
  <circle cx="20" cy="21.5" r="4.6" fill="none" stroke="#7BAEFF" stroke-width="2.2"></circle>
</symbol>
<symbol id="cal" viewBox="0 0 40 40">
  <rect x="13" y="5" width="25" height="24" rx="5" fill="#0057FF"></rect>
  <rect x="15.5" y="11" width="20" height="15.5" rx="2.5" fill="#fff"></rect>
  <path d="M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8" stroke="#003293" stroke-width="2.4" stroke-linecap="round"></path>
  <path d="M26.6 17.6l2.4 2.4 4.2-4.6" fill="none" stroke="#0057FF" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"></path>
  <circle cx="15" cy="25.5" r="14.2" fill="var(--halo,#fff)"></circle>
  <use href="#wheel" x="2" y="12.5" width="26" height="26"></use>
</symbol>
```

- On a dark background the design sets `--disc:#fff`, `--win:#121418` and `--halo:#121418`: the halo and the windows are the backdrop color, so they read as see-through.
- Lockup: mark, then a column of name and tagline. Name `Drive<b>Cal</b>` / `דרייב<b>קל</b>`: weight 800, `line-height: 1`, navy `#003293` with the accent half in sky `#0057FF`. English is italic with `letter-spacing: -.02em` in `'Helvetica Now Display','Helvetica Neue',Arial,sans-serif`. Hebrew is upright Rubik 800 with `letter-spacing: -.01em`. The tagline is gray-3.
- Proportions: mark 84 / name 52 / tagline 15, gap 18 (English hero); mark 54 / name 36 / tagline 12.5, gap 12 (Hebrew lockup); mark 48 / name 30, gap 12 (name only). The mark is about 1.6 times the name size, up from 1.4 in Option B, because the artwork fills less of its 40-unit box.
- Small sizes: "Under 24px, use the wheel on its own". The design shows the full mark at 32 and on a 64px white app tile (`border-radius: 22%`, mark 50px), and the wheel alone at 16.
- Copy is unchanged: en "DriveCal" = "Drive" + "Cal", tagline "Weekly lesson demand, in one link"; he "דרייבקל" = "דרייב" + "קל", tagline "ביקוש שבועי לשיעורים, בקישור אחד".

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **One component, not a new one.** All three headers already render `<app-brand-logo />`; changing it rebrands all of them. The `showName` / `showTagline` inputs and their defaults don't change, and no translation key changes. |
| 2 | **The wheel is a nested `<svg class="brand__wheel" x="2" y="12.5" width="26" height="26" viewBox="0 0 40 40">`**, not a `<symbol>` + `<use>`. It keeps the design's numbers verbatim and needs no document-wide `id`, so several logos on one page can't collide. |
| 3 | **Colors stay in the stylesheet, never in SVG attributes.** Calendar and check: `--app-brand`. Rings, disc and wordmark name: new `--app-brand-deep` (`--p-sky-700`). Hub: new `--app-brand-soft` (`--p-sky-300`). Calendar page: `--app-on-accent`. No hex in app code (`client-primeng.md` rule 3). |
| 4 | **The halo and the windows use the backdrop color**, as the design's dark variant shows: `fill: var(--brand-logo-backdrop, var(--app-bg-card))`. The admin and student bars are `--app-bg-card`, so they need nothing. The sign-in top bar sits on `--app-bg-page`, so `login.component.scss` sets `--brand-logo-backdrop: var(--app-bg-page)` on its `app-brand-logo`. A transparent mask was rejected: `mask="url(#id)"` needs a unique id per instance and resolves against `<base href="/">` on deep routes in some browsers. |
| 5 | **Wordmark tokens:** new `--app-font-wordmark: 'Rubik', 'Helvetica Now Display', 'Helvetica Neue', Arial, 'Liberation Sans', sans-serif` and `--app-wordmark-style: italic` (`normal` under `:root[lang='he']`); `--app-wordmark-tracking` becomes `-0.02em` (`-0.01em` in Hebrew). `--app-wordmark-tracking` is used only by the logo. Hebrew upright comes from the token, so a live language switch flips it with `lang`. |
| 6 | **Rubik covers Hebrew only.** The `@font-face` registers the family `'Rubik'` with Fontsource's Hebrew file and Hebrew `unicode-range`, exactly like the Noto Sans Hebrew face. Latin glyphs fall through to Helvetica Now Display / Helvetica Neue / Arial, as the design specifies. Package `@fontsource-variable/rubik@^5.3.0` (file `files/rubik-hebrew-wght-normal.woff2`, weights 300-900), same major as the Noto package. |
| 7 | **Sizes:** header mark 1.75rem → **2rem** (32px, 1.67 times the 1.2rem name); sign-in lockup mark 2.5rem → **2.625rem** (42px, 1.6 times the 1.625rem name). Name sizes, tagline size and gaps don't change. The admin bar is 3.875rem tall, so a 2rem mark fits. |
| 8 | **No dark variant in the component.** Every header is light. Only the favicon reverses, under `prefers-color-scheme: dark`. |
| 9 | **The SVG favicon is the plain wheel.** A tab draws it at 16 CSS px, below the design's 24px floor for the full mark. In dark mode the disc turns white and the windows `#121418`, as the design's dark cell; the hub stays `#7BAEFF`. |
| 10 | **The ICO holds 16 and 32 as the wheel, 48 and 256 as the full mark on the white app tile.** The 32 frame is what a 2x screen uses for a 16 CSS px tab, so it follows the 24px rule by display size. The tile gives the halo a defined white backdrop on any desktop or bookmark background. `index.html` keeps `sizes="32x32"` on the ICO link so Chromium still prefers the SVG. |
| 11 | **The spec travels with the code.** `DriveCal Logo Options v2.html` is copied into `Driving Lesson Mockup\` next to v1, and `CLAUDE.md` names Option E and the v2 file. The old plan folders are history and stay. |
| 12 | **No backend change.** Nothing on the server draws the logo. |

## Global Constraints

- Mark geometry exactly as the design, all in `viewBox="0 0 40 40"`: calendar `rect x="13" y="5" width="25" height="24" rx="5"`; page `rect x="15.5" y="11" width="20" height="15.5" rx="2.5"`; rings `path d="M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8"` `stroke-width="2.4"` `stroke-linecap="round"`; check `path d="M26.6 17.6l2.4 2.4 4.2-4.6"` `stroke-width="2.3"` `stroke-linecap="round"` `stroke-linejoin="round"`; halo `circle cx="15" cy="25.5" r="14.2"`; wheel `svg x="2" y="12.5" width="26" height="26" viewBox="0 0 40 40"` holding disc `circle cx="20" cy="20" r="19"`, the three window paths from the design, and hub `circle cx="20" cy="21.5" r="4.6"` `stroke-width="2.2"`.
- Paint order inside the mark: calendar, page, rings, check, halo, wheel.
- Colors reach app CSS only through `var(--app-brand)`, `var(--app-brand-deep)`, `var(--app-brand-soft)`, `var(--app-on-accent)`, `var(--app-bg-card)`, `var(--app-bg-page)`. Hex is allowed only in the static `public\favicon.svg` and in the throwaway render pages.
- Wordmark: weight 800; English italic, tracking -0.02em; Hebrew upright Rubik, tracking -0.01em; name `--app-brand-deep`, accent `--app-brand`. Translation keys unchanged.
- Standalone, `ChangeDetectionStrategy.OnPush`, signal inputs. The only new dependency is `@fontsource-variable/rubik@^5.3.0`.
- Logical CSS properties only; no `!important` (`client-i18n.md` rule 3, `client-primeng.md` rule 4).
- No code comments (`CLAUDE.md` rule 3); test markers `//given //when //then` only.
- No en dash, em dash or ellipsis character in `client\src` sources or in translations (`core\source-text.spec.ts`, `core\translations.spec.ts`).
- Component files indent 2 spaces; spec files indent 4 spaces (match the neighbors).

**Commands** (from `client\` in PowerShell, using the local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" install @fontsource-variable/rubik@^5.3.0
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include <spec path>
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

The install prints `EBADENGINE` warnings; they're harmless.

Dev servers: `preview_start {name:"api"}` and `preview_start {name:"client"}` (`.claude\launch.json`). Sign in with the dev Administrator from `docs\development\running-the-project.md` (Setup B row). Run the API against the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres`.

## Review Focus

1. **The halo shows as a pale disc on the sign-in page.** The halo and windows are painted, not transparent, so on `--app-bg-page` a white halo would be a visible circle around the wheel. Expected: on every surface the halo fill and window fill equal the background color behind the logo. Measured in task 3, step 2 (`halo`, `windows` vs `backdrop`).
2. **Rubik never loads, and Hebrew silently falls back to Noto Sans Hebrew.** A wrong `url()` path, a family name that doesn't match the token, or a missing install all look "fine" at a glance. Expected: the Rubik file is installed and referenced (unit-tested in task 1, `self-hosts rubik for the hebrew wordmark only`), and in Hebrew the page's `Rubik` face reports `loaded` (task 3, step 2, `rubik`).
3. **The Hebrew wordmark turns italic.** Browsers synthesize an oblique for Hebrew, which the design avoids. Expected: `font-style` is `normal` in Hebrew and `italic` in English, including after a live language switch without reload. Measured in task 3, step 2.
4. **A space or a break between "Drive" and "Cal".** The heavier, wider italic name could also push the admin bar's nav into overflow. Expected: the name reads as one word on one line, and the admin bar fits at 1280px with a long email. Unit-tested in task 1 (`names the product in two tones, as one word`, kept); measured in task 3, steps 2 and 3.
5. **The tab icon is an unreadable smudge.** The full mark at 16px loses the check and the hub. Expected: the SVG favicon is the wheel only, and the ICO's 16 and 32 frames are the wheel. Unit-tested in task 2 (`draws the plain wheel in the svg favicon`, which also refuses any `<rect`, and `ships an ico fallback that embeds png renders at 16, 32, 48 and 256`); which frame holds the wheel and which the tile is checked by eye on the PNGs in task 2, step 6.

## Tasks

| # | Task | Deliverable |
|---|------|-------------|
| 1 | [The Option E lockup](task-01-option-e-lockup.md) | `BrandLogoComponent` draws the wheel-and-calendar mark and the Option E wordmark; new tokens; Rubik self-hosted for Hebrew; sign-in backdrop override |
| 2 | [Wheel favicon](task-02-wheel-favicon.md) | `favicon.svg` as the wheel, `favicon.ico` with 16/32 wheel and 48/256 app tile, asset specs updated |
| 3 | [Verify, docs and PR](task-03-verify-docs-and-pr.md) | Browser measurements for the Review Focus, v2 design file in the snapshot, `CLAUDE.md`, full suites, build, PR |

## Execution

Native execution fits better: three small tasks that share one component, and task 3's proof is browser-pane measurement, which needs the session that has the Browser tools.
