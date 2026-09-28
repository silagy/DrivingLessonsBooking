import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { CreateSubmissionRequest } from '../data/create-submission.request';
import { GetPublicationByLinkResponse } from '../data/get-publication-by-link.response';
import { IdentifyStudentResponse } from '../data/identify-student.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { IdentifyStatus } from '../domain/identify-status.enum';
import { formatWindowInstant } from '../domain/jerusalem-time';
import { nameInitials } from '../domain/name-initials';
import { isCompleteNationalId, isNationalIdCandidate } from '../domain/national-id-input';
import { PickSheet, pickSheetFor } from '../domain/pick-sheet';
import { reviewItemsOf } from '../domain/review-item';
import { groupSlotsByDay, hasOpenSlot } from '../domain/slot-day';
import { PickChoice, removePick, SlotPick, upsertPick } from '../domain/slot-pick';
import { previousStepOf, stepNumberOf, WIZARD_STEPS } from '../domain/student-form-step';
import { StudentFormStep } from '../domain/student-form-step.enum';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { SubmitStatus } from '../domain/submit-status.enum';
import { MIN_TARGET_COUNT, missingPickCount } from '../domain/target-count';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };
const SINGLE_PICK = 1;
const SUBMITTED_BODY_ONE = 'studentForm.submitted.bodyOne';
const SUBMITTED_BODY_MANY = 'studentForm.submitted.bodyMany';
const SUBMIT_LOCKING_STATUSES: ReadonlySet<SubmitStatus> = new Set([
    SubmitStatus.submitting,
    SubmitStatus.rejected,
    SubmitStatus.notFound,
]);

const CAPTION_KEY_BY_STEP: Record<StudentFormStep, string | null> = {
    [StudentFormStep.identify]: null,
    [StudentFormStep.details]: 'studentForm.weekCaption',
    [StudentFormStep.target]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.slots]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.review]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.done]: null,
};

interface IdentifyLookup {
    token: string;
    nationalId: string;
}

type IdentifyResult =
    | { status: IdentifyStatus.found; student: IdentifyStudentResponse }
    | { status: IdentifyStatus.notOnRoster | IdentifyStatus.invalidId };

@Injectable()
export class StudentFormStore {
    private readonly api = inject(SubmissionsApiService);
    private readonly language = inject(LanguageService);

    private readonly linkToken = signal<string | null>(null);
    private readonly nationalId = signal<string | null>(null);
    private readonly step = signal(StudentFormStep.identify);
    private readonly target = signal(MIN_TARGET_COUNT);
    private readonly picks = signal<SlotPick[]>([]);
    private readonly openSlotId = signal<string | null>(null);
    private readonly submitState = signal(SubmitStatus.idle);
    private readonly submittedThisVisit = signal(false);

    private readonly publicationResource = resource({
        params: () => this.linkToken() ?? undefined,
        loader: ({ params: token }) => this.loadByLink(token),
    });

    private readonly identifyLookup = computed<IdentifyLookup | undefined>(() => {
        const token = this.linkToken();
        const nationalId = this.nationalId();

        return token && nationalId ? { token, nationalId } : undefined;
    });

    private readonly identifyResource = resource({
        params: () => this.identifyLookup(),
        loader: ({ params: lookup }) => this.identify(lookup),
    });

    private readonly publication = computed<GetPublicationByLinkResponse | null>(() =>
        this.publicationResource.hasValue() ? this.publicationResource.value() : null,
    );

    private readonly identifyResult = computed<IdentifyResult | null>(() =>
        this.identifyResource.hasValue() ? this.identifyResource.value() : null,
    );

    readonly view = computed<StudentFormView>(() => {
        if (!this.linkToken() || this.publicationResource.isLoading()) {
            return StudentFormView.loading;
        }

        if (this.publicationResource.error()) {
            return StudentFormView.loadFailed;
        }

        const publication = this.publication();

        return publication ? viewForPublicationState(publication.state) : StudentFormView.invalidLink;
    });

    readonly isOpen = computed(() => this.view() === StudentFormView.open);

    readonly weekParams = computed(() => {
        const publication = this.publication();

        if (!publication) {
            return EMPTY_WEEK_PARAMS;
        }

        return {
            weekNumber: publication.weekNumber,
            weekRange: weekRangeLabel(publication.weekStart, this.language.lang()),
        };
    });

    readonly opensAt = computed(() => this.formatInstant(this.publication()?.windowStartUtc));
    readonly closesAt = computed(() => this.formatInstant(this.publication()?.windowEndUtc));

    readonly identifyStatus = computed<IdentifyStatus>(() => {
        if (!this.identifyLookup()) {
            return IdentifyStatus.idle;
        }

        if (this.identifyResource.isLoading()) {
            return IdentifyStatus.checking;
        }

        if (this.identifyResource.error()) {
            return IdentifyStatus.failed;
        }

        return this.identifyResult()?.status ?? IdentifyStatus.checking;
    });

