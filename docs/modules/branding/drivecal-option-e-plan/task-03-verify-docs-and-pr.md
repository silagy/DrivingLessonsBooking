# Task 3: Verify, docs and PR

Part of [DriveCal logo, Option E](README.md). Read the README's Global Constraints and Review Focus first. jsdom can't measure layout, apply component styles or load fonts, so this task's tests are browser-pane measurements with fixed pass conditions. Wait for `await document.fonts.ready` before every measurement and screenshot (the first frame after navigate can be half-rendered; if a screenshot hangs, rely on the DOM measurements).

**Files:**
- Create: `Driving Lesson Mockup\DriveCal Logo Options v2.html` (copy of the spec)
- Modify: `CLAUDE.md:3` (logo reference)
- Modify, only if a measurement fails: `client\src\app\shared\brand-logo\brand-logo.component.scss`, `client\src\app\features\auth\login.component.scss`

**Interfaces:**
- Consumes: everything from tasks 1 and 2. DOM hooks: `app-brand-logo`, `.brand__mark`, `.brand__calendar`, `.brand__page`, `.brand__rings`, `.brand__check`, `.brand__halo`, `.brand__disc`, `.brand__window`, `.brand__hub`, `.brand__name`, `.brand__accent`, `.brand__tagline`, `.shell__nav`, `.shell__email`, `.login__top`, `.login__card`, `app-language-toggle`.
- Produces: the measured proof for the PR description, and the PR.

- [ ] **Step 1: Start the app and sign in**

