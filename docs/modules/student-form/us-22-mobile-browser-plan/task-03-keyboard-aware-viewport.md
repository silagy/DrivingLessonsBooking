# Task 3 of 4: The on-screen keyboard shrinks the layout on Android (D3)

> Part of [US-22: Student Form Fully Usable on a Mobile Browser](README.md). Requires tasks 1–2 committed. Work on branch `23-us-22-mobile-browser`.

**Files:**
- Modify: `client\src\index.html` (the viewport `<meta>`)

**Interfaces:**
- Consumes: the existing fixed pick sheet (`:host { position: fixed; inset: 0; justify-content: flex-end }`, `.pick-sheet { max-height: 90dvh; overflow-y: auto }`), the sticky wizard footer (`.wizard-step__footer { position: sticky; inset-block-end: 0 }`) and the shell's `min-height: 100dvh`. None of them change.
- Produces: `<meta name="viewport" content="width=device-width, initial-scale=1, interactive-widget=resizes-content">`. Task 4 Step 2.3 checks it in the served page, and task 4 Step 7.3 checks the behavior on a real Android phone.

**Why:** README defect D3 and decision 4. Since Chrome 108, Android Chrome's default is `interactive-widget=resizes-visual`: the keyboard covers the page, but the layout viewport, `position: fixed` boxes and `dvh` keep their full height. When a student taps the constraint textarea, the browser pans so the textarea shows, while the sheet's Save button (at the bottom of the fixed sheet) and the sticky footer stay behind the keyboard until the student closes it. `resizes-content` shrinks the layout viewport to the area above the keyboard. The fixed sheet's `inset: 0` box then ends at the keyboard's top edge, `90dvh` caps the sheet to that height (it scrolls inside if needed), and Save sits in view. The sticky footer behaves the same on the ID step. iOS Safari ignores the key (open item 2), and desktop browsers have no on-screen keyboard, so the admin is unaffected. `initial-scale=1` and the absence of `maximum-scale` / `user-scalable=no` keep pinch-zoom available, which matters for accessibility.

**Test approach:** neither jsdom nor the browser pane has an on-screen keyboard. The build check below proves the meta ships, task 4 Step 2.3 proves the served page carries it, and task 4 Step 7 is the human-run check on a phone.

- [ ] **Step 1: Extend the viewport meta**

In `client\src\index.html`, change

```html
  <meta name="viewport" content="width=device-width, initial-scale=1">
```

to

```html
  <meta name="viewport" content="width=device-width, initial-scale=1, interactive-widget=resizes-content">
```

Keep the file's 2-space indentation.

- [ ] **Step 2: Run the specs and the build, and check the built page**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

Then (in `client\`):

```bash
grep -o 'content="[^"]*interactive-widget[^"]*"' dist/client/browser/index.html
```

Expected: exactly `content="width=device-width, initial-scale=1, interactive-widget=resizes-content"`.

- [ ] **Step 3: Commit**

```bash
git add client/src/index.html
git commit -m "fix(student-form): keep the pick sheet and footer above the Android keyboard

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
