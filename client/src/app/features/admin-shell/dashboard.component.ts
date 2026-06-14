import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-dashboard',
  imports: [TranslocoPipe],
  template: `
    <section class="dashboard">
      <h1 class="dashboard__title">{{ 'dashboard.title' | transloco }}</h1>
      <p class="dashboard__lead">{{ 'dashboard.placeholder' | transloco }}</p>
    </section>
  `,
  styles: `
    .dashboard {
      background: var(--app-bg-card);
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius-card);
      box-shadow: var(--app-shadow-card);
      padding: 2rem;
      max-width: 36rem;
    }

    .dashboard__title {
      margin: 0;
      font-family: var(--app-font-display);
      font-weight: 500;
      font-size: 1.625rem;
      letter-spacing: -0.01em;
      color: var(--app-ink);
    }

    .dashboard__lead {
      margin: 0.75rem 0 0;
      color: var(--app-text-secondary);
      line-height: 1.5;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {}
