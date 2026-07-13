import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationsStore } from '../../../state/publications.store';
import { ItemForFindPublicationHistoryResponse } from '../../../data/item-for-find-publication-history.response';

@Component({
    selector: 'app-publications-history-page',
    imports: [
        TranslocoPipe,
        ButtonModule,
        TableModule,
        ProgressSpinnerModule,
        PublicationStateTagComponent,
    ],
    templateUrl: './publications-history.page.html',
    styleUrl: './publications-history.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsHistoryPage {
    protected readonly store = inject(PublicationsStore);
    private readonly router = inject(Router);
    private readonly language = inject(LanguageService);

    protected readonly PublicationState = PublicationState;

    protected windowLabel(row: ItemForFindPublicationHistoryResponse): string {
        if (!row.windowStartUtc || !row.windowEndUtc) {
            return '—';
        }

        return `${this.formatInstant(row.windowStartUtc)} → ${this.formatInstant(row.windowEndUtc)}`;
    }

    protected canRedownload(row: ItemForFindPublicationHistoryResponse): boolean {
        return row.state === PublicationState.closed && row.latestExcelVersion !== null;
    }

    protected onRedownload(row: ItemForFindPublicationHistoryResponse): void {
        void this.store.downloadExcel(row.publicationId, row.teacherId);
    }

    protected onViewDashboard(row: ItemForFindPublicationHistoryResponse): void {
        void this.router.navigate(['/', AppRoutes.publications], {
            queryParams: { teacherId: row.teacherId, week: row.weekStart },
        });
    }

    private formatInstant(utcIso: string): string {
        return formatInstantInJerusalem(utcIso, this.language.lang());
    }
}
