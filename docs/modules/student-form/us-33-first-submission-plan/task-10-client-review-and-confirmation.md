# Task 10 of 11: Client — ranked review, validation message, submit (POST or PUT), confirmation (US-39, US-40, US-41)

> Part of [US-33…US-41: First Submission](README.md). Requires task 9 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Create: `ui\components\review-step\review-step.component.ts`, `.html`, `.scss`
- Modify: `state\student-form.store.ts`
- Modify: `ui\pages\student-form\student-form.page.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 7): `SubmissionsApiService.createSubmission` / `reviseSubmission`, `CreateSubmissionRequest`, `IdentifyStudentResponse.hasSubmission`, `ReviewItem`, `reviewItemsOf`, `missingPickCount`, `SubmitStatus`, `SessionType`; (task 8): `StudentFormStep.review` / `done`, `goBack()`, `WizardStepComponent` Back; (task 9): the store's private `picks`, `pickCount`, `SlotsStepComponent.reviewed`, page-spec helpers `reachTarget`, `press`, `textOf`, `tapChip`, `addPick`, `chooseDouble`, `typeConstraint`.
- Produces:
  - `ReviewStepComponent` (`app-review-step`): inputs `stepNumber`, `stepCount`, `targetCount`, `pickCount`, `items`, `missingPicks`, `replacesEarlierSubmission`, `submitStatus`, `canSubmit`; outputs `submitted`, `addSlots`, `changeTarget`, `recheck`, `back`. Classes `.review__item(--preferred)`, `.review__not-enough`, `.review__add-slots`, `.review__change-target`, `.review__replaces`, `.review__rejected`, `.review__recheck`, `.review__not-found`, `.review__failed`, `.review__unlock`.
  - `StudentFormStore`: `reviewItems`, `missingPicks`, `submitStatus`, `replacesEarlierSubmission`, `canSubmit`, `submittedBodyKey`, `submittedParams`, `continueToReview()`, `changeTarget()`, `submit()`, `editSubmission()`, `recheck()`.
  - Confirmation view (`steps.done`): `.status__heading` = `studentForm.submitted.title`, `.student-form__submitted`, `.student-form__edit`.
  - Translation keys `studentForm.review.*`, `studentForm.submitted.*`.

Precedents: slice 2's `identify-step` (status-driven `p-message`s, the `.p-message-leave-active { animation: none }` fix), slice 1's `app-status-message` views, mockup `student.jsx` → `SReview` / `PickRow` (rank circle — gradient inside the target, grey beyond; "Day · Window" + time; session-type pill; constraint in quotes), `SReviewErr` ("Not enough picks." box, "ADD MORE SLOTS", disabled SUBMIT + "Submit unlocks at 2+ picks"), `SDone` (check circle, "Submitted", "…edit anytime before …", "EDIT MY SUBMISSION").

**Behavior** (README decisions 2, 9, 10, 17; CLAUDE.md rule 12):
- **Picks ≥ target** is mirrored for immediate feedback only: when the list is short the review shows the mockup's message with the exact shortfall, Submit is disabled with "Submit unlocks once you have N picks", and two ways out — **Add more slots** (back to the grid, picks kept) and **Change my target** (**Review Focus 8**: a target far above what was picked is fixed in one tap). The backend still decides (task 2); a list the client lets through is never assumed valid.
- **POST or PUT** comes from the identify response's `hasSubmission` (roadmap decision 2), or from a successful submit earlier in this visit. A returning student starts from an empty list in this slice (slice 5 loads their picks), so the review warns "You already sent a list for this week — submitting now replaces it."
- **Outcomes by status only** (the client never renders backend text): 204 → confirmation; **409** → "we couldn't save your list — the window may have closed, or this week's slots changed" + **Check the form again**, which re-loads the link: a closed window then shows slice 1's closed screen (**Review Focus 4**; slice 5 adds the dedicated mockup `SDoneErr` screen); **404** → "we couldn't find your roster details or this week's slots anymore — contact your school" (e.g. removed from the roster, **Review Focus 6**); anything else → "couldn't send, try again" with Submit re-enabled. A 409 or 404 locks Submit — retrying cannot change the answer (client-architecture "never retry 409").
- **No double submit**: Submit shows PrimeNG's loading state and is disabled while the request is in flight.
- **Confirmation** (US-41): "N picks for week W with Teacher. You can edit them until {close} — just reopen this link and enter your national ID." **Edit my submission** returns to the review with the list intact; the next submit is a `PUT`.
- The national ID is sent from memory (the store's `nationalId` signal), in the body only.

- [ ] **Step 1: Write the failing page-spec cases**

In `ui\pages\student-form\student-form.page.spec.ts`:

1. Add the imports:
   ```typescript
   import { CreateSubmissionRequest } from '../../../data/create-submission.request';
   ```
   after the `GetPublicationByLinkResponse` import, and
   ```typescript
   import { SessionType } from '../../../domain/session-type.enum';
   ```
   before the `Transmission` import.

2. Replace the `type IdentifyStudent = …;` line and the `FakeSubmissionsApi` interface with:

```typescript
type IdentifyStudent = (token: string, request: IdentifyStudentRequest) => Observable<IdentifyStudentResponse>;
type SubmitCommand = (token: string, request: CreateSubmissionRequest) => Observable<void>;

