import { Routes } from '@angular/router';
import { AppRoutes } from '../../shared/config/app-routes';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', component: DashboardComponent },
      {
        path: AppRoutes.teachers,
        loadChildren: () => import('../teachers/teachers.routes'),
      },
    ],
  },
];
