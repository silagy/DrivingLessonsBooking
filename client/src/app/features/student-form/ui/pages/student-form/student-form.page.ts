import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';

@Component({
    selector: 'app-student-form-page',
    imports: [TranslocoPipe, ProgressSpinnerModule],
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
