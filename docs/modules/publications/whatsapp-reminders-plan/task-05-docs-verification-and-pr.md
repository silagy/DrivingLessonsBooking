# Task 5: Requirements, browser verification and the PR

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README's Review Focus first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** Task 4 committed.
**Scope guard:** `docs\requirements.md`, `docs\modules\publications\whatsapp-reminders-intent.md`, this plan's `README.md`. Code only for a defect this task finds; record each fix in the README's Manual verification section.

- [ ] **Step 5.1: Update the requirements** (intent §5):
- **§3 In scope:** after "Live admin dashboard during the submission window", add: ", listing the Students who haven't submitted yet, each with a WhatsApp reminder button".
- **§6.3:** a new bullet:
  - the school-wide list for Administrators (with a teacher filter) and the own-teacher list for Teacher-role Users;
  - who is listed (active, the current Teacher has a Week Schedule that week, no Submission in the Publication);
  - the click-to-chat link from the sender's own WhatsApp, with the fixed Hebrew message;
  - Israeli mobiles only;
  - nothing recorded.
- **§6.5 step 3:** add "and the teacher's own Students who haven't submitted, with their phones".
- **§11:** decision **#27**, "WhatsApp reminders are click-to-chat links from the sender's own WhatsApp; DriveCal records nothing". Rationale: "Students forget to submit; the list and the pre-written message replace writing to each one by hand, and need no Meta onboarding (the notifications module stays the automated route)".
- **Intent header:** `**Status:** Agreed intent, input to planning ([plan](whatsapp-reminders-plan/README.md))`.

- [ ] **Step 5.2: Start the app and open an Open week**

`preview_start {name: "api"}` and the client dev server, against the compose Postgres. Sign in as the dev Administrator. Use the Open Publication from Task 2. Have at least 2 Teachers with missing Students and one Student whose phone isn't a mobile; edit a phone on the Students screen if needed.

- [ ] **Step 5.3: Walk the success criteria (intent §6)** in Hebrew and English, light and dark:
1. Administrator: the list holds every Teacher's missing Students, ordered by Teacher then name. Changing the page's teacher picker doesn't change it. The filter narrows it to one Teacher and back.
2. Submit for one listed Student in another tab, then press Refresh. Expected: they're gone.
3. Read a row's `href`: decode `text` and compare it with intent §4.4. Expected: the Student's name, the week number and range, the Teacher, the close time in Asia/Jerusalem, and the weekly link. Open the link once to confirm WhatsApp (Web) shows the chat with the text. **Don't press send.**
4. Switch the UI to English. Expected: the decoded text is still Hebrew.
5. The non-mobile Student shows the phone and "המספר לא מתאים לוואטסאפ", with no button.
6. A Teacher-role User, if the dev data has one (create one on the Users screen with a Temporary Password from the project's seed or example config, linked to a Teacher, if none exists). Expected: only that Teacher's Students, with no teacher column and no filter. If there is no way to sign in as one, record it as not verified; `FindStudentsWithoutSubmissionInteractorTest` covers the scoping.
7. The Closed and Draft weeks show no section.
8. In the API log and `psql`, nothing is written when the list loads or a button is pressed.

- [ ] **Step 5.4: Phone width**

`resize_window {width: 390, height: 844}`, then `{width: 360, height: 780}`; reload after each; Hebrew and English. Run the [admin-mobile phone measurement](../../admin-mobile/admin-mobile-plan/README.md#the-phone-measurement) on `/publications`.
Expected: `pageOverflows: false`, `clippedAtInlineEdge: []`, `smallText: []`, and no new short targets. A long Hebrew name and teacher name (rename a dev Student if needed) wrap, and the WhatsApp button stays on its row.
Reset with `resize_window {preset: "desktop"}`.

- [ ] **Step 5.5: Full suites**

Run `dotnet test`, the client `ng test --watch=false` and the client `ng build --project client`.
Expected: 0 failures. Counts: Domain is Task 0's + 19; Application is Task 0's + 8; the client is Task 0's + 19.

- [ ] **Step 5.6: Fill in Manual verification** in the README: what was checked, the measurements table (page widths at 390 and 360, he and en), anything not verified and why, and any code fix this task made.

- [ ] **Step 5.7: Commit, push and open the PR**

```bash
git add docs/requirements.md docs/modules/publications/whatsapp-reminders-intent.md docs/modules/publications/whatsapp-reminders-plan/README.md
git commit -m "docs(publications): WhatsApp reminders in the requirements, decision 27"
git push -u origin whatsapp-reminders
```

Open the PR against `main` with the `mattpocock-skills:pr` skill. It links the intent and the plan, and lists what was not verified. Then bind it with the `ccd_pr` tools. Merge by hand with `--merge` once it is reviewed and CI is green.
