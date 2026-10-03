import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    FormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    ValidatorFn,
    Validators,
} from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { LinkableTeacher } from '../../../domain/linkable-teacher.model';
import { Role } from '../../../domain/role.enum';
import { isTeacherLinkRequired } from '../../../domain/teacher-link';

export interface AddUserDialogData {
    teachers: LinkableTeacher[];
}

export interface AddUserResult {
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    temporaryPassword: string;
}

const TEACHER_LINK_MISSING = 'teacherLinkMissing';

const teacherLinkValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
    const role = group.get('role')?.value as Role;
    const teacherId = group.get('teacherId')?.value as string | null;
    const missing = isTeacherLinkRequired(role) && !teacherId;

    return missing ? { [TEACHER_LINK_MISSING]: true } : null;
};

@Component({
    selector: 'app-add-user-dialog',
    imports: [
        ReactiveFormsModule,
        TranslocoPipe,
        ButtonModule,
        InputTextModule,
        PasswordModule,
        SelectModule,
        SelectButtonModule,
    ],
    templateUrl: './add-user.dialog.html',
    styleUrls: ['../dialog-form.scss', './add-user.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddUserDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<AddUserDialogData>);
    private readonly transloco = inject(TranslocoService);

    protected readonly teacherLinkMissing = TEACHER_LINK_MISSING;

    protected readonly teachers: LinkableTeacher[] = this.config.data?.teachers ?? [];

    protected readonly roleOptions = Object.values(Role).map((value) => ({
        value,
        label: this.transloco.translate(`users.roles.${value}`),
    }));

    protected readonly form = this.fb.group(
        {
            name: this.fb.nonNullable.control('', Validators.required),
            signInEmail: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
            role: this.fb.nonNullable.control(Role.teacher, Validators.required),
            teacherId: this.fb.control<string | null>(null),
            temporaryPassword: this.fb.nonNullable.control('', Validators.required),
        },
        { validators: teacherLinkValidator },
    );

    private readonly role = toSignal(this.form.controls.role.valueChanges, {
        initialValue: this.form.controls.role.value,
    });

    protected readonly teacherLinkRequired = computed(() => isTeacherLinkRequired(this.role()));

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: AddUserResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
