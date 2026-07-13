import { ChangeDetectionStrategy, Component, computed, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { Popover, PopoverModule } from 'primeng/popover';
import { Teacher } from '../../../domain/teacher.model';

interface AssignRow {
    id: string;
    name: string;
    selected: boolean;
    assigned: boolean;
}

@Component({
    selector: 'app-assign-teachers-popover',
    imports: [FormsModule, TranslocoPipe, ButtonModule, CheckboxModule, PopoverModule],
    templateUrl: './assign-teachers-popover.component.html',
    styleUrl: './assign-teachers-popover.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignTeachersPopoverComponent {
    readonly teachers = input.required<Teacher[]>();
    readonly assignedIds = input.required<string[]>();

    readonly applied = output<string[]>();

    private readonly popover = viewChild.required(Popover);
    private readonly selectedIds = signal<string[]>([]);

    protected readonly rows = computed<AssignRow[]>(() => {
        const selected = this.selectedIds();
        const assigned = this.assignedIds();

        return this.teachers().map((teacher) => ({
            id: teacher.id,
            name: teacher.name,
            selected: selected.includes(teacher.id),
            assigned: assigned.includes(teacher.id),
        }));
    });

    protected open(event: Event): void {
        this.selectedIds.set([...this.assignedIds()]);
        this.popover().toggle(event);
    }

    protected setSelected(teacherId: string, checked: boolean): void {
        const current = this.selectedIds();

        if (checked) {
            if (!current.includes(teacherId)) {
                this.selectedIds.set([...current, teacherId]);
            }

            return;
        }

        this.selectedIds.set(current.filter((id) => id !== teacherId));
    }

    protected apply(): void {
        this.applied.emit(this.selectedIds());
        this.popover().hide();
    }

    protected cancel(): void {
        this.popover().hide();
    }
}
