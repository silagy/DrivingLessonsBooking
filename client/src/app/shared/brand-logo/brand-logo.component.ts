import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand-logo',
  imports: [TranslocoPipe],
  template: `
    <span class="brand" [class.brand--lockup]="showTagline()">
      <svg class="brand__mark" viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <rect class="brand__page" x="1" y="5.5" width="38" height="33.5" rx="10" />
        <path class="brand__rings" d="M12.5 1.8v7M27.5 1.8v7" stroke-width="3.4" stroke-linecap="round" />
        <circle class="brand__hole" cx="12.5" cy="9.5" r="1.6" />
        <circle class="brand__hole" cx="27.5" cy="9.5" r="1.6" />
        <path class="brand__lane" d="M20 15.5v4M20 23.5v4M20 31.5v3" stroke-width="3.2" stroke-linecap="round" />
      </svg>
      @if (showName()) {
        @let lead = 'brand.nameLead' | transloco;
        @let accent = 'brand.nameAccent' | transloco;
        <span class="brand__word">
          <span class="brand__name" role="img" [attr.aria-label]="'brand.appName' | transloco">
            <span aria-hidden="true">{{ lead }}<span class="brand__accent">{{ accent }}</span></span>
          </span>
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
