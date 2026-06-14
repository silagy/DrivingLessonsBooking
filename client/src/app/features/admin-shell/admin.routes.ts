import { Routes } from '@angular/router';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [{ path: '', component: DashboardComponent }],
  },
];