interface FakeSubmissionsApi {
    getPublicationByLink: () => Observable<GetPublicationByLinkResponse>;
    identifyStudent: IdentifyStudent;
    createSubmission: SubmitCommand;
    reviseSubmission: SubmitCommand;
}
```

3. Add after `function answer(…) { … }`:

```typescript

function accepting(): SubmitCommand {
    return vi.fn(() => of(undefined));
}
```

4. Replace `provideOpenLinkIdentifying` with:

```typescript
function provideOpenLinkIdentifying(identifyStudent: IdentifyStudent): void {
    provideOpenLinkSubmitting(identifyStudent, accepting(), accepting());
}

function provideOpenLinkSubmitting(
    identifyStudent: IdentifyStudent,
    createSubmission: SubmitCommand,
    reviseSubmission: SubmitCommand,
): void {
    provideApi({
        getPublicationByLink: publicationIn(PublicationState.open),
        identifyStudent,
        createSubmission,
        reviseSubmission,
    });
}
```

5. In the first test (`renders the $view view read-only`), replace its `provideApi({ … })` line with:

```typescript
        provideApi({
            getPublicationByLink: load,
            identifyStudent: identifyingAs(COHEN_STUDENT),
            createSubmission: accepting(),
            reviseSubmission: accepting(),
        });
```

6. Add this helper after `constraintValue`:

```typescript

