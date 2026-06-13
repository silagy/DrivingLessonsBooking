import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-dashboard',
  imports: [TranslocoPipe],
  template: `
    <h2>{{ 'dashboard.title' | transloco }}</h2>
    <p>{{ 'dashboard.placeholder' | transloco }}</p>
  `,
})
export class DashboardComponent {}
