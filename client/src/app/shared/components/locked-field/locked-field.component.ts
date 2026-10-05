import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'app-locked-field',
    templateUrl: './locked-field.component.html',
    styleUrl: './locked-field.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LockedFieldComponent {
    readonly label = input.required<string>();
    readonly value = input.required<string>();
}
