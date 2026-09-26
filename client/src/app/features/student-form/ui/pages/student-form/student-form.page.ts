import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';
import { StatusMessageComponent } from '../../components/status-message/status-message.component';
import { StudentShellComponent } from '../../components/student-shell/student-shell.component';

@Component({
    selector: 'app-student-form-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, StatusMessageComponent, StudentShellComponent],
    providers: [StudentFormStore],
    templateUrl: './student-form.page.html',
    styleUrl: './student-form.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
    protected readonly store = inject(StudentFormStore);
    protected readonly views = StudentFormView;

    readonly token = input.required<string>();

    constructor() {
        effect(() => this.store.open(this.token()));
    }
}
