import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { GetPublicationByLinkResponse } from '../data/get-publication-by-link.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { formatWindowInstant } from '../domain/jerusalem-time';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };

@Injectable()
export class StudentFormStore {
    private readonly api = inject(SubmissionsApiService);
    private readonly language = inject(LanguageService);

    private readonly linkToken = signal<string | null>(null);

    private readonly publicationResource = resource({
        params: () => this.linkToken() ?? undefined,
        loader: ({ params: token }) => this.loadByLink(token),
    });

    private readonly publication = computed<GetPublicationByLinkResponse | null>(() =>
        this.publicationResource.hasValue() ? this.publicationResource.value() : null,
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

    open(token: string): void {
        this.linkToken.set(token);
    }

    retry(): void {
        this.publicationResource.reload();
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
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