async function reachReview(
    fixture: ComponentFixture<StudentFormPage>,
    targetCount: number,
    slotIds: readonly string[],
): Promise<void> {
    await reachTarget(fixture);

    for (let count = 1; count < targetCount; count++) {
        await press(fixture, '.target__increase');
    }

    await clickContinue(fixture);

    for (const slotId of slotIds) {
        await addPick(fixture, slotId);
    }

    await clickContinue(fixture);
}
```

7. Add this `describe` block after the `picking slots` block:

```typescript
    describe('review and submit', () => {
        it('lists the picks in rank order, the ones inside the target as preferred', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //then
            expect(page(fixture).querySelector('app-review-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(3);
            expect(page(fixture).querySelectorAll('.review__item--preferred').length).toBe(2);
            expect(page(fixture).querySelector('.review__not-enough')).toBeNull();
            expect(page(fixture).querySelector('.review__replaces')).toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(false);
        });

        it('submits the ranked list with each pick\'s session type and constraint', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await clickContinue(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await typeConstraint(fixture, '  only after 16:00 ');
            await press(fixture, '.pick-sheet__save button');
            await addPick(fixture, 'monday-evening');
            await addPick(fixture, 'wednesday-afternoon');
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'sunday-afternoon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'monday-evening', sessionType: SessionType.single, constraint: null },
                    { slotId: 'wednesday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(reviseSubmission).not.toHaveBeenCalled();
        });

        it('blocks a list shorter than the target with a clear message', async () => {
            //given
            const createSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon', 'monday-evening']);

            //when
            continueButton(fixture)!.click();
            await fixture.whenStable();

            //then
            expect(page(fixture).querySelector('.review__not-enough')).not.toBeNull();
            expect(page(fixture).querySelector('.review__unlock')).not.toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
            expect(createSubmission).not.toHaveBeenCalled();
        });

        it('lets the student lower the target from the not-enough message', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon']);

            //when
            await press(fixture, '.review__change-target button');

            //then
            expect(page(fixture).querySelector('app-target-step')).not.toBeNull();
            expect(textOf(fixture, '.target__count')).toBe('3');
        });

        it('sends the student back to the grid for more slots, keeping the picks', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon']);

            //when
            await press(fixture, '.review__add-slots button');

            //then
            expect(page(fixture).querySelector('app-slots-step')).not.toBeNull();
            expect(rankOn(fixture, 'sunday-afternoon')).toBe('1');
        });

        it('confirms the submission and explains self-service editing', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.status__heading')).toBe('studentForm.submitted.title');
            expect(textOf(fixture, '.student-form__submitted')).toContain('studentForm.submitted.bodyMany');
            expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
        });

        it('edits from the confirmation by replacing the submission', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);

            //when
            await press(fixture, '.student-form__edit button');
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(reviseSubmission).toHaveBeenCalledTimes(1);
        });

        it('replaces the earlier submission of a returning student', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            const returning = { ...COHEN_STUDENT, hasSubmission: true };
            provideOpenLinkSubmitting(identifyingAs(returning), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(reviseSubmission).toHaveBeenCalledTimes(1);
            expect(createSubmission).not.toHaveBeenCalled();
        });

        it.each([
            { status: HTTP_CONFLICT, message: '.review__rejected', canRetry: false },
            { status: HTTP_NOT_FOUND, message: '.review__not-found', canRetry: false },
            { status: HTTP_SERVER_ERROR, message: '.review__failed', canRetry: true },
        ])('keeps the list on screen when the submit answers $status', async ({ status, message, canRetry }) => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), failingWith(status), accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector(message)).not.toBeNull();
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(1);
            expect(continueButton(fixture)!.disabled).toBe(!canRetry);
        });

        it('checks the form again after a rejection and shows the window has closed', async () => {
            //given
            const link = { state: PublicationState.open };
            provideApi({
                getPublicationByLink: () => publicationIn(link.state)(),
                identifyStudent: identifyingAs(COHEN_STUDENT),
                createSubmission: failingWith(HTTP_CONFLICT),
                reviseSubmission: accepting(),
            });
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);
            link.state = PublicationState.closed;

            //when
            await press(fixture, '.review__recheck button');

            //then
            expect(textOf(fixture, '.status__heading')).toBe('studentForm.closed.title');
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
        });

        it('never submits twice while a submission is in flight', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(() => new Subject<void>());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(continueButton(fixture)!.disabled).toBe(true);
        });
    });
