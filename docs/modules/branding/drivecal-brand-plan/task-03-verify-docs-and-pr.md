# Task 3: Verify, docs and PR

Part of [DriveCal branding](README.md). Read the README's Global Constraints and Review Focus first. jsdom can't measure layout or apply component styles, so this task's tests are browser-pane measurements with fixed pass conditions. Wait for `await document.fonts.ready` before every measurement and screenshot (memory: the first frame after navigate can be half-rendered; if a screenshot hangs, rely on the DOM measurements).

**Files:**
- Create: `Driving Lesson Mockup\DriveCal Logo Options.html` (copy of the spec)
- Modify: `README.md:3` (product name)
- Modify: `CLAUDE.md:3` (product name and logo reference)
- Modify, only if a measurement fails: `client\src\app\shared\brand-logo\brand-logo.component.scss`, `client\src\app\features\auth\login.component.scss`

**Interfaces:**
- Consumes: everything from tasks 1 and 2. DOM hooks: `app-brand-logo`, `.brand__mark`, `.brand__page`, `.brand__rings`, `.brand__hole`, `.brand__lane`, `.brand__name`, `.brand__accent`, `.brand__tagline`, `.shell__nav`, `.shell__email`, `.login__top`, `.login__card`, `app-language-toggle`.
- Produces: the measured proof for the PR description, and the PR.

- [ ] **Step 1: Start the app and sign in**

`preview_start {name:"api"}`, then `preview_start {name:"client"}`. At `http://localhost:4200/login`, sign in as the dev Administrator from `docs\development\running-the-project.md` (Setup B). Use the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres`.

- [ ] **Step 2: Check the lockup on every surface, in both languages, switching live (Review Focus 1, 2, 4)**

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
const paint = (selector, property) => getComputedStyle(mark.querySelector(selector))[property];
({
  lang: document.documentElement.lang,
  title: document.title,
  name: name.textContent,
  accent: accent.textContent,
  tagline: brand.querySelector('.brand__tagline')?.textContent.trim() ?? null,
  colors: {
    page: paint('.brand__page', 'fill'),
    rings: paint('.brand__rings', 'stroke'),
    hole: paint('.brand__hole', 'fill'),
    lane: paint('.brand__lane', 'stroke'),
    name: getComputedStyle(name).color,
    accent: getComputedStyle(accent).color,
  },
  tracking: getComputedStyle(name).letterSpacing,
  nameLines: Math.round(nameBox.height / parseFloat(getComputedStyle(name).fontSize)),
  markAtInlineStart: rtl ? markBox.left > nameBox.left : markBox.right < nameBox.left,
  accentAtWordEnd: rtl ? accentBox.left <= nameBox.left + 1 : accentBox.right >= nameBox.right - 1,
  markSize: Math.round(markBox.width),
})
```

Run it on `/login`, on `/students` (signed in), and on `/s/not-a-real-token` (the student-form shell renders for any token). On each page, run it in Hebrew, then click the EN option of the language toggle (no reload) and run it again.

Pass when, on every page:
- Hebrew: `name` and `title` are `דרייבקל`, `accent` is `קל`, `tracking` is `0px` or `normal`.
- English: `name` and `title` are `DriveCal`, `accent` is `Cal`, `tracking` is negative (about `-0.48px` in the headers, `-0.65px` on sign-in).
- `colors.page`, `colors.rings` and `colors.accent` are `rgb(0, 87, 255)`; `colors.hole` and `colors.lane` are `rgb(255, 255, 255)`; `colors.name` is `rgb(18, 20, 24)`.
- `nameLines` is `1`; `markAtInlineStart` and `accentAtWordEnd` are `true`.
- `tagline` is the language's tagline on `/login` only, and `null` on the other two pages.
- `markSize` is `40` on `/login` and `28` elsewhere.

If `colors.hole` or `colors.lane` isn't white, check that the `.brand__hole` / `.brand__lane` rules from task 1, step 6 are in the component SCSS and that `--app-on-accent` resolves on `:root`; fix and measure again.

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

Pass: both `true` in both languages. The name is about as wide as "Slotline" / "סלוטליין", so this is expected to pass unchanged.
If `navFits` is `false`: in `brand-logo.component.scss` change `.brand__name` `font-size` from `1.2rem` to `1.125rem`, reload, and measure again in both languages. If it still fails, stop and report `nav.scrollWidth` and `nav.clientWidth` instead of changing #106's breakpoint.

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
If `taglineClearOfToggle` or `pageFits` is `false`, the tagline isn't wrapping: check that `app-brand-logo` computes `min-width: 0px` and `.brand__word` `min-width: 0px`, fix, and measure again.

