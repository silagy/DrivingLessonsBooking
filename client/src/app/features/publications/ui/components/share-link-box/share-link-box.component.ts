import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-share-link-box',
    imports: [ButtonModule, TranslocoPipe],
    templateUrl: './share-link-box.component.html',
    styleUrl: './share-link-box.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareLinkBoxComponent {
    readonly link = input.required<string>();

    readonly copyClicked = output<void>();
}
