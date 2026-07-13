import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { DialogService } from 'primeng/dynamicdialog';
import { WeekGridComponent } from '../../../../../shared/components/week-grid/week-grid.component';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationsStore } from '../../../state/publications.store';
import { SlotCountCellComponent } from '../../components/slot-count-cell/slot-count-cell.component';
import { PublicationStatChipComponent } from '../../components/publication-stat-chip/publication-stat-chip.component';
import { ShareLinkBoxComponent } from '../../components/share-link-box/share-link-box.component';
import { PublishWeekDialog, PublishWeekResult } from '../../dialogs/publish-week/publish-week.dialog';
import { ExtendWindowDialog, WindowEndResult } from '../../dialogs/extend-window/extend-window.dialog';
import { ReopenWindowDialog } from '../../dialogs/reopen-window/reopen-window.dialog';

const PUBLISH_FLAG = '1';
const DIALOG_WIDTH = '35rem';
const NARROW_DIALOG_WIDTH = '28rem';

@Component({
    selector: 'app-publications-dashboard-page',
    imports: [
        FormsModule,
        TranslocoPipe,
        ButtonModule,
        SelectModule,
        ProgressSpinnerModule,
        WeekGridComponent,
        PublicationStateTagComponent,
        SlotCountCellComponent,
        PublicationStatChipComponent,
        ShareLinkBoxComponent,
    ],
    providers: [DialogService],
    templateUrl: './publications-dashboard.page.html',
    styleUrl: './publications-dashboard.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsDashboardPage {
    protected readonly store = inject(PublicationsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);
    private readonly language = inject(LanguageService);

    protected readonly PublicationState = PublicationState;

    readonly teacherId = input<string>();
    readonly week = input<string>();
    readonly publish = input<string>();

    protected readonly windowLabel = computed(() => {
        const dashboard = this.store.dashboard();

        return dashboard?.windowEndUtc ? this.formatInstant(dashboard.windowEndUtc) : '';
    });

    constructor() {
        effect(() => {
            const teacherId = this.teacherId();

            if (teacherId) {
                this.store.selectTeacher(teacherId);
            }
        });

        effect(() => {
            const week = this.week();

            if (week) {
                this.store.selectWeek(week);
            }
        });

        effect(() => {
            if (this.publish() === PUBLISH_FLAG && this.store.state() === PublicationState.draft) {
                this.onPublish();
            }
        });
    }

    protected formatInstant(utcIso: string): string {
        if (!utcIso) {
            return '';
        }

        return formatInstantInJerusalem(utcIso, this.language.lang());
    }

    protected onPublish(): void {
        const publication = this.store.publication();

        if (!publication) {
            return;
        }

        const ref = this.dialogs.open(PublishWeekDialog, {
            header: this.transloco.translate('publications.publish'),
            width: DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
            data: { weekLabel: this.store.weekLabel(), link: this.store.shareLink() },
        });

        ref?.onClose.subscribe((result?: PublishWeekResult) => {
            if (result) {
                void this.store.publish(result);
            }
        });
    }

    protected onExtend(): void {
        const ref = this.dialogs.open(ExtendWindowDialog, {
            header: this.transloco.translate('publications.extendWindow'),
            width: NARROW_DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: WindowEndResult) => {
            if (result) {
                void this.store.extendWindow(result);
            }
        });
    }

    protected onReopen(): void {
        const ref = this.dialogs.open(ReopenWindowDialog, {
            header: this.transloco.translate('publications.reopenWindow'),
            width: NARROW_DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: WindowEndResult) => {
            if (result) {
                void this.store.reopen(result);
            }
        });
    }
}
