import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { NewVersionStore } from '../../../core/pwa/new-version.store';

@Component({
  selector: 'app-new-version-notice',
  imports: [TranslocoPipe],
  template: `
    <div role="status">
      @if (store.isShown()) {
        <div class="new-version">
          <span class="new-version__message">{{ 'pwa.newVersionReady' | transloco }}</span>
          <div class="new-version__actions">
            <button type="button" class="new-version__later" (click)="store.dismiss()">
              {{ 'pwa.later' | transloco }}
            </button>
            <button type="button" class="new-version__refresh" (click)="store.reload()">
              {{ 'general.refresh' | transloco }}
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styleUrl: './new-version-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewVersionNoticeComponent {
  protected readonly store = inject(NewVersionStore);
}
