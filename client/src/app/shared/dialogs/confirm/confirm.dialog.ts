import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

export interface ConfirmDialogData {
    messageKey: string;
    messageParams?: Record<string, string>;
}

@Component({
    selector: 'app-confirm-dialog',
    imports: [TranslocoPipe, ButtonModule],
    templateUrl: './confirm.dialog.html',
    styleUrl: './confirm.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<ConfirmDialogData>);

    protected readonly messageKey = this.config.data?.messageKey ?? '';
    protected readonly messageParams = this.config.data?.messageParams ?? {};

    protected confirm(): void {
        this.ref.close(true);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
