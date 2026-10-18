import { afterNextRender, ChangeDetectionStrategy, Component, computed, ElementRef, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LanguageService } from '../../../../../core/language.service';
import { gridDayDate } from '../../../../../shared/dates/grid-date';
import { SheetDragCloseDirective } from '../../../../../shared/directives/sheet-drag-close.directive';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { SlotRequestsDialogData } from './slot-requests-dialog-data';

const SINGLE_STUDENT = 1;
const DIALOG_FRAME_SELECTOR = '[role="dialog"]';

let nextTitleNumber = 0;

@Component({
    selector: 'app-slot-requests-dialog',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, InitialsPipe, SheetDragCloseDirective],
    templateUrl: './slot-requests.dialog.html',
    styleUrl: './slot-requests.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotRequestsDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly language = inject(LanguageService);
    private readonly data = inject(DynamicDialogConfig<SlotRequestsDialogData>).data as SlotRequestsDialogData;
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    protected readonly titleId = `slot-requests-title-${nextTitleNumber++}`;

    protected readonly slot = this.data.slot;
    protected readonly requests = this.data.requests;
    protected readonly isLoading = this.data.isLoading;
    protected readonly errorKey = this.data.errorKey;

    protected readonly date = computed(() => {
        const slot = this.slot();

        return slot ? gridDayDate(this.data.weekStart(), slot.day, this.language.locale()) : '';
    });

    protected readonly time = computed(() => {
        const slot = this.slot();

        return slot ? (this.data.windowTimes()[slot.window] ?? '') : '';
    });

    protected readonly studentCount = computed(() => {
        const listIsReady = !this.isLoading() && !this.errorKey();

        return listIsReady ? this.requests().length : (this.slot()?.requestCount ?? 0);
    });

    protected readonly studentCountKey = computed(() =>
        this.studentCount() === SINGLE_STUDENT
            ? 'publications.slotRequests.requestedByOne'
            : 'publications.slotRequests.requestedByMany',
    );

    constructor() {
        afterNextRender(() => {
            const frame = this.host.nativeElement.closest(DIALOG_FRAME_SELECTOR);
            frame?.setAttribute('aria-labelledby', this.titleId);
        });
    }

    protected close(): void {
        this.ref.close();
    }
}
