import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-publications-history-page',
    imports: [TranslocoPipe],
    template: `<h2>{{ 'publications.history.title' | transloco }}</h2>`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsHistoryPage {}
