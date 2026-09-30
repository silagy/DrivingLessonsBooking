# Task 6 of 7: Client: "The window just closed" when the window closed between load and submit (US-42)

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires task 5 committed. Work on branch `26-us-25-43-42-edit-and-close-race`. Client commands run from `client\`.

**Files:**
- Modify: `client\src\app\features\student-form\state\student-form.store.ts`
- Modify: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html` / `.scss`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`

**Interfaces:**
- Consumes (task 4): `isWindowClosedProblem(error)`, `SUBMISSION_WINDOW_CLOSED_PROBLEM`, `StudentFormStep.windowClosed` (caption `null`, unnumbered). From task 5: the private `sentAsRevision` signal, set as each submit starts. Existing: `captionParams()` (`weekNumber`, `weekRange`, `teacherName`), `submitFailureOf`, `app-status-message` (`icon`, `tone`, `heading`).
- Produces:
  - `StudentFormStore.closedMidSubmitBodyKey: Signal<string>`: `studentForm.closedMidSubmit.bodyChanges` when the refused command was a `PUT`, otherwise `…bodyNew`.
  - Submit outcome: a window-closed problem moves the wizard to `windowClosed` (mockup `SDoneErr`). Every other failure keeps slice 3's `SubmitStatus` mapping (`rejected` / `notFound` / `failed`) on the review step.
  - Translation keys `studentForm.closedMidSubmit.title` / `bodyNew` / `bodyChanges` / `hint`, and a reworded `studentForm.review.errors.rejected`.

The screen has no action, which matches the mockup: the attempt is over, and a reopened window is reached by opening the link again. It shows no time (README decision 8). The shell's language toggle stays available, as on every status screen.

- [ ] **Step 1: Failing page specs**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Add the import (next to the other `../../../data/…` imports):
   ```ts
   import { SUBMISSION_WINDOW_CLOSED_PROBLEM } from '../../../data/problem-types';
   ```
2. After the `failingWith` helper add:
   ```ts
   function closingWindow(): () => Observable<never> {
       return () =>
           throwError(
               () =>
                   new HttpErrorResponse({
                       status: HTTP_CONFLICT,
                       error: {
                           type: SUBMISSION_WINDOW_CLOSED_PROBLEM,
                           title: 'Conflict',
                           status: HTTP_CONFLICT,
                           detail: "Submissions are accepted only while the week's submission window is open.",
                       },
                   }),
           );
   }
   ```
3. Add this block as the last child of `describe('StudentFormPage', …)`, after `describe('returning student', …)`:

```ts
    describe('window closed mid-submit', () => {
        it('tells a first-time student the window just closed and that their list was not saved', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(closingWindow());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.closedMidSubmit\.title$/);
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain('studentForm.closedMidSubmit.bodyNew');
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
            expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
            expect(page(fixture).querySelectorAll('app-status-message button').length).toBe(0);
        });

        it('tells a returning student their changes were not saved and the earlier list stands', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), accepting(), closingWindow());
            const fixture = await renderPage();
            await reachSlots(fixture);
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.closedMidSubmit\.title$/);
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain(
                'studentForm.closedMidSubmit.bodyChanges',
            );
        });

        it('says the edit was not saved when the window closes during an edit from the confirmation', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), accepting(), closingWindow());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);
            await press(fixture, '.student-form__edit button');

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain(
                'studentForm.closedMidSubmit.bodyChanges',
            );
        });

        it('still offers a recheck for any other rejection', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), failingWith(HTTP_CONFLICT), accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('.review__rejected')).not.toBeNull();
            expect(page(fixture).querySelector('.review__recheck')).not.toBeNull();
            expect(page(fixture).querySelector('.student-form__closed-mid-submit')).toBeNull();
        });
    });
```

- [ ] **Step 2: Run the specs to see them fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: the first three new tests FAIL. The window-closed 409 still lands on the review's `.review__rejected`, so `.status__heading` is absent. `still offers a recheck…` PASSES already, and so does every earlier test.

- [ ] **Step 3: Route the window-closed problem to its own step**

In `client\src\app\features\student-form\state\student-form.store.ts`:

1. After `import { IdentifyStudentResponse } from '../data/identify-student.response';` add
   ```ts
   import { isWindowClosedProblem } from '../data/problem-types';
   ```
2. After `const REVISED_TITLE = 'studentForm.submitted.revisedTitle';` add
   ```ts
   const CLOSED_MID_SUBMIT_BODY_NEW = 'studentForm.closedMidSubmit.bodyNew';
   const CLOSED_MID_SUBMIT_BODY_CHANGES = 'studentForm.closedMidSubmit.bodyChanges';
   ```
3. After the `submittedTitleKey` computed add
   ```ts
    readonly closedMidSubmitBodyKey = computed(() =>
        this.sentAsRevision() ? CLOSED_MID_SUBMIT_BODY_CHANGES : CLOSED_MID_SUBMIT_BODY_NEW,
    );
   ```
4. In `submit()`, replace
   ```ts
        } catch (error) {
            this.submitState.set(submitFailureOf(error));
        }
   ```
   with
   ```ts
        } catch (error) {
            if (isWindowClosedProblem(error)) {
                this.submitState.set(SubmitStatus.idle);
                this.step.set(StudentFormStep.windowClosed);
                return;
            }

            this.submitState.set(submitFailureOf(error));
        }
   ```

The link resource is deliberately **not** reloaded. A reload would flip `view` to slice 1's generic closed screen, and US-42 wants the specific "while you had this page open" message.

- [ ] **Step 4: Render the screen**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`, directly after the closing `}` of the `@case (steps.done) { … }` block (still inside the inner `@switch (store.currentStep())`), add:

