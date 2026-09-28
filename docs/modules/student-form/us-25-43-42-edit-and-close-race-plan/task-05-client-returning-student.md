# Task 5 of 7: Client: a returning student's submission is loaded for editing (US-25, US-43)

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires task 4 committed. Work on branch `26-us-25-43-42-edit-and-close-race`. Client commands run from `client\`.

**Files:**
- Create: `client\src\app\features\student-form\domain\welcome-back.ts`
- Modify: `client\src\app\features\student-form\state\student-form.store.ts`
- Modify: `client\src\app\features\student-form\ui\components\identify-step\identify-step.component.ts` / `.html`
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` / `.html`
- Modify: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`

**Interfaces:**
- Consumes (task 4): `IdentifyStudentResponse.submission`, `loadedSubmissionOf(saved, slots)`, fixtures `SAVED_SUBMISSION` / `RETURNING_STUDENT` (target 2; `monday-noon` Double "only after 16:00", `sunday-afternoon`, `wednesday-evening`). Existing store: `student()`, `closesAt()`, `formatInstant`, `replacesEarlierSubmission()`, `target` / `chosenPicks` signals.
- Produces:
  - `WelcomeBack { pickCount: number; savedAt: string; closesAt: string }` (`domain\welcome-back.ts`); `StudentFormStore.welcomeBack: Signal<WelcomeBack | null>`, non-null only for an identified student with a saved submission.
  - `StudentFormStore.droppedPickCount: Signal<number>`: set when the student leaves the ID step, reset to 0 after a successful submit.
  - `StudentFormStore.submittedTitleKey: Signal<string>`: `studentForm.submitted.revisedTitle` after a `PUT`, `studentForm.submitted.title` after a `POST`.
  - Private `sentAsRevision` signal, set as each submit starts. Task 6 reads it for the window-closed body.
  - `IdentifyStepComponent.welcomeBack` input (required, `WelcomeBack | null`); `ReviewStepComponent.droppedPickCount` input (required, `number`).
  - Translation keys: `studentForm.identify.welcomeBackTitle` / `welcomeBackBodyOne` / `welcomeBackBodyMany` / `editSubmission`, `studentForm.review.droppedOne` / `droppedMany`, `studentForm.submitted.revisedTitle`.

Seeding happens in `continueToDetails()` and nowhere else (README decision 3). "Check the form again" reloads identify but must never overwrite edits in progress, and a student without a saved submission is always reset to target 1 with no picks.

- [ ] **Step 1: Failing page specs**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Next to `ROSTER_NATIONAL_ID` add:
   ```ts
   const FIRST_TIMER_NATIONAL_ID = '000000026';
   ```
2. **Delete** the whole test `it('replaces the earlier submission of a returning student', …)` inside `describe('review and submit')`. The new `describe` below replaces it with the loaded-list flow.
3. Add this block as the last child of `describe('StudentFormPage', …)`, after the closing `});` of `describe('review and submit', …)`:

```ts
    describe('returning student', () => {
        it('welcomes a returning student back and offers to edit the saved submission', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(textOf(fixture, '.identify__welcome-back')).toContain('studentForm.identify.welcomeBackTitle');
            expect(textOf(fixture, '.identify__welcome-back')).toContain('studentForm.identify.welcomeBackBodyMany');
            expect(page(fixture).querySelector('.identify__found')).toBeNull();
            expect(continueButton(fixture)!.textContent).toContain('studentForm.identify.editSubmission');
        });

        it('greets a first-time student without a welcome back', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(page(fixture).querySelector('.identify__welcome-back')).toBeNull();
            expect(page(fixture).querySelector('.identify__found')).not.toBeNull();
            expect(continueButton(fixture)!.textContent).toContain('studentForm.continue');
        });

        it('shows the roster details read-only before the saved list', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('app-details-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('app-details-step input, app-details-step select').length).toBe(0);
        });

        it('starts the target at the saved count', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await reachTarget(fixture);

            //then
            expect(textOf(fixture, '.target__count')).toBe('2');
        });

        it('shows the saved picks ranked on the grid with their session type and constraint', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'monday-noon');

            //then
            expect(rankOn(fixture, 'monday-noon')).toBe('1');
            expect(rankOn(fixture, 'sunday-afternoon')).toBe('2');
            expect(rankOn(fixture, 'wednesday-evening')).toBe('3');
            expect(isChecked(fixture, '.pick-sheet__double')).toBe(true);
            expect(constraintValue(fixture)).toBe('only after 16:00');
        });

        it('replaces the saved submission with the edited list and says the changes are saved', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await clickContinue(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await press(fixture, '.pick-sheet__remove button');
            await addPick(fixture, 'thursday-morning');
            await addPick(fixture, 'friday-noon');
            await clickContinue(fixture);
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;

            //when
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(createSubmission).not.toHaveBeenCalled();
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 3,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'wednesday-evening', sessionType: SessionType.single, constraint: null },
                    { slotId: 'thursday-morning', sessionType: SessionType.single, constraint: null },
                    { slotId: 'friday-noon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.submitted\.revisedTitle$/);
        });

        it('tells the student when saved picks are no longer offered and never sends them back', async () => {
            //given
            const reviseSubmission = accepting();
            const regridded = { ...RETURNING_STUDENT, slots: weekSlots(['monday-noon', 'wednesday-evening']) };
            provideOpenLinkSubmitting(identifyingAs(regridded), accepting(), reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            const survivingRank = rankOn(fixture, 'sunday-afternoon');
            await clickContinue(fixture);
            const droppedNotice = textOf(fixture, '.review__dropped');
            const notEnoughShown = page(fixture).querySelector('.review__not-enough') !== null;
            await press(fixture, '.review__add-slots button');
            await addPick(fixture, 'tuesday-evening');
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(survivingRank).toBe('1');
            expect(droppedNotice).toContain('studentForm.review.droppedMany');
            expect(notEnoughShown).toBe(true);
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                    { slotId: 'tuesday-evening', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('says nothing about dropped picks when every saved pick is still offered', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(3);
            expect(page(fixture).querySelector('.review__dropped')).toBeNull();
        });

        it('edits again from the confirmation any number of times, always replacing', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            await clickContinue(fixture);
            await clickContinue(fixture);

            //when
            await press(fixture, '.student-form__edit button');
            await press(fixture, '.wizard-step__back button');
            await tapChip(fixture, 'wednesday-evening');
            await press(fixture, '.pick-sheet__remove button');
            await clickContinue(fixture);
            await clickContinue(fixture);
            await press(fixture, '.student-form__edit button');
            await clickContinue(fixture);

            //then
            expect(createSubmission).not.toHaveBeenCalled();
            expect(reviseSubmission).toHaveBeenCalledTimes(3);
            expect(reviseSubmission).toHaveBeenLastCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.submitted\.revisedTitle$/);
        });

        it('keeps the edits in progress when the form is checked again', async () => {
            //given
            const reviseSubmission: SubmitCommand = vi
                .fn(accepting())
                .mockImplementationOnce(failingWith(HTTP_CONFLICT));
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), accepting(), reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'wednesday-evening');
            await press(fixture, '.pick-sheet__remove button');
            await clickContinue(fixture);
            await clickContinue(fixture);

            //when
            await press(fixture, '.review__recheck button');
            const reviewItemCount = page(fixture).querySelectorAll('.review__item').length;
            await clickContinue(fixture);

            //then
            expect(reviewItemCount).toBe(2);
            expect(reviseSubmission).toHaveBeenLastCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('starts fresh when a returning ID is replaced by a first-timer\'s', async () => {
            //given
            provideOpenLinkIdentifying(
                vi.fn((_token: string, request: IdentifyStudentRequest) =>
                    of(request.nationalId === ROSTER_NATIONAL_ID ? RETURNING_STUDENT : COHEN_STUDENT),
                ),
            );
            const fixture = await renderPage();
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //when
            await typeNationalId(fixture, FIRST_TIMER_NATIONAL_ID);
            await clickContinue(fixture);
            await clickContinue(fixture);
            const target = textOf(fixture, '.target__count');
            await clickContinue(fixture);

            //then
            expect(target).toBe('1');
            expect(page(fixture).querySelectorAll('.slot-chip__rank').length).toBe(0);
        });
    });