    readonly student = computed<IdentifyStudentResponse | null>(() => {
        const result = this.identifyResult();
        const isFound = this.identifyStatus() === IdentifyStatus.found;

        return isFound && result?.status === IdentifyStatus.found ? result.student : null;
    });

    readonly studentName = computed(() => this.student()?.studentName ?? '');
    readonly teacherName = computed(() => this.student()?.teacherName ?? '');
    readonly teacherInitials = computed(() => nameInitials(this.teacherName()));
    readonly hasAvailability = computed(() => hasOpenSlot(this.student()?.slots ?? []));
    readonly slotDays = computed(() => {
        const student = this.student();
        const publication = this.publication();

        return student && publication
            ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang(), this.picks())
            : [];
    });

    readonly targetCount = this.target.asReadonly();
    readonly minTargetCount = MIN_TARGET_COUNT;
    readonly pickCount = computed(() => this.picks().length);
    readonly pickSheet = computed<PickSheet | null>(() => {
        const slotId = this.openSlotId();
        const slot = this.student()?.slots.find(x => x.id === slotId);

        return slot ? pickSheetFor(slot, this.picks()) : null;
    });
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

    readonly currentStep = this.step.asReadonly();
    readonly stepNumber = computed(() => stepNumberOf(this.step()));
    readonly stepCount = WIZARD_STEPS.length;

    readonly captionKey = computed(() => (this.isOpen() ? CAPTION_KEY_BY_STEP[this.step()] : null));
    readonly captionParams = computed(() => ({ ...this.weekParams(), teacherName: this.teacherName() }));

    open(token: string): void {
        this.linkToken.set(token);
    }

    retry(): void {
        this.publicationResource.reload();
    }

    changeNationalId(digits: string): void {
        const lookup = isCompleteNationalId(digits) ? digits : null;
        this.nationalId.set(lookup);
    }

    requestLookup(digits: string): void {
        if (!isNationalIdCandidate(digits)) {
            return;
        }

        if (digits === this.nationalId()) {
            this.identifyResource.reload();
            return;
        }

        this.nationalId.set(digits);
    }

    continueToDetails(): void {
        if (!this.student()) {
            return;
        }

        this.step.set(StudentFormStep.details);
    }

    continueToTarget(): void {
        if (!this.hasAvailability()) {
            return;
        }

        this.step.set(StudentFormStep.target);
    }

    continueToSlots(): void {
        if (!this.hasAvailability()) {
            return;
        }

        this.step.set(StudentFormStep.slots);
    }

    increaseTarget(): void {
        this.target.update(count => count + 1);
    }

    decreaseTarget(): void {
        this.target.update(count => Math.max(MIN_TARGET_COUNT, count - 1));
    }

    openPick(slotId: string): void {
        const slot = this.student()?.slots.find(x => x.id === slotId);

        if (slot?.state !== SlotState.open) {
            return;
        }

        this.openSlotId.set(slotId);
    }

    savePick(choice: PickChoice): void {
        const slotId = this.openSlotId();

        if (!slotId) {
            return;
        }

        this.picks.update(picks => upsertPick(picks, { slotId, ...choice }));
        this.openSlotId.set(null);
    }

    removeOpenPick(): void {
        const slotId = this.openSlotId();

        if (!slotId) {
            return;
        }

        this.picks.update(picks => removePick(picks, slotId));
        this.openSlotId.set(null);
    }

    closePick(): void {
        this.openSlotId.set(null);
    }

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

    goBack(): void {
        const previous = previousStepOf(this.step());

        if (!previous) {
            return;
        }

        this.step.set(previous);
    }

    private formatInstant(utcIso: string | undefined): string {
        return utcIso ? formatWindowInstant(utcIso, this.language.lang()) : '';
    }

    private async loadByLink(token: string): Promise<GetPublicationByLinkResponse | null> {
        try {
            return await firstValueFrom(this.api.getPublicationByLink(token));
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return null;
            }

            throw error;
        }
    }

    private async identify(lookup: IdentifyLookup): Promise<IdentifyResult> {
        try {
            const request = { nationalId: lookup.nationalId };
            const student = await firstValueFrom(this.api.identifyStudent(lookup.token, request));

            return { status: IdentifyStatus.found, student };
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return { status: IdentifyStatus.notOnRoster };
            }

            if (isStatus(error, HTTP_CONFLICT)) {
                return { status: IdentifyStatus.invalidId };
            }

            throw error;
        }
    }
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}

function submitFailureOf(error: unknown): SubmitStatus {
    if (isStatus(error, HTTP_CONFLICT)) {
        return SubmitStatus.rejected;
    }

    if (isStatus(error, HTTP_NOT_FOUND)) {
        return SubmitStatus.notFound;
    }

    return SubmitStatus.failed;
}
