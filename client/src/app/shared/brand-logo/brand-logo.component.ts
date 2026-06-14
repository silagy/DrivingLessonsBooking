import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand-logo',
  imports: [TranslocoPipe],
  template: `
    <span class="brand">
      <span class="brand__mark" aria-hidden="true">W</span>
      @if (showName()) {
        <span class="brand__name">{{ 'brand.appName' | transloco }}</span>
      }
    </span>
  `,
  styleUrl: './brand-logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandLogoComponent {
  readonly showName = input(true);
}
