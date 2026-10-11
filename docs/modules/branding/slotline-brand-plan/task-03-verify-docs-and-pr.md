# Task 3: Verify, docs and PR

Part of [Slotline branding](README.md). Read the README's Global Constraints and Review Focus first. jsdom can't measure layout, so this task's tests are browser-pane measurements with fixed pass conditions. Wait for `await document.fonts.ready` before every measurement and screenshot (memory: the first frame after navigate can be half-rendered).

**Files:**
- Modify: `README.md:1-3` (product name)
- Modify: `CLAUDE.md:3` (product name)
- Modify, only if a measurement fails: `client\src\app\shared\brand-logo\brand-logo.component.scss`, `client\src\app\features\auth\login.component.scss`

**Interfaces:**
- Consumes: everything from tasks 1 and 2. DOM hooks: `app-brand-logo`, `.brand__mark`, `.brand__name`, `.brand__tagline`, `.shell__nav`, `.shell__email`, `.login__top`, `.login__card`, `app-language-toggle`.
- Produces: the measured proof for the PR description, and the PR.

- [ ] **Step 1: Start the app and sign in**

`preview_start {name:"api"}`, then `preview_start {name:"client"}`. At `http://localhost:4200/login`, sign in as the dev Administrator from `docs\development\running-the-project.md` (Setup B). Use the compose Postgres (`drivinglessonsbooking-postgres-1`), not `dl-postgres` (memory: its migration history is stale).

- [ ] **Step 2: Check the lockup on every surface, in both languages, switching live**

Define this check once with `javascript_tool` and run it on each page:

```javascript
await document.fonts.ready;
const brand = document.querySelector('app-brand-logo');
const mark = brand.querySelector('.brand__mark');
const name = brand.querySelector('.brand__name');
const rtl = document.documentElement.dir === 'rtl';
const markBox = mark.getBoundingClientRect();
const nameBox = name.getBoundingClientRect();
({
  lang: document.documentElement.lang,
  title: document.title,
  name: name.textContent.trim(),
  tagline: brand.querySelector('.brand__tagline')?.textContent.trim() ?? null,
  markColor: getComputedStyle(mark).color,
  nameColor: getComputedStyle(name).color,
  tracking: getComputedStyle(name).letterSpacing,
  markAtInlineStart: rtl ? markBox.left > nameBox.left : markBox.right < nameBox.left,
  markSize: Math.round(markBox.width),
})
```

Run it on `/login`, on `/students` (signed in), and on `/s/not-a-real-token` (the student-form shell renders for any token). On each page, run it in Hebrew, then click the EN option of the language toggle (no reload) and run it again.

Pass when, on every page:
- Hebrew: `name` and `title` are `סלוטליין`, `tracking` is `0px` or `normal`, `markAtInlineStart` is `true`.
- English: `name` and `title` are `Slotline`, `tracking` is negative (about `-0.48px` in the headers, `-0.65px` on sign-in), `markAtInlineStart` is `true`.
- `markColor` and `nameColor` are `rgb(0, 87, 255)`.
- `tagline` is the language's tagline on `/login` only, and `null` on the other two pages.
- `markSize` is `40` on `/login` and `28` elsewhere.

- [ ] **Step 3: Measure the admin bar at 1280px with a long email (Review Focus 1)**

`resize_window {width: 1280, height: 800}`, open `/students` in Hebrew, then run:

```javascript
await document.fonts.ready;
const email = document.querySelector('.shell__email');
email.textContent = 'a.very.long.administrator.address.for.testing@driving-school.example.co.il';
await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
const nav = document.querySelector('.shell__nav');
({ navFits: nav.scrollWidth <= nav.clientWidth, pageFits: document.documentElement.scrollWidth <= innerWidth })
```

The email text is replaced in the DOM for this measurement only (it is capped at 9rem, so any long address measures the same); reload afterwards. Repeat in English.

Pass: both `true` in both languages.
If `navFits` is `false`: in `brand-logo.component.scss` change `.brand__name` `font-size` from `1.2rem` to `1.125rem`, reload, and measure again in both languages. If it still fails, stop and report the numbers (`nav.scrollWidth`, `nav.clientWidth`) instead of changing #106's breakpoint.

