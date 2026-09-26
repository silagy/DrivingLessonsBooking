import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BrandLogoComponent } from '../../../../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../../../../shared/language-toggle/language-toggle.component';

@Component({
    selector: 'app-student-shell',
    imports: [BrandLogoComponent, LanguageToggleComponent],
    templateUrl: './student-shell.component.html',
    styleUrl: './student-shell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentShellComponent {
    readonly caption = input<string>('');
}
