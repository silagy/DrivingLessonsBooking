import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand-logo',
  imports: [TranslocoPipe],
  template: `
    <span class="brand" [class.brand--lockup]="showTagline()">
      <svg class="brand__mark" viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <rect x="2" y="2" width="36" height="36" rx="10" fill="none" stroke="currentColor" stroke-width="3" />
        <path d="M20 9v5M20 17.5v5M20 26v5" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
      </svg>
      @if (showName()) {
        <span class="brand__word">
          <span class="brand__name">{{ 'brand.appName' | transloco }}</span>
          @if (showTagline()) {
            <span class="brand__tagline">{{ 'brand.tagline' | transloco }}</span>
          }
        </span>
      }
    </span>
  `,
  styleUrl: './brand-logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandLogoComponent {
  readonly showName = input(true);
  readonly showTagline = input(false);
}