- [ ] **Step 4: Measure sign-in at phone and tablet sizes (Review Focus 2)**

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
If `topAboveCard` is `false` at 375×667: in `login.component.scss`, at the end of the file, put the top bar in the flow on phones and measure again:

```scss
@media screen and (max-width: 576px) {
  .login {
    justify-content: flex-start;
  }

  .login__top {
    position: static;
    inline-size: 100%;
    margin-block-end: 2rem;
  }
}
```

If `taglineClearOfToggle` or `pageFits` is `false`, the tagline isn't wrapping: check that `app-brand-logo` computes `min-width: 0px` and `.brand__word` `min-width: 0px`, fix, and measure again.

- [ ] **Step 5: Check the student-form shell on a phone**

`resize_window {preset: "mobile"}`, open `/s/not-a-real-token` in both languages, and run:

```javascript
await document.fonts.ready;
const brand = document.querySelector('app-brand-logo').getBoundingClientRect();
const toggle = document.querySelector('app-language-toggle').getBoundingClientRect();
({ pageFits: document.documentElement.scrollWidth <= innerWidth, apart: brand.right <= toggle.left || toggle.right <= brand.left })
```

Pass: both `true` in both languages.

- [ ] **Step 6: Check the favicon is served (Review Focus 5)**

On any page:

```javascript
const svg = await fetch('/favicon.svg');
const ico = await fetch('/favicon.ico');
({
  links: [...document.querySelectorAll('link[rel=icon]')].map(link => link.getAttribute('href')),
  svg: [svg.status, svg.headers.get('content-type'), (await svg.text()).includes('M20 9v5M20 17.5v5M20 26v5')],
  ico: [ico.status, (await ico.arrayBuffer()).byteLength],
})
```

Pass: `links` is `['favicon.ico', 'favicon.svg']`; `svg` is `[200, 'image/svg+xml', true]`; `ico[0]` is `200` and `ico[1]` is not `15086` (the Angular file's size).

- [ ] **Step 7: Take the proof screenshots**

`resize_window {preset: "desktop"}`. After `await document.fonts.ready` plus about 1.5s, screenshot `/login` in Hebrew, `/login` in English, and `/students` in Hebrew. Then `resize_window {preset: "mobile"}` and screenshot `/login` in Hebrew. Compare each with the design's Option A: outlined blue tile with three dashes, blue bold name, gray tagline under it on sign-in. Reset with `resize_window {preset: "desktop"}`.

- [ ] **Step 8: Name the product in the living docs**

`README.md`, the first lines become:

```markdown
# Driving Lessons Booking

**Slotline** (סלוטליין in Hebrew) is a weekly demand collection system for a driving school. It replaces a ~3-hour manual WhatsApp-to-Excel ritual with a self-service flow: the admin publishes a weekly availability grid per teacher, students submit ranked session preferences through a shareable link, and the system produces the Excel file the teacher uses for booking.
```

`CLAUDE.md:3`:

```markdown
**Slotline** (סלוטליין) - weekly demand-collection system for a driving school (see [README.md](README.md) and [docs/requirements.md](docs/requirements.md)). The logo is Option A, Lane tile, from the Claude Design file `Slotline Logo Options.html`; the client draws it in `client\src\app\shared\brand-logo\`.
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
git add README.md CLAUDE.md client/src/app/shared/brand-logo/brand-logo.component.scss client/src/app/features/auth/login.component.scss
git commit -m "docs: name the product Slotline in README and CLAUDE.md"
git push -u origin slotline-branding
```

Open the PR into `main` with `gh pr create --base main --title "Slotline branding: lane-tile logo, wordmark, tagline, tab title and favicon"`. The body lists: the three surfaces and what changed on each; the decisions from the README (one key for the name, `--app-brand`, Hebrew tracking 0, SVG + ICO favicon); the step 3-6 measurement results per language and size; and that the screenshots were checked against Option A. End the body with:

```
🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Then bind the PR with the ccd_pr tools. The repo has no CI and auto-merge is off; merge by hand with `--merge` once it's reviewed.
