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
import { weekRangeLabel } from '../../../domain/week-options';
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

    protected weekLabel(row: ItemForFindPublicationHistoryResponse): string {
        const [year, month, day] = row.weekStart.split('-').map(Number);
        const weekStart = new Date(year, month - 1, day);

        return weekRangeLabel(weekStart, this.language.locale());
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

    protected formatInstant(utcIso: string): string {
        return formatInstantInJerusalem(utcIso, this.language.locale());
    }
}