```

`weekSlots([...])` marks the listed slots Unavailable, which is how an admin change after submitting looks to identify. The first-timer's ID `000000026` has a valid check digit (the task-2 smoke's student B).

- [ ] **Step 2: Run the specs to see them fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL on eight of the eleven new tests, because nothing is loaded yet. `welcomes a returning student back…` finds no `.identify__welcome-back`. `starts the target at the saved count` reads `1`. `shows the saved picks ranked…` finds no rank badges. The flows that rely on the loaded list (`replaces the saved submission…`, `tells the student when saved picks…`, `says nothing about dropped picks…`, `edits again…`, `keeps the edits in progress…`) never reach the review with their expected picks. `greets a first-time student…`, `shows the roster details read-only…` and `starts fresh…` already PASS: they pin behaviour the seeding must not break. Every earlier test PASSES.

- [ ] **Step 3: The welcome-back view model**

Create `client\src\app\features\student-form\domain\welcome-back.ts`:

```ts
export interface WelcomeBack {
    pickCount: number;
    savedAt: string;
    closesAt: string;
}
```

- [ ] **Step 4: Seed the store and expose the new state**

In `client\src\app\features\student-form\state\student-form.store.ts`:

1. Imports. After `import { formatWindowInstant } from '../domain/jerusalem-time';` add
   ```ts
   import { loadedSubmissionOf } from '../domain/loaded-submission';
   ```
   and after `import { weekRangeLabel } from '../domain/week-label';` add
   ```ts
   import { WelcomeBack } from '../domain/welcome-back';
   ```
2. Constants. After `const SUBMITTED_BODY_MANY = 'studentForm.submitted.bodyMany';` add
   ```ts
   const SUBMITTED_TITLE = 'studentForm.submitted.title';
   const REVISED_TITLE = 'studentForm.submitted.revisedTitle';
   ```
3. Signals. After `private readonly submittedThisVisit = signal(false);` add
   ```ts
    private readonly droppedPicks = signal(0);
    private readonly sentAsRevision = signal(false);
   ```
4. After `readonly hasAvailability = computed(() => hasOpenSlot(this.student()?.slots ?? []));` add
   ```ts
    readonly welcomeBack = computed<WelcomeBack | null>(() => {
        const saved = this.student()?.submission;

        if (!saved) {
            return null;
        }

        return {
            pickCount: saved.slotRequests.length,
            savedAt: this.formatInstant(saved.lastSavedAtUtc),
            closesAt: this.closesAt(),
        };
    });
   ```
5. After `readonly submitStatus = this.submitState.asReadonly();` add
   ```ts
    readonly droppedPickCount = this.droppedPicks.asReadonly();
   ```
6. After the `submittedBodyKey` computed add
   ```ts
    readonly submittedTitleKey = computed(() => (this.sentAsRevision() ? REVISED_TITLE : SUBMITTED_TITLE));
   ```
7. Replace the whole `continueToDetails()` method with
   ```ts
    continueToDetails(): void {
        const student = this.student();

        if (!student) {
            return;
        }

        const loaded = loadedSubmissionOf(student.submission, student.slots);
        this.target.set(loaded.targetCount);
        this.chosenPicks.set(loaded.picks);
        this.droppedPicks.set(loaded.droppedPickCount);
        this.step.set(StudentFormStep.details);
    }
   ```
8. In `submit()`, replace
   ```ts
        const command = this.replacesEarlierSubmission()
            ? this.api.reviseSubmission(token, request)
            : this.api.createSubmission(token, request);

        this.submitState.set(SubmitStatus.submitting);

        try {
            await firstValueFrom(command);
            this.submittedThisVisit.set(true);
            this.submitState.set(SubmitStatus.idle);
   ```
   with
   ```ts
        const isRevision = this.replacesEarlierSubmission();
        const command = isRevision
            ? this.api.reviseSubmission(token, request)
            : this.api.createSubmission(token, request);

        this.sentAsRevision.set(isRevision);
        this.submitState.set(SubmitStatus.submitting);

        try {
            await firstValueFrom(command);
            this.submittedThisVisit.set(true);
            this.droppedPicks.set(0);
            this.submitState.set(SubmitStatus.idle);
   ```

`recheck()` is deliberately untouched. It reloads identify (which now carries the saved list) without re-seeding, so the edits on screen stay. The dropped count resets after a successful submit because the list just sent no longer contains those picks, and editing from the confirmation must not repeat the notice.

- [ ] **Step 5: Welcome back on the ID step**

In `client\src\app\features\student-form\ui\components\identify-step\identify-step.component.ts`:

1. Add the import after the `IdentifyStatus` import:
   ```ts
   import { WelcomeBack } from '../../../domain/welcome-back';
   ```
2. After `const REJECTED_STATUSES … ;` add
   ```ts
   const SINGLE_PICK = 1;
   ```
3. After `readonly weekNumber = input.required<number>();` add
   ```ts
    readonly welcomeBack = input.required<WelcomeBack | null>();
   ```
4. After the `isRejected` computed add
   ```ts
    protected readonly welcomeBackBodyKey = computed(() =>
        this.welcomeBack()?.pickCount === SINGLE_PICK
            ? 'studentForm.identify.welcomeBackBodyOne'
            : 'studentForm.identify.welcomeBackBodyMany',
    );
    protected readonly continueKey = computed(() =>
        this.welcomeBack() ? 'studentForm.identify.editSubmission' : 'studentForm.continue',
    );
   ```

In `identify-step.component.html`:

1. Replace the whole `@case (statuses.found) { … }` block with
   ```html
                @case (statuses.found) {
                    @if (welcomeBack(); as returning) {
                        <p-message severity="success" class="identify__welcome-back">
                            <span>
                                <strong>{{ 'studentForm.identify.welcomeBackTitle' | transloco: { studentName: studentName() } }}</strong>
                                {{
                                    welcomeBackBodyKey()
                                        | transloco
                                            : {
                                                  pickCount: returning.pickCount,
                                                  savedAt: returning.savedAt,
                                                  closesAt: returning.closesAt,
                                              }
                                }}
                            </span>
                        </p-message>
                    } @else {
                        <p-message severity="success" class="identify__found">
                            <span>
                                <strong>{{ 'studentForm.identify.foundTitle' | transloco: { studentName: studentName() } }}</strong>
                                {{
                                    'studentForm.identify.foundBody'
                                        | transloco: { weekNumber: weekNumber(), teacherName: teacherName() }
                                }}
                            </span>
                        </p-message>
                    }
                }
   ```
2. Replace the footer button
   ```html
            <p-button type="submit" fluid [label]="'studentForm.continue' | transloco" [disabled]="!canSubmit()" />
   ```
   with
   ```html
            <p-button type="submit" fluid [label]="continueKey() | transloco" [disabled]="!canSubmit()" />
   ```

The params are spelled out as an object literal: Transloco's params type is an index-signature `HashMap`, and an `interface` such as `WelcomeBack` is not assignable to one under strict templates. The banner sits inside `.identify__result`, so the existing `.identify__result .p-message-leave-active { animation: none; }` rule already stops a stale welcome-back from lingering when the ID is edited (the slice-2 lesson). No SCSS change.

- [ ] **Step 6: Dropped-picks notice on the review**

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts`:

1. After `const SINGLE_PREFERRED_TARGET = 1;` add
   ```ts
   const SINGLE_DROPPED_PICK = 1;
   ```
2. After `readonly replacesEarlierSubmission = input.required<boolean>();` add
   ```ts
    readonly droppedPickCount = input.required<number>();
   ```
3. After the `preferredKey` computed add
   ```ts
    protected readonly droppedKey = computed(() =>
        this.droppedPickCount() === SINGLE_DROPPED_PICK ? 'studentForm.review.droppedOne' : 'studentForm.review.droppedMany',
    );
   ```

In `review-step.component.html`, directly after the `@if (replacesEarlierSubmission()) { … }` block add
```html
        @if (droppedPickCount() > 0) {
            <p-message severity="warn" class="review__dropped">
                {{ droppedKey() | transloco: { count: droppedPickCount() } }}
            </p-message>
        }
```

- [ ] **Step 7: Wire the page**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`:

1. In `<app-identify-step …>`, after `[weekNumber]="store.weekParams().weekNumber"` add
   ```html
                        [welcomeBack]="store.welcomeBack()"
   ```
2. In `<app-review-step …>`, after `[replacesEarlierSubmission]="store.replacesEarlierSubmission()"` add
   ```html
                        [droppedPickCount]="store.droppedPickCount()"
   ```
3. In the `@case (steps.done)` block, replace
   ```html
                        [heading]="'studentForm.submitted.title' | transloco">
   ```
   with
   ```html
                        [heading]="store.submittedTitleKey() | transloco">
   ```

- [ ] **Step 8: Translations (both files, same commit)**

`client\public\i18n\en.json`, inside `studentForm`:

1. In `identify`, replace
   ```json
      "failed": "We couldn't check your ID right now. Check your connection and try again."
   ```
   with
   ```json
      "failed": "We couldn't check your ID right now. Check your connection and try again.",
      "welcomeBackTitle": "Welcome back, {{studentName}}.",
      "welcomeBackBodyOne": "You already sent 1 pick on {{savedAt}}, so we've loaded it. You can edit your submission until the window closes ({{closesAt}}).",
      "welcomeBackBodyMany": "You already sent {{pickCount}} picks on {{savedAt}}, so we've loaded them. You can edit your submission until the window closes ({{closesAt}}).",
      "editSubmission": "Edit my submission"
   ```
2. In `review`, replace
   ```json
      "replaces": "You already sent a list for this week. Submitting now replaces it.",
   ```
   with
   ```json
      "replaces": "You already sent a list for this week. Submitting now replaces it.",
      "droppedOne": "1 of your earlier picks is no longer offered this week, so we took it off your list.",
      "droppedMany": "{{count}} of your earlier picks are no longer offered this week, so we took them off your list.",
   ```
3. In `submitted`, replace
   ```json
      "title": "Submitted",
   ```
   with
   ```json
      "title": "Submitted",
      "revisedTitle": "Changes saved",
   ```

`client\public\i18n\he.json`, inside `studentForm`:

1. In `identify`, replace
   ```json
      "failed": "לא הצלחנו לבדוק את תעודת הזהות כרגע. בדקו את החיבור ונסו שוב."
   ```
   with
   ```json
      "failed": "לא הצלחנו לבדוק את תעודת הזהות כרגע. בדקו את החיבור ונסו שוב.",
      "welcomeBackTitle": "ברוכים השבים, {{studentName}}.",
      "welcomeBackBodyOne": "כבר שלחתם בחירה אחת ב{{savedAt}}, אז טענו אותה. אפשר לערוך את ההגשה עד שהחלון ייסגר ({{closesAt}}).",
      "welcomeBackBodyMany": "כבר שלחתם {{pickCount}} בחירות ב{{savedAt}}, אז טענו אותן. אפשר לערוך את ההגשה עד שהחלון ייסגר ({{closesAt}}).",
      "editSubmission": "עריכת ההגשה"
   ```
2. In `review`, replace
   ```json
      "replaces": "כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.",
   ```
   with
   ```json
      "replaces": "כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.",
      "droppedOne": "אחת הבחירות הקודמות שלכם כבר לא זמינה השבוע, אז הסרנו אותה מהרשימה.",
      "droppedMany": "{{count}} מהבחירות הקודמות שלכם כבר לא זמינות השבוע, אז הסרנו אותן מהרשימה.",
   ```
3. In `submitted`, replace
   ```json
      "title": "נשלח",
   ```
   with
   ```json
      "title": "נשלח",
      "revisedTitle": "השינויים נשמרו",
   ```

`{{savedAt}}` renders through `formatWindowInstant` (weekday, date, time in Asia/Jerusalem), so the Hebrew prefix `ב` reads `ביום חמישי, 12 בנוב׳, 10:30`. The indentation in both files is whatever the file already uses (match the surrounding lines). Validate both files parse: `node -e "for (const f of ['public/i18n/en.json','public/i18n/he.json']) JSON.parse(require('fs').readFileSync(f,'utf8'))"` (in `client\`) must print nothing.

- [ ] **Step 9: Run everything**

Run (in `client\`): `npm test -- --watch=false` then `npm run build`
Expected: every spec PASSES, including all eleven `returning student` tests and the untouched `review and submit` tests (a first-timer still gets `studentForm.submitted.title`). The build is clean with no new warnings.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/student-form/domain/welcome-back.ts client/src/app/features/student-form/state/student-form.store.ts client/src/app/features/student-form/ui/components/identify-step/identify-step.component.ts client/src/app/features/student-form/ui/components/identify-step/identify-step.component.html client/src/app/features/student-form/ui/components/review-step/review-step.component.ts client/src/app/features/student-form/ui/components/review-step/review-step.component.html client/src/app/features/student-form/ui/pages/student-form/student-form.page.html client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): returning students edit their loaded submission

Entering the national ID of a student who already submitted shows a
welcome back with Edit my submission and loads the saved target and
ranked picks into the wizard. Picks whose slot is no longer offered are
dropped with a notice. Every resubmit replaces the list and the
confirmation says the changes are saved."
```

---

**Next:** [task-06-client-window-closed-screen.md](task-06-client-window-closed-screen.md)