```

What these pin: US-39 (three picks against target 2 go out as ranks 1–3 — the list order is the rank), US-35/US-36/US-37 end to end in the request body (the constraint trimmed, `null` when blank), US-40 (short list blocked with the message, nothing sent), US-41 (confirmation + edit), **Review Focus 4** (rejection → re-check → closed screen), **Review Focus 5** (a returning student and an in-visit edit are `PUT`s, never a second `POST`), **Review Focus 6** (404 → not-found message, Submit locked), **Review Focus 8** (change the target from the message), and the double-tap guard.

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — "Review my list" is not wired, there is no `app-review-step`, and the store has no `submit`.

- [ ] **Step 2: Translations (en + he, same commit)**

In **both** files, add `"review"` and `"submitted"` objects inside `"studentForm"`, directly after `"pick"`.

`client\public\i18n\en.json`:

```json
    "review": {
      "title": "Your ranked list",
      "preferredOne": "Rank 1 is your preferred slot; any others are backups.",
      "preferredMany": "Ranks 1–{{target}} are your preferred slots; the rest are backups.",
      "notEnoughTitle": "Not enough picks.",
      "notEnoughBody": "Your target is {{target}}, but you've picked {{picked}}. Add {{missing}} more, or lower your target.",
      "addSlots": "Add more slots",
      "changeTarget": "Change my target",
      "replaces": "You already sent a list for this week. Submitting now replaces it.",
      "submit": "Submit",
      "unlockHint": "Submit unlocks once you have {{target}} picks.",
      "recheck": "Check the form again",
      "errors": {
        "rejected": "We couldn't save your list — the submission window may have closed, or this week's slots changed.",
        "notFound": "We couldn't find your roster details or this week's slots anymore. Please contact your school.",
        "failed": "We couldn't send your list. Check your connection and try again."
      }
    },
    "submitted": {
      "title": "Submitted",
      "bodyOne": "1 pick for week {{weekNumber}} with {{teacherName}}. You can edit it until {{closesAt}} — just reopen this link and enter your national ID.",
      "bodyMany": "{{count}} picks for week {{weekNumber}} with {{teacherName}}. You can edit them until {{closesAt}} — just reopen this link and enter your national ID.",
      "edit": "Edit my submission"
    }
```

`client\public\i18n\he.json`:

```json
    "review": {
      "title": "הרשימה המדורגת שלכם",
      "preferredOne": "דירוג 1 הוא השעה המועדפת שלכם; כל השאר הן גיבוי.",
      "preferredMany": "דירוגים 1–{{target}} הם השעות המועדפות שלכם; השאר הן גיבוי.",
      "notEnoughTitle": "אין מספיק בחירות.",
      "notEnoughBody": "היעד שלכם הוא {{target}}, אבל בחרתם {{picked}}. הוסיפו עוד {{missing}}, או הורידו את היעד.",
      "addSlots": "הוספת שעות",
      "changeTarget": "שינוי היעד",
      "replaces": "כבר שלחתם רשימה לשבוע הזה. שליחה עכשיו תחליף אותה.",
      "submit": "שליחה",
      "unlockHint": "השליחה תיפתח כשיהיו לכם {{target}} בחירות.",
      "recheck": "בדיקת הטופס מחדש",
      "errors": {
        "rejected": "לא הצלחנו לשמור את הרשימה — ייתכן שחלון ההגשה נסגר או שהשעות של השבוע השתנו.",
        "notFound": "לא מצאנו יותר את הפרטים שלכם ברשימת התלמידים או את השעות של השבוע. פנו לבית הספר.",
        "failed": "לא הצלחנו לשלוח את הרשימה. בדקו את החיבור ונסו שוב."
      }
    },
    "submitted": {
      "title": "נשלח",
      "bodyOne": "בחירה אחת לשבוע {{weekNumber}} אצל {{teacherName}}. אפשר לערוך אותה עד {{closesAt}} — פשוט פתחו שוב את הקישור והזינו את מספר תעודת הזהות.",
      "bodyMany": "{{count}} בחירות לשבוע {{weekNumber}} אצל {{teacherName}}. אפשר לערוך אותן עד {{closesAt}} — פשוט פתחו שוב את הקישור והזינו את מספר תעודת הזהות.",
      "edit": "עריכת ההגשה"
    }
```

Singular/plural are separate keys chosen in the store (client-i18n "explicit singular/plural keys selected in code"). `unlockHint` only shows when the list is short, i.e. the target is at least 2. Session-type pills reuse `studentForm.pick.single` / `.double`; the slot title reuses `studentForm.slotLabel`. Verify parity with the task-8 `node -e …` command → `[] []`.

- [ ] **Step 3: `ReviewStepComponent` (mockup `SReview` / `SReviewErr`)**

`ui\components\review-step\review-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ReviewItem } from '../../../domain/review-item';
import { SessionType } from '../../../domain/session-type.enum';
import { SubmitStatus } from '../../../domain/submit-status.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const SINGLE_PREFERRED_TARGET = 1;

