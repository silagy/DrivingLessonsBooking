import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormStep } from '../../../domain/student-form-step.enum';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';
import { DetailsStepComponent } from '../../components/details-step/details-step.component';
import { IdentifyStepComponent } from '../../components/identify-step/identify-step.component';
import { SlotsStepComponent } from '../../components/slots-step/slots-step.component';
import { StatusMessageComponent } from '../../components/status-message/status-message.component';
import { StudentShellComponent } from '../../components/student-shell/student-shell.component';

@Component({
    selector: 'app-student-form-page',
    imports: [
        TranslocoPipe,
        ButtonModule,
        ProgressSpinnerModule,
        StatusMessageComponent,
        StudentShellComponent,
        IdentifyStepComponent,
        DetailsStepComponent,
        SlotsStepComponent,
    ],
    providers: [StudentFormStore],
    templateUrl: './student-form.page.html',
    styleUrl: './student-form.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
    protected readonly store = inject(StudentFormStore);
    protected readonly views = StudentFormView;
    protected readonly steps = StudentFormStep;

    readonly token = input.required<string>();

    constructor() {
        effect(() => this.store.open(this.token()));
    }
}