- [ ] **Step 5: Check the student-form shell on a phone and the served favicon (Review Focus 5)**

`resize_window {preset: "mobile"}`, open `/s/not-a-real-token` in both languages, and run:

```javascript
await document.fonts.ready;
const brand = document.querySelector('app-brand-logo').getBoundingClientRect();
const toggle = document.querySelector('app-language-toggle').getBoundingClientRect();
const svg = await fetch('/favicon.svg', { cache: 'no-store' });
const ico = await fetch('/favicon.ico', { cache: 'no-store' });
({
  pageFits: document.documentElement.scrollWidth <= innerWidth,
  apart: brand.right <= toggle.left || toggle.right <= brand.left,
  links: [...document.querySelectorAll('link[rel=icon]')].map(link => link.getAttribute('href')),
  svg: [svg.status, svg.headers.get('content-type'), (await svg.text()).includes('M20 15.5v4M20 23.5v4M20 31.5v3')],
  ico: [ico.status, (await ico.arrayBuffer()).byteLength],
})
```

Pass: `pageFits` and `apart` are `true` in both languages; `links` is `['favicon.ico', 'favicon.svg']`; `svg` is `[200, 'image/svg+xml', true]`; `ico[0]` is `200` and `ico[1]` is the size printed in task 2, step 7 (not `8327`, the Slotline file).

- [ ] **Step 6: Take the proof screenshots**

`resize_window {preset: "desktop"}`. After `await document.fonts.ready` plus about 1.5s, screenshot `/login` in Hebrew, `/login` in English, and `/students` in Hebrew. Then `resize_window {preset: "mobile"}` and screenshot `/login` in Hebrew. Compare each with the design's Option B: solid blue calendar with white holes and a white lane; "Drive" / "דרייב" in near-black and "Cal" / "קל" in blue, as one word; gray tagline under it on sign-in. Reset with `resize_window {preset: "desktop"}`.

- [ ] **Step 7: Put the spec next to the code**

Copy the design file from the user's export (already extracted in the session scratchpad as `mockup-zip\`; if it's gone, extract `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` into a new empty scratchpad folder first):

```powershell
Copy-Item "<scratchpad>\mockup-zip\DriveCal Logo Options.html" "V:\DrivingLessonsBooking\Driving Lesson Mockup\DriveCal Logo Options.html"
(Get-Item "V:\DrivingLessonsBooking\Driving Lesson Mockup\DriveCal Logo Options.html").Length
```

Expected: `8923`. Don't copy any other file from the zip (README decision 12).

- [ ] **Step 8: Name the product in the living docs**

`README.md:3` becomes:

```markdown
**DriveCal** (דרייבקל in Hebrew) is a weekly demand collection system for a driving school. It replaces a ~3-hour manual WhatsApp-to-Excel ritual with a self-service flow: the admin publishes a weekly availability grid per teacher, students submit ranked session preferences through a shareable link, and the system produces the Excel file the teacher uses for booking.
```

`CLAUDE.md:3` becomes:

```markdown
**DriveCal** (דרייבקל) - weekly demand-collection system for a driving school (see [README.md](README.md) and [docs/requirements.md](docs/requirements.md)). The logo is Option B, Solid calendar, from the Claude Design file `DriveCal Logo Options.html` (a copy is in `Driving Lesson Mockup\`); the client draws it in `client\src\app\shared\brand-logo\`.
```

Then confirm no living file still names Slotline (expect no output; the old plan folder is history and stays):

```powershell
git grep -il "slotline\|סלוטליין" -- . ':!docs/modules/branding/slotline-brand-plan' ':!docs/modules/branding/drivecal-brand-plan'
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
git add "Driving Lesson Mockup/DriveCal Logo Options.html" README.md CLAUDE.md client/src/app/shared/brand-logo/brand-logo.component.scss client/src/app/features/auth/login.component.scss
git commit -m "docs: name the product DriveCal and keep the logo spec in the design snapshot"
git push -u origin drivecal-branding
```

End the commit message with the attribution line from the session's attribution reminder.

Open the PR into `main` with `gh pr create --base main --title "DriveCal branding: solid-calendar logo, two-tone wordmark, tab title and favicon"`. The body lists: the three surfaces and what changed on each; the decisions from the README (two wordmark keys plus `appName`, colors through `--app-brand` / `--app-ink` / `--app-on-accent`, no in-app dark variant, SVG + ICO favicon); the step 2-5 measurement results per language and size; and that the screenshots were checked against Option B. End the body with:

```
🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Then bind the PR with the ccd_pr tools and read its CI. Auto-merge is off; merge by hand with `--merge` once it's reviewed and green.