```html
                @case (steps.windowClosed) {
                    <app-status-message
                        icon="pi pi-exclamation-circle"
                        tone="danger"
                        [heading]="'studentForm.closedMidSubmit.title' | transloco">
                        <p class="student-form__text student-form__closed-mid-submit">
                            {{ store.closedMidSubmitBodyKey() | transloco: store.captionParams() }}
                        </p>
                        <p class="student-form__hint">{{ 'studentForm.closedMidSubmit.hint' | transloco }}</p>
                    </app-status-message>
                }
```

In `student-form.page.scss`, replace

```scss
.student-form__submitted {
    max-width: 18rem;
}
```

with

```scss
.student-form__submitted,
.student-form__closed-mid-submit {
    max-width: 18rem;
}
```

- [ ] **Step 5: Translations (both files, same commit)**

`client\public\i18n\en.json`, inside `studentForm`:

1. In `review.errors`, replace
   ```json
        "rejected": "We couldn't save your list — the submission window may have closed, or this week's slots changed.",
   ```
   with
   ```json
        "rejected": "We couldn't save your list — something changed since you opened the form, for example a slot is no longer available.",
   ```
2. At the end of `submitted`, replace
   ```json
      "edit": "Edit my submission"
    }
   ```
   with
   ```json
      "edit": "Edit my submission"
    },
    "closedMidSubmit": {
      "title": "The window just closed",
      "bodyNew": "Submissions for week {{weekNumber}} closed while you had this page open. Your list was not saved. If it's urgent, contact {{teacherName}} directly.",
      "bodyChanges": "Submissions for week {{weekNumber}} closed while you had this page open. Your changes were not saved — the list you sent earlier stays as it was. If it's urgent, contact {{teacherName}} directly.",
      "hint": "If the school reopens the window, this link will work again."
    }
   ```

`client\public\i18n\he.json`, inside `studentForm`:

1. In `review.errors`, replace
   ```json
        "rejected": "לא הצלחנו לשמור את הרשימה — ייתכן שחלון ההגשה נסגר או שהשעות של השבוע השתנו.",
   ```
   with
   ```json
        "rejected": "לא הצלחנו לשמור את הרשימה — משהו השתנה מאז שפתחתם את הטופס, למשל שעה שכבר לא זמינה.",
   ```
2. At the end of `submitted`, replace
   ```json
      "edit": "עריכת ההגשה"
    }
   ```
   with
   ```json
      "edit": "עריכת ההגשה"
    },
    "closedMidSubmit": {
      "title": "החלון נסגר ממש עכשיו",
      "bodyNew": "ההגשה לשבוע {{weekNumber}} נסגרה בזמן שהדף היה פתוח אצלכם. הרשימה שלכם לא נשמרה. אם זה דחוף, פנו ישירות אל {{teacherName}}.",
      "bodyChanges": "ההגשה לשבוע {{weekNumber}} נסגרה בזמן שהדף היה פתוח אצלכם. השינויים שלכם לא נשמרו — הרשימה ששלחתם קודם נשארה כפי שהייתה. אם זה דחוף, פנו ישירות אל {{teacherName}}.",
      "hint": "אם בית הספר יפתח את החלון מחדש, הקישור יעבוד שוב."
    }
   ```

The `"edit": …` line (key `edit`, not task 5's `editSubmission`) is unique in each file. Match the files' existing indentation. `submitted` is the last key of `studentForm` today; if it no longer is, put `closedMidSubmit` directly after it anyway and mind the commas. Validate: `node -e "for (const f of ['public/i18n/en.json','public/i18n/he.json']) JSON.parse(require('fs').readFileSync(f,'utf8'))"` (in `client\`) prints nothing.

- [ ] **Step 6: Run everything**

Run (in `client\`): `npm test -- --watch=false` then `npm run build`
Expected: every spec PASSES, including the four `window closed mid-submit` tests and slice 3's `checks the form again after a rejection and shows the window has closed`. That test uses a type-less 409, so it still takes the recheck path to slice 1's closed screen. The build is clean with no new warnings.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/features/student-form/state/student-form.store.ts client/src/app/features/student-form/ui/pages/student-form/student-form.page.html client/src/app/features/student-form/ui/pages/student-form/student-form.page.scss client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): say so when the window closed while the form was open

A submit refused because the window closed now ends on its own screen:
the window just closed, nothing was saved, and for an edit the earlier
list stands. Other rejections keep the inline message and Check the
form again."
```

---

**Next:** [task-07-verification-and-pr.md](task-07-verification-and-pr.md)