@Component({
    selector: 'app-review-step',
    imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent],
    templateUrl: './review-step.component.html',
    styleUrl: './review-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly targetCount = input.required<number>();
    readonly pickCount = input.required<number>();
    readonly items = input.required<readonly ReviewItem[]>();
    readonly missingPicks = input.required<number>();
    readonly replacesEarlierSubmission = input.required<boolean>();
    readonly submitStatus = input.required<SubmitStatus>();
    readonly canSubmit = input.required<boolean>();

    readonly submitted = output<void>();
    readonly addSlots = output<void>();
    readonly changeTarget = output<void>();
    readonly recheck = output<void>();
    readonly back = output<void>();

    protected readonly statuses = SubmitStatus;
    protected readonly sessionTypes = SessionType;
    protected readonly isSubmitting = computed(() => this.submitStatus() === SubmitStatus.submitting);
    protected readonly preferredKey = computed(() =>
        this.targetCount() === SINGLE_PREFERRED_TARGET
            ? 'studentForm.review.preferredOne'
            : 'studentForm.review.preferredMany',
    );
}
```

`ui\components\review-step\review-step.component.html`:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.review.title' | transloco"
    [canGoBack]="true"
    (back)="back.emit()">
    <div class="review">
        @if (missingPicks() > 0) {
            <p-message severity="error" class="review__not-enough">
                <span>
                    <strong>{{ 'studentForm.review.notEnoughTitle' | transloco }}</strong>
                    {{
                        'studentForm.review.notEnoughBody'
                            | transloco: { target: targetCount(), picked: pickCount(), missing: missingPicks() }
                    }}
                </span>
            </p-message>
            <div class="review__fixes">
                <p-button
                    type="button"
                    class="review__add-slots"
                    severity="secondary"
                    fluid
                    [outlined]="true"
                    [label]="'studentForm.review.addSlots' | transloco"
                    (onClick)="addSlots.emit()" />
                <p-button
                    type="button"
                    class="review__change-target"
                    [text]="true"
                    [label]="'studentForm.review.changeTarget' | transloco"
                    (onClick)="changeTarget.emit()" />
            </div>
        } @else {
            <p class="review__summary">
                <strong>{{ 'studentForm.slots.summary' | transloco: { target: targetCount(), picked: pickCount() } }}</strong>
                {{ preferredKey() | transloco: { target: targetCount() } }}
            </p>
        }

        @if (replacesEarlierSubmission()) {
            <p-message severity="info" class="review__replaces">
                {{ 'studentForm.review.replaces' | transloco }}
            </p-message>
        }

        <ol class="review__list">
            @for (item of items(); track item.slotId) {
                @let dayName = 'weekGrid.days.' + item.day | transloco;
                @let windowName = 'weekGrid.windows.' + item.window | transloco;
                <li class="review__item" [class.review__item--preferred]="item.isPreferred">
                    <span class="review__rank" aria-hidden="true">{{ item.rank }}</span>
                    <div class="review__details">
                        <p class="review__slot">
                            {{ 'studentForm.slotLabel' | transloco: { day: dayName, window: windowName } }}
                            <span class="review__time" dir="ltr">{{ item.timeLabel }}</span>
                        </p>
                        <p class="review__meta">
                            <span
                                class="review__type"
                                [class.review__type--double]="item.sessionType === sessionTypes.double">
                                {{ 'studentForm.pick.' + item.sessionType | transloco }}
                            </span>
                            @if (item.constraint) {
                                <q class="review__constraint">{{ item.constraint }}</q>
                            }
                        </p>
                    </div>
                </li>
            }
        </ol>

        @switch (submitStatus()) {
            @case (statuses.rejected) {
                <p-message severity="error" class="review__rejected">
                    {{ 'studentForm.review.errors.rejected' | transloco }}
                </p-message>
                <p-button
                    type="button"
                    class="review__recheck"
                    severity="secondary"
                    [outlined]="true"
                    [label]="'studentForm.review.recheck' | transloco"
                    (onClick)="recheck.emit()" />
            }
            @case (statuses.notFound) {
                <p-message severity="error" class="review__not-found">
                    {{ 'studentForm.review.errors.notFound' | transloco }}
                </p-message>
            }
            @case (statuses.failed) {
                <p-message severity="warn" class="review__failed">
                    {{ 'studentForm.review.errors.failed' | transloco }}
                </p-message>
            }
        }
    </div>

    <div wizardFooter>
        <p-button
            type="button"
            fluid
            [label]="'studentForm.review.submit' | transloco"
            [disabled]="!canSubmit()"
            [loading]="isSubmitting()"
            (onClick)="submitted.emit()" />
        @if (missingPicks() > 0) {
            <small class="review__unlock">{{ 'studentForm.review.unlockHint' | transloco: { target: targetCount() } }}</small>
        }
    </div>
</app-wizard-step>
```

