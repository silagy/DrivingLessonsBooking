import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { Popover, PopoverModule } from 'primeng/popover';
import { LanguageService } from '../../../../../core/language.service';
import { Teacher } from '../../../domain/teacher.model';
import { rtlPopoverPlacement } from './rtl-popover-placement';

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
    private readonly language = inject(LanguageService);

    readonly teachers = input.required<Teacher[]>();
    readonly assignedIds = input.required<string[]>();

    readonly applied = output<string[]>();

    private readonly popover = viewChild.required(Popover);
    private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
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

    protected anchorToTrigger(): void {
        const panel = this.popover().container;

        if (!this.language.isRtl() || !panel) {
            return;
        }

        const placement = rtlPopoverPlacement(
            this.trigger().nativeElement.getBoundingClientRect().right,
            panel.offsetWidth,
            document.documentElement.getBoundingClientRect().right,
        );
        panel.style.insetInlineEnd = '';
        panel.style.insetInlineStart = `${placement.insetInlineStart}px`;
        panel.style.setProperty('--p-popover-arrow-left', `${placement.arrowInset}px`);
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
