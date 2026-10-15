import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand-logo',
  imports: [TranslocoPipe],
  template: `
    <span class="brand" [class.brand--lockup]="showTagline()">
      <svg class="brand__mark" viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <rect class="brand__calendar" x="13" y="5" width="25" height="24" rx="5" />
        <rect class="brand__page" x="15.5" y="11" width="20" height="15.5" rx="2.5" />
        <path class="brand__rings" d="M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8" stroke-width="2.4" stroke-linecap="round" />
        <path class="brand__check" d="M26.6 17.6l2.4 2.4 4.2-4.6" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" />
        <circle class="brand__halo" cx="15" cy="25.5" r="14.2" />
        <svg class="brand__wheel" x="2" y="12.5" width="26" height="26" viewBox="0 0 40 40">
          <circle class="brand__disc" cx="20" cy="20" r="19" />
          <path class="brand__window" d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z" />
          <path class="brand__window" d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z" />
          <path class="brand__window" d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z" />
          <circle class="brand__hub" cx="20" cy="21.5" r="4.6" stroke-width="2.2" />
        </svg>
      </svg>
      @if (showName()) {
        @let lead = 'brand.nameLead' | transloco;
        @let accent = 'brand.nameAccent' | transloco;
        <span class="brand__word">
          <span class="brand__name">{{ lead }}<span class="brand__accent">{{ accent }}</span></span>
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
