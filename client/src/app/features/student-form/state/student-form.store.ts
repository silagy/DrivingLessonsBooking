import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { GetPublicationByLinkResponse } from '../data/get-publication-by-link.response';
import { IdentifyStudentResponse } from '../data/identify-student.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { IdentifyStatus } from '../domain/identify-status.enum';
import { formatWindowInstant } from '../domain/jerusalem-time';
import { nameInitials } from '../domain/name-initials';
import { isCompleteNationalId, isNationalIdCandidate } from '../domain/national-id-input';
import { groupSlotsByDay } from '../domain/slot-day';
import { stepNumberOf, WIZARD_STEPS } from '../domain/student-form-step';
import { StudentFormStep } from '../domain/student-form-step.enum';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };

const CAPTION_KEY_BY_STEP: Record<StudentFormStep, string | null> = {
    [StudentFormStep.identify]: null,
    [StudentFormStep.details]: 'studentForm.weekCaption',
    [StudentFormStep.slots]: 'studentForm.weekTeacherCaption',
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
    readonly hasAvailability = computed(() => (this.student()?.slots.length ?? 0) > 0);
    readonly slotDays = computed(() => {
        const student = this.student();
        const publication = this.publication();

        return student && publication
            ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang(), [])
            : [];
    });

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

    continueToSlots(): void {
        if (!this.hasAvailability()) {
            return;
        }

        this.step.set(StudentFormStep.slots);
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
