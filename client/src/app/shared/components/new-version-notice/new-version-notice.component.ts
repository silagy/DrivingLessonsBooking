import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { NewVersionStore } from '../../../core/pwa/new-version.store';

@Component({
  selector: 'app-new-version-notice',
  imports: [TranslocoPipe],
  template: `
    @if (store.isReady()) {
      <div class="new-version" role="status">
        <span>{{ 'pwa.newVersionReady' | transloco }}</span>
        <button type="button" class="new-version__refresh" (click)="store.reload()">
          {{ 'general.refresh' | transloco }}
        </button>
      </div>
    }
  `,
  styleUrl: './new-version-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewVersionNoticeComponent {
  protected readonly store = inject(NewVersionStore);
}