`ui\components\review-step\review-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.review {
    display: flex;
    flex-direction: column;
    gap: 0.625rem;
}

.review .p-message-leave-active {
    animation: none;
}

.review__summary {
    margin: 0;
    font-size: 0.8rem;
    line-height: 1.5;
    color: var(--app-text-secondary);

    strong {
        color: var(--app-ink);
    }
}

.review__fixes {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.25rem;
}

.review__list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 0;
    padding: 0;
    list-style: none;
}

.review__item {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    padding: 0.7rem 0.75rem;
    border: 1.5px solid var(--app-border);
    border-radius: 10px;
    background: var(--app-bg-card);
    box-shadow: var(--app-shadow-card);
}

.review__item--preferred {
    border-color: var(--p-sky-500);
}

.review__rank {
    display: flex;
    flex: none;
    align-items: center;
    justify-content: center;
    width: 1.3rem;
    height: 1.3rem;
    border-radius: 999px;
    background: var(--app-border);
    color: var(--app-text-secondary);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.7rem;
}

.review__item--preferred .review__rank {
    background: var(--app-grad-sky);
    color: var(--app-on-accent);
}

.review__details {
    min-width: 0;
}

.review__slot {
    margin: 0;
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--app-ink);
}

.review__time {
    margin-inline-start: 0.25rem;
    font-size: 0.72rem;
    font-weight: 400;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}

.review__meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.375rem;
    margin: 0.25rem 0 0;
}

.review__type {
    padding: 0.125rem 0.5rem;
    border: 1px solid var(--app-border);
    border-radius: 999px;
    background: var(--app-bg-muted);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.62rem;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    color: var(--app-text-secondary);
}

.review__type--double {
    background: var(--p-sky-50);
    color: var(--p-sky-700);
}

.review__constraint {
    font-size: 0.7rem;
    font-style: italic;
    color: var(--app-text-secondary);
    overflow-wrap: anywhere;
}

.review__unlock {
    display: block;
    text-align: center;
    font-size: 0.7rem;
    color: var(--app-text-muted);
}
```

Notes for the implementer:
- `.review .p-message-leave-active { animation: none; }` is the slice-2 fix (identify step) applied here: PrimeNG 21's `p-message` keeps a leaving message on screen for its 150ms `animate.leave` fade, so without it "Not enough picks" or a stale error could linger over the next state.
- `<q>` lets the browser draw the constraint's quotation marks for the page language (`lang` is set by `LanguageService`) — no hardcoded quote glyphs; `overflow-wrap: anywhere` keeps a 200-character constraint without spaces inside the card at 375px (**Review Focus 7**).
- The `<ol>` gives screen readers "1 of 5" semantics; the visible rank circle is `aria-hidden`. No drag handle — reorder is slice 4.
- Ranks inside the target get the gradient circle and sky border (mockup `PickRow top`); the rest are grey backups.

