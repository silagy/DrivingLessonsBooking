import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { LanguageService } from '../../../core/language.service';
import { formatInstantInJerusalem } from '../domain/jerusalem-time';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { ExtendPublicationWindowRequest } from '../data/extend-publication-window.request';
import { GetPublicationDashboardResponse } from '../data/get-publication-dashboard.response';
import { GetPublicationResponse } from '../data/get-publication.response';
import { ItemForFindPublicationHistoryResponse } from '../data/item-for-find-publication-history.response';
import { PublicationsApiService } from '../data/publications-api.service';
import { PublishPublicationRequest } from '../data/publish-publication.request';
import { ReopenPublicationRequest } from '../data/reopen-publication.request';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { SlotCountForGetPublicationDashboardResponse } from '../data/get-publication-dashboard.response';
import { TeacherOption } from '../domain/teacher-option.model';
import { buildWeekOptions, WeekOption } from '../domain/week-options';

const HTTP_NOT_FOUND = 404;
const STUDENT_LINK_PREFIX = '/s/';

@Injectable({ providedIn: 'root' })
export class PublicationsStore {
    private readonly api = inject(PublicationsApiService);
    private readonly teachersApi = inject(TeacherOptionsApiService);
    private readonly toast = inject(ToastService);
    private readonly language = inject(LanguageService);
    private readonly clipboard = inject(ClipboardService);
    private readonly fileDownload = inject(FileDownloadService);
    private readonly document = inject(DOCUMENT);

    private readonly selectedTeacherIdState = signal<string | null>(null);
    private readonly selectedWeekStartState = signal<string>(defaultWeekStart());
    private readonly mutating = signal(false);
    private readonly loadedAtState = signal<string | null>(null);

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teachersApi.findTeachers()),
    });

    private readonly publicationResource = resource({
        params: () => ({ weekStart: this.selectedWeekStartState() }),
        loader: ({ params }) => this.loadByWeek(params.weekStart),
    });

    private readonly dashboardResource = resource({
        params: () => {
            const publication = this.publicationResource.value();
            const teacherId = this.selectedTeacherIdState();

            return publication && teacherId ? { publicationId: publication.id, teacherId } : undefined;
        },
        loader: async ({ params }) => {
            const dashboard = await firstValueFrom(
                this.api.getDashboard(params.publicationId, params.teacherId),
            );
            this.loadedAtState.set(new Date().toISOString());

            return dashboard;
        },
    });

    private readonly historyResource = resource({
        loader: () => firstValueFrom(this.api.findHistory()),
    });

    readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();
    readonly selectedWeekStart = this.selectedWeekStartState.asReadonly();
    readonly isMutating = this.mutating.asReadonly();

    readonly history = computed<ItemForFindPublicationHistoryResponse[]>(() => this.historyResource.value() ?? []);
    readonly historyIsLoading = this.historyResource.isLoading;
    readonly historyError = computed(() => (this.historyResource.error() ? 'publications.history.loadFailed' : null));
    readonly historyIsEmpty = computed(() => !this.historyIsLoading() && this.history().length === 0);

    readonly teachers = computed<TeacherOption[]>(() => {
        const items = this.teachersResource.value() ?? [];

        return items
            .map((item) => ({ id: item.id, name: item.name }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });

    readonly weekOptions = computed<WeekOption[]>(() => buildWeekOptions(this.language.lang()));

    readonly weekLabel = computed<string>(() => {
        const weekStart = this.selectedWeekStartState();

        return this.weekOptions().find((option) => option.weekStart === weekStart)?.label ?? '';
    });

    readonly publication = computed<GetPublicationResponse | undefined>(() => this.publicationResource.value());

    readonly state = computed<PublicationState | undefined>(() => this.publication()?.state);

    readonly shareLink = computed<string>(() => {
        const token = this.publication()?.linkToken;

        return token ? this.buildShareLink(token) : '';
    });

    readonly dataAsOf = computed<string>(() => {
        const loadedAt = this.loadedAtState();

        return loadedAt ? formatInstantInJerusalem(loadedAt, this.language.lang()) : '';
    });

    readonly dashboard = computed<GetPublicationDashboardResponse | undefined>(() => this.dashboardResource.value());

    readonly slotCounts = computed<SlotCountForGetPublicationDashboardResponse[]>(
        () => this.dashboard()?.slotCounts ?? [],
    );

    readonly hasPublication = computed(() => this.publication() !== undefined);

    readonly isLoading = computed(
        () => this.publicationResource.isLoading() || this.dashboardResource.isLoading(),
    );

    readonly loadError = computed(() => {
        const failed =
            this.publicationResource.error() ||
            this.dashboardResource.error() ||
            this.teachersResource.error();

        return failed ? 'publications.loadFailed' : undefined;
    });

    selectTeacher(teacherId: string): void {
        this.selectedTeacherIdState.set(teacherId);
    }

    selectWeek(weekStart: string): void {
        this.selectedWeekStartState.set(weekStart);
    }

    async publish(request: PublishPublicationRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.publish(id, request), 'publications.published');
    }

    async extendWindow(request: ExtendPublicationWindowRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.extendWindow(id, request), 'publications.extended');
    }

    async reopen(request: ReopenPublicationRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.reopen(id, request), 'publications.reopened');
    }

    async copyLink(): Promise<void> {
        const link = this.shareLink();

        if (!link) {
            return;
        }

        const copied = await this.clipboard.copy(link);

        if (copied) {
            this.toast.success('publications.linkCopied');

            return;
        }

        this.toast.apiError(undefined);
    }

    async downloadExcel(publicationId?: string, teacherId?: string): Promise<void> {
        const id = publicationId ?? this.publication()?.id;
        const teacher = teacherId ?? this.selectedTeacherIdState();

        if (!id || !teacher) {
            return;
        }

        this.mutating.set(true);

        try {
            const blob = await firstValueFrom(this.api.downloadExcel(id, teacher));
            this.fileDownload.download(blob, `week-${this.selectedWeekStartState()}.xlsx`);
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }

    refresh(): void {
        this.publicationResource.reload();
    }

    reloadHistory(): void {
        this.historyResource.reload();
    }

    private buildShareLink(token: string): string {
        return `${this.document.location.origin}${STUDENT_LINK_PREFIX}${token}`;
    }

    private async loadByWeek(weekStart: string): Promise<GetPublicationResponse | undefined> {
        try {
            return await firstValueFrom(this.api.getByWeek(weekStart));
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return undefined;
            }

            throw error;
        }
    }

    private async executeCommand(command: () => Observable<void>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.refresh();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}

function defaultWeekStart(): string {
    const options = buildWeekOptions('en');

    return options[0].weekStart;
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
