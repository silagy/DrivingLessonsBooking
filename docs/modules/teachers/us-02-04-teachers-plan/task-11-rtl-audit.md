# Task 11 of 12: Hebrew/RTL audit

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 9–10 committed. Work on branch `3-us-02-04-teachers-module`.

## Shared Context

**Goal:** A feature is not done until verified in Hebrew — mirror-correct layout is part of acceptance. Hebrew is the default language, so most usage happens in RTL.

---

- [x] **Step 1: Static sweep of the new code**

Search every file added in tasks 8–10 for physical CSS properties — all of these must be zero hits (logical equivalents only):

```bash
grep -rnE "margin-(left|right)|padding-(left|right)|text-align: (left|right)|[^-](left|right):" client/src/app/features/teachers client/src/app/core/services client/src/app/shared/config client/src/app/features/admin-shell/admin-shell.component.scss
```

Fix any hit with the logical equivalent (`margin-inline-start`, `padding-inline-end`, `inset-inline-*`, `text-align: start`).

- [x] **Step 2: Runtime pass in Hebrew**

With API + client running, switch the language toggle to עברית and walk the teachers screen:

1. Topbar: logo at inline-start, nav flows right-to-left, active underline sits under the correct item.
2. Card grid mirrors; avatar at the inline-start of each card, Edit button at the inline-end.
3. Car rows: icon inline-start, transmission pill inline-end.
4. Emails render LTR inside the RTL layout without breaking (the `<bdi>` wrapper in the card and the `dir="ltr"` email input in the teacher dialog).
5. Dialogs: labels right-aligned naturally, action buttons at the inline-end, select opens correctly.
6. Toasts appear translated in Hebrew.

- [x] **Step 3: Missing-key check**

With the browser console open, walk every screen and dialog in both languages — zero Transloco missing-key warnings. Add any missing key to **both** `en.json` and `he.json`.

- [x] **Step 4: Commit (only if changes were needed)**

```bash
git add client
git commit -m "fix(client): rtl corrections for teachers feature"
```

---

**Next:** [task-12-manual-verification.md](task-12-manual-verification.md)