- [ ] **Step 4: Store — review, submit, confirmation**

In `state\student-form.store.ts`:

1. Add the imports (sorted by path):
   ```typescript
   import { CreateSubmissionRequest } from '../data/create-submission.request';
   ```
   before the `GetPublicationByLinkResponse` import;
   ```typescript
   import { reviewItemsOf } from '../domain/review-item';
   ```
   after the `pick-sheet` import;
   ```typescript
   import { SubmitStatus } from '../domain/submit-status.enum';
   ```
   after the `student-form-view.enum` import; and change the `target-count` import to:
   ```typescript
   import { MIN_TARGET_COUNT, missingPickCount } from '../domain/target-count';
   ```
2. Add these constants after `const EMPTY_WEEK_PARAMS = …;`:
   ```typescript
   const SINGLE_PICK = 1;
   const SUBMITTED_BODY_ONE = 'studentForm.submitted.bodyOne';
   const SUBMITTED_BODY_MANY = 'studentForm.submitted.bodyMany';
   const SUBMIT_LOCKING_STATUSES: ReadonlySet<SubmitStatus> = new Set([
       SubmitStatus.submitting,
       SubmitStatus.rejected,
       SubmitStatus.notFound,
   ]);
   ```
3. Add these private signals after `private readonly openSlotId = …;`:
   ```typescript
       private readonly submitState = signal(SubmitStatus.idle);
       private readonly submittedThisVisit = signal(false);
   ```
4. Add these computeds directly after the `pickSheet` computed:
   ```typescript
       readonly reviewItems = computed(() =>
           reviewItemsOf(this.picks(), this.student()?.slots ?? [], this.target()),
       );
       readonly missingPicks = computed(() => missingPickCount(this.target(), this.pickCount()));
       readonly submitStatus = this.submitState.asReadonly();
       readonly replacesEarlierSubmission = computed(
           () => (this.student()?.hasSubmission ?? false) || this.submittedThisVisit(),
       );
       readonly canSubmit = computed(
           () =>
               this.pickCount() > 0 && this.missingPicks() === 0 && !SUBMIT_LOCKING_STATUSES.has(this.submitState()),
       );
       readonly submittedBodyKey = computed(() =>
           this.pickCount() === SINGLE_PICK ? SUBMITTED_BODY_ONE : SUBMITTED_BODY_MANY,
       );
       readonly submittedParams = computed(() => ({
           count: this.pickCount(),
           weekNumber: this.weekParams().weekNumber,
           teacherName: this.teacherName(),
           closesAt: this.closesAt(),
       }));
   ```
5. Add these methods directly after `closePick()`:
   ```typescript

       continueToReview(): void {
           if (!this.pickCount()) {
               return;
           }

           this.submitState.set(SubmitStatus.idle);
           this.step.set(StudentFormStep.review);
       }

       changeTarget(): void {
           this.step.set(StudentFormStep.target);
       }

       async submit(): Promise<void> {
           const token = this.linkToken();
           const nationalId = this.nationalId();

           if (!token || !nationalId || !this.canSubmit()) {
               return;
           }

           const request: CreateSubmissionRequest = {
               nationalId,
               targetCount: this.target(),
               slotRequests: this.picks().map(pick => ({
                   slotId: pick.slotId,
                   sessionType: pick.sessionType,
                   constraint: pick.constraint,
               })),
           };
           const command = this.replacesEarlierSubmission()
               ? this.api.reviseSubmission(token, request)
               : this.api.createSubmission(token, request);

           this.submitState.set(SubmitStatus.submitting);

           try {
               await firstValueFrom(command);
               this.submittedThisVisit.set(true);
               this.submitState.set(SubmitStatus.idle);
               this.step.set(StudentFormStep.done);
           } catch (error) {
               this.submitState.set(submitFailureOf(error));
           }
       }

       editSubmission(): void {
           this.step.set(StudentFormStep.review);
       }

       recheck(): void {
           this.submitState.set(SubmitStatus.idle);
           this.publicationResource.reload();
       }
   ```