`preview_start {name:"api"}`, then `preview_start {name:"client"}`. At `http://localhost:4200/login`, sign in as the dev Administrator from `docs\development\running-the-project.md` (Setup B row). Use the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres`.

- [ ] **Step 2: Check the lockup on every surface, in both languages, switching live (Review Focus 1, 2, 3, 4)**

Run this with `javascript_tool` on each page:

```javascript
await document.fonts.ready;
const brand = document.querySelector('app-brand-logo');
const mark = brand.querySelector('.brand__mark');
const name = brand.querySelector('.brand__name');
const accent = name.querySelector('.brand__accent');
const rtl = document.documentElement.dir === 'rtl';
const markBox = mark.getBoundingClientRect();
const nameBox = name.getBoundingClientRect();
const accentBox = accent.getBoundingClientRect();
const nameStyle = getComputedStyle(name);
const paint = (selector, property) => getComputedStyle(mark.querySelector(selector))[property];
const backdropOf = element => {
  for (let node = element; node; node = node.parentElement) {
    const color = getComputedStyle(node).backgroundColor;
    if (color !== 'rgba(0, 0, 0, 0)') return color;
  }
  return null;
};
({
  lang: document.documentElement.lang,
  title: document.title,
  name: name.textContent,
  accent: accent.textContent,
  tagline: brand.querySelector('.brand__tagline')?.textContent.trim() ?? null,
  colors: {
    calendar: paint('.brand__calendar', 'fill'),
    page: paint('.brand__page', 'fill'),
    rings: paint('.brand__rings', 'stroke'),
    check: paint('.brand__check', 'stroke'),
    disc: paint('.brand__disc', 'fill'),
    hub: paint('.brand__hub', 'stroke'),
    name: nameStyle.color,
    accent: getComputedStyle(accent).color,
  },
  backdrop: backdropOf(brand),
  halo: paint('.brand__halo', 'fill'),
  windows: [...mark.querySelectorAll('.brand__window')].map(part => getComputedStyle(part).fill),
  font: { style: nameStyle.fontStyle, weight: nameStyle.fontWeight, tracking: nameStyle.letterSpacing },
  rubik: [...document.fonts].filter(face => face.family.replace(/"/g, '') === 'Rubik').map(face => face.status),
  nameLines: Math.round(nameBox.height / parseFloat(nameStyle.fontSize)),
  markAtInlineStart: rtl ? markBox.left > nameBox.left : markBox.right < nameBox.left,
  accentAtWordEnd: rtl ? accentBox.left <= nameBox.left + 1 : accentBox.right >= nameBox.right - 1,
  markSize: Math.round(markBox.width),
})
```

Run it on `/login`, on `/students` (signed in), and on `/s/not-a-real-token` (the student-form shell renders for any token). On each page, run it in Hebrew, then click the EN option of the language toggle (no reload) and run it again.

Pass when, on every page:
- Hebrew: `name` and `title` are `דרייבקל`, `accent` is `קל`, `font.style` is `normal`, `font.tracking` is about `-0.19px` in the headers and `-0.26px` on sign-in, and `rubik` contains `loaded`.
- English: `name` and `title` are `DriveCal`, `accent` is `Cal`, `font.style` is `italic`, `font.tracking` is about `-0.38px` in the headers and `-0.52px` on sign-in.
- `font.weight` is `800`.
- `colors.calendar`, `colors.check` and `colors.accent` are `rgb(0, 87, 255)`; `colors.rings`, `colors.disc` and `colors.name` are `rgb(0, 50, 147)`; `colors.hub` is `rgb(123, 174, 255)`; `colors.page` is `rgb(255, 255, 255)`.
- `halo` and all three `windows` equal `backdrop`. On `/login` that is `rgb(251, 251, 255)` (`--app-bg-page`); on `/students` and `/s/...` it is `rgb(255, 255, 255)` (`--app-bg-card`).
- `nameLines` is `1`; `markAtInlineStart` and `accentAtWordEnd` are `true`.
- `tagline` is the language's tagline on `/login` only, and `null` on the other two pages.
- `markSize` is `42` on `/login` and `32` elsewhere.

If `halo` is white on `/login`, the step 9 rule from task 1 isn't reaching the logo: check it's in `login.component.scss` and that the selector is `.login__top app-brand-logo`; fix and measure again. If `rubik` is empty or `error`, the `@font-face` from task 1, step 5 isn't in the served CSS or its file 404s: check `read_network_requests` for `rubik-hebrew`.

- [ ] **Step 3: Measure the admin bar at 1280px with a long email**

`resize_window {width: 1280, height: 800}`, open `/students` in Hebrew, then run:

```javascript
await document.fonts.ready;
const email = document.querySelector('.shell__email');
email.textContent = 'a.very.long.administrator.address.for.testing@driving-school.example.co.il';
await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
const nav = document.querySelector('.shell__nav');
({ navFits: nav.scrollWidth <= nav.clientWidth, pageFits: document.documentElement.scrollWidth <= innerWidth })
```

The email text is replaced in the DOM for this measurement only; reload afterwards. Repeat in English.

Pass: both `true` in both languages.
If `navFits` is `false`: in `brand-logo.component.scss` change `.brand__name` `font-size` from `1.2rem` to `1.125rem`, reload, and measure again in both languages (then also rerun step 2 on `/students`; the tracking values scale down with it). If it still fails, stop and report `nav.scrollWidth` and `nav.clientWidth` instead of changing the admin bar's breakpoint.

- [ ] **Step 4: Measure sign-in at phone, tablet and desktop sizes**

Sign out. For each size `{375, 667}`, `{768, 1024}` and `{1440, 900}`, in both languages, open `/login` and run:

```javascript
await document.fonts.ready;
const rtl = document.documentElement.dir === 'rtl';
const top = document.querySelector('.login__top').getBoundingClientRect();
const card = document.querySelector('.login__card').getBoundingClientRect();
const tagline = document.querySelector('.brand__tagline').getBoundingClientRect();
const toggle = document.querySelector('app-language-toggle').getBoundingClientRect();
({
  pageFits: document.documentElement.scrollWidth <= innerWidth,
  topAboveCard: top.bottom <= card.top,
  taglineClearOfToggle: rtl ? tagline.left >= toggle.right : tagline.right <= toggle.left,
})
```

Pass: all three `true` at every size, in both languages.
If `taglineClearOfToggle` or `pageFits` is `false`, the tagline isn't wrapping: check that `app-brand-logo` computes `min-width: 0px` and `.brand__word` `min-width: 0px`, fix, and measure again. If only `topAboveCard` fails at `{375, 667}`, the larger mark made the top bar taller: report the two values instead of shrinking the mark.

- [ ] **Step 5: Check the student-form shell on a phone and the served favicon (Review Focus 5)**

`resize_window {preset: "mobile"}`, open `/s/not-a-real-token` in both languages, and run:

```javascript
await document.fonts.ready;
const brand = document.querySelector('app-brand-logo').getBoundingClientRect();
const toggle = document.querySelector('app-language-toggle').getBoundingClientRect();
const svg = await fetch('/favicon.svg', { cache: 'no-store' });
const svgText = await svg.text();
const ico = await fetch('/favicon.ico', { cache: 'no-store' });
({
  pageFits: document.documentElement.scrollWidth <= innerWidth,
  apart: brand.right <= toggle.left || toggle.right <= brand.left,
  links: [...document.querySelectorAll('link[rel=icon]')].map(link => link.getAttribute('href')),
  svg: [svg.status, svg.headers.get('content-type'), svgText.includes('M6.06 16A14.5 14.5 0 0 1 33.94 16'), svgText.includes('<rect')],
  ico: [ico.status, (await ico.arrayBuffer()).byteLength],
})
```

Pass: `pageFits` and `apart` are `true` in both languages; `links` is `['favicon.ico', 'favicon.svg']`; `svg` is `[200, 'image/svg+xml', true, false]`; `ico[0]` is `200` and `ico[1]` is the size printed in task 2, step 7 (not `6885`, the Solid-calendar file).

- [ ] **Step 6: Take the proof screenshots**

`resize_window {preset: "desktop"}`. After `await document.fonts.ready` plus about 1.5s, screenshot `/login` in Hebrew, `/login` in English, and `/students` in Hebrew. Then `resize_window {preset: "mobile"}` and screenshot `/login` in Hebrew. Compare each with the design's Option E frame: blue calendar with navy rings and a blue check, the navy steering wheel in front of its lower corner with no visible ring around it; "Drive" / "דרייב" in navy and "Cal" / "קל" in blue, as one word, italic in English and upright in Hebrew; gray tagline under it on sign-in. Reset with `resize_window {preset: "desktop"}`.

- [ ] **Step 7: Put the spec next to the code**

Copy the design file from the user's export (already extracted in the session scratchpad as `mockup-e\`; if it's gone, extract `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` into a new empty scratchpad folder first):

```powershell
Copy-Item "<scratchpad>\mockup-e\DriveCal Logo Options v2.html" "V:\DrivingLessonsBooking\Driving Lesson Mockup\DriveCal Logo Options v2.html"
(Get-Item "V:\DrivingLessonsBooking\Driving Lesson Mockup\DriveCal Logo Options v2.html").Length
```

Expected: `11714`. Don't copy any other file from the zip; `DriveCal Logo Options.html` (v1) stays as history.

- [ ] **Step 8: Point CLAUDE.md at Option E**

`CLAUDE.md:3` becomes:

```markdown
**DriveCal** (דרייבקל) - weekly demand-collection system for a driving school (see [README.md](README.md) and [docs/requirements.md](docs/requirements.md)). The logo is Option E, Wheel and calendar, from the Claude Design file `DriveCal Logo Options v2.html` (a copy is in `Driving Lesson Mockup\`); the client draws it in `client\src\app\shared\brand-logo\`, and the favicon is its steering wheel alone.
```

Then confirm no living file still describes the Solid calendar (expect exactly one line of output, the unrelated "Option B" setup reference in `docs/modules/student-form/us-23-public-link-gateway-plan/task-03-anonymous-endpoint.md`):

```powershell
git grep -n -i "solid calendar\|solid-calendar\|Option B" -- . ':!docs/modules/branding/drivecal-brand-plan' ':!docs/modules/branding/drivecal-option-e-plan' ':!Driving Lesson Mockup'
```

- [ ] **Step 9: Run the full client suite and build**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: PASS and a clean build. The backend isn't touched, so `dotnet test` isn't needed.

- [ ] **Step 10: Commit, push and open the PR**

```bash
git add "Driving Lesson Mockup/DriveCal Logo Options v2.html" CLAUDE.md
git commit -m "docs: Option E is the DriveCal logo; keep its design file in the snapshot"
git push -u origin drivecal-logo-option-e
```

If step 3 or 4 changed `brand-logo.component.scss` or `login.component.scss`, add those files to this commit too. End the commit message with the attribution line from the session's attribution reminder.

Open the PR into `main` with `gh pr create --base main --title "DriveCal logo: Option E wheel and calendar, navy italic wordmark, wheel favicon"`. The body lists: the three surfaces and what changed on each; the decisions from the README (nested wheel SVG, backdrop-colored halo and windows with the sign-in override, new tokens, Rubik for Hebrew only, larger mark, wheel favicon and the ICO frames); the new dependency `@fontsource-variable/rubik`; the step 2-5 measurement results per language and size; and that the screenshots were checked against Option E. End the body with:

```
🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Then bind the PR with the ccd_pr tools and read its CI. Auto-merge is off; merge by hand with `--merge` once it's reviewed and green.
