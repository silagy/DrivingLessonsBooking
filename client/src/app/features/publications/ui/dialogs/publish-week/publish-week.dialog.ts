import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ClipboardService } from '../../../../../core/services/clipboard.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { jerusalemWallTimeToUtcIso } from '../../../domain/jerusalem-time';
import { ShareLinkBoxComponent } from '../../components/share-link-box/share-link-box.component';

export interface PublishWeekDialogData {
    weekLabel: string;
    link: string;
}

export interface PublishWeekResult {
    startUtc: string;
    endUtc: string;
}

const LIFECYCLE_STEPS = ['draft', 'published', 'open', 'closed'] as const;

@Component({
    selector: 'app-publish-week-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, DatePickerModule, ShareLinkBoxComponent],
    templateUrl: './publish-week.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublishWeekDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<PublishWeekDialogData>);
    private readonly clipboard = inject(ClipboardService);
    private readonly toast = inject(ToastService);

    protected readonly weekLabel = this.config.data?.weekLabel ?? '';
    protected readonly link = this.config.data?.link ?? '';
    protected readonly steps = LIFECYCLE_STEPS;

    protected readonly form = this.fb.nonNullable.group({
        start: this.fb.nonNullable.control<Date | null>(null, Validators.required),
        end: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    });

    protected async copyLink(): Promise<void> {
        const copied = await this.clipboard.copy(this.link);
        this.toast.success(copied ? 'publications.shareLink.copied' : 'publications.shareLink.copyFailed');
    }

    protected submit(): void {
        const { start, end } = this.form.getRawValue();

        if (this.form.invalid || !start || !end) {
            return;
        }

        const result: PublishWeekResult = {
            startUtc: jerusalemWallTimeToUtcIso(start),
            endUtc: jerusalemWallTimeToUtcIso(end),
        };

        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