6. Add this function after the existing `isStatus` function at the bottom of the file:
   ```typescript

   function submitFailureOf(error: unknown): SubmitStatus {
       if (isStatus(error, HTTP_CONFLICT)) {
           return SubmitStatus.rejected;
       }

       if (isStatus(error, HTTP_NOT_FOUND)) {
           return SubmitStatus.notFound;
       }

       return SubmitStatus.failed;
   }
   ```

Notes for the implementer:
- `submit` is an `async` store method (client-state "writes → async store methods"); the command observable is cold, so choosing `create…` / `revise…` before flipping the status sends nothing twice.
- `recheck` reloads the link context: while it loads the page shows the spinner (the wizard state stays in the store), and the view then follows the publication — `closed` shows slice 1's closed screen; still `open` returns to the review with the list intact and Submit enabled again. The identify lookup is **not** re-run (same token + ID).
- The `CreateSubmissionRequest` shape is also a valid `ReviseSubmissionRequest` (identical members, task 7), so one object serves both calls.
- No toast: every outcome renders inline on the review step (the student surface has no toast host by design).

- [ ] **Step 5: Wire the page — review and confirmation**

In `ui\pages\student-form\student-form.page.ts`, add after the `IdentifyStepComponent` import:

```typescript
import { ReviewStepComponent } from '../../components/review-step/review-step.component';
```

and add `ReviewStepComponent` to the `imports` array after `SlotsStepComponent`.

In `ui\pages\student-form\student-form.page.html`:

1. On `<app-slots-step …>`, add after `(pickCancelled)="store.closePick()"`:
   ```html
                        (reviewed)="store.continueToReview()"
   ```
2. Add these cases after the closing `}` of `@case (steps.slots) { … }` (inside the step `@switch`):
   ```html
                @case (steps.review) {
                    <app-review-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [targetCount]="store.targetCount()"
                        [pickCount]="store.pickCount()"
                        [items]="store.reviewItems()"
                        [missingPicks]="store.missingPicks()"
                        [replacesEarlierSubmission]="store.replacesEarlierSubmission()"
                        [submitStatus]="store.submitStatus()"
                        [canSubmit]="store.canSubmit()"
                        (submitted)="store.submit()"
                        (addSlots)="store.goBack()"
                        (changeTarget)="store.changeTarget()"
                        (recheck)="store.recheck()"
                        (back)="store.goBack()" />
                }
                @case (steps.done) {
                    <app-status-message
                        icon="pi pi-check"
                        tone="success"
                        [heading]="'studentForm.submitted.title' | transloco">
                        <p class="student-form__text student-form__submitted">
                            {{ store.submittedBodyKey() | transloco: store.submittedParams() }}
                        </p>
                        <p-button
                            type="button"
                            class="student-form__edit"
                            severity="secondary"
                            [outlined]="true"
                            [label]="'studentForm.submitted.edit' | transloco"
                            (onClick)="store.editSubmission()" />
                    </app-status-message>
                }
   ```

In `ui\pages\student-form\student-form.page.scss`, add at the end:

```scss

.student-form__submitted {
    max-width: 18rem;
}
```

The confirmation reuses slice 1's `app-status-message` with its `success` tone (mockup `SDone`: check circle, title, one short paragraph, a secondary button); the caption bar is hidden on `done` (task 8's caption map — mockup `noSub`).

- [ ] **Step 6: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — including the thirteen review-and-submit cases (11 tests, one of them three data rows).

Then `npm run build` → success, no new warnings.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/features/student-form client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): review, submit and confirm the weekly submission

The student reviews their ranked list, is stopped with a clear message
when it is shorter than their target, and submits; a returning student
replaces their earlier list. The confirmation explains that the
submission can be edited through the same link and national ID until
the window closes."
```

---

**Next:** [task-11-verification-and-pr.md](task-11-verification-and-pr.md)
