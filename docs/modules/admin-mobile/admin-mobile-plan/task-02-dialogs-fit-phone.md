# Task 2 of 7: Dialogs fit a phone

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires task 1 committed. Needs the Browser pane tools.

**Files:**
- Modify: `client\src\styles\_global.scss`
- Modify: `.claude\rules\client-primeng.md:139`

**Interfaces:**
- Consumes: PrimeNG's `.p-dialog` root, which `DialogService` opens with an inline `width` (28rem to 35rem).
- Produces: no class or API. Every later task's "Add" and "Edit" buttons open dialogs that now fit.

**Why:** the design's "Add car" / "Add teacher" buttons (frame 02) open 28rem (448px) dialogs, wider than a 390px phone. `client-primeng.md` already promises a global mobile override that doesn't exist. README decision 5 explains why the dialog fits the width instead of going full-screen.

- [ ] **Step 1: Measure the bug (RED)**

At 390 × 844 in Hebrew, on `/teachers`, tap "הוספת רכב". Then run:

```javascript
await new Promise((resolve) => setTimeout(resolve, 300));
const dialog = document.querySelector('.p-dialog').getBoundingClientRect();
({ left: Math.round(dialog.left), right: Math.round(dialog.right), innerWidth, fits: dialog.left >= 0 && dialog.right <= innerWidth });
```

Expected: `fits: false` (about 448px wide). Close the dialog with Escape.

- [ ] **Step 2: Cap the dialog**

At the end of `_global.scss` (already inside `@layer app` through `styles.scss`), add:

```scss
@media screen and (max-width: 639px) {
  .p-dialog {
    max-inline-size: calc(100vw - 2rem);
  }
}
```

- [ ] **Step 3: Measure four dialogs (GREEN)**

At 390 × 844, in Hebrew and then in English, open each dialog and run the step 1 snippet:
1. `/teachers`, "Add car".
2. `/teachers`, "Edit" on a teacher card. On the phone it's the pencil icon after task 3; before task 3, use the text button.
3. `/users`, "Add user" (33.75rem).
4. `/students`, "Add student" (35rem), the widest.
5. The avatar menu, "Change my password" (32.5rem).

Expected for each: `fits: true`, `left` 16 and `right` 374 (centered with a 1rem gutter). The form fields inside wrap to the dialog's width, with no horizontal scrollbar inside the dialog: `document.querySelector('.p-dialog-content').scrollWidth <= document.querySelector('.p-dialog-content').clientWidth`.

At 1280 × 900, open "Add car" once. Expected: the dialog is still 448px wide.

- [ ] **Step 4: Make the rule match**

In `client-primeng.md`, replace the line "On mobile widths the dialog goes full-screen (global override, not per-dialog)" with "Below 640px a dialog is capped at the viewport width less 2rem (one global `max-inline-size` override in `_global.scss`, not per-dialog; full-screen would need `!important` over PrimeNG's inline width)".

- [ ] **Step 5: Commit**

```bash
git add client/src/styles/_global.scss .claude/rules/client-primeng.md
git commit -m "feat(admin-mobile): dialogs fit the screen below 640px"
```

End the commit message with the attribution trailer from the session's instructions.
