import { Routes } from '@angular/router';
import { administratorGuard, homeGuard } from '../../core/role.guards';
import { AppRoutes } from '../../shared/config/app-routes';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', pathMatch: 'full', component: DashboardComponent, canActivate: [homeGuard] },
      {
        path: AppRoutes.teachers,
        canMatch: [administratorGuard],
        loadChildren: () => import('../teachers/teachers.routes'),
      },
      {
        path: `${AppRoutes.students}/${AppRoutes.rosterImport}`,
        canMatch: [administratorGuard],
        loadChildren: () => import('../roster/roster.routes'),
      },
      {
        path: AppRoutes.students,
        canMatch: [administratorGuard],
        loadChildren: () => import('../students/students.routes'),
      },
      {
        path: AppRoutes.roster,
        pathMatch: 'full',
        redirectTo: `${AppRoutes.students}/${AppRoutes.rosterImport}`,
      },
      {
        path: AppRoutes.weekSchedules,
        loadChildren: () => import('../week-schedules/week-schedules.routes'),
      },
      {
        path: AppRoutes.publications,
        loadChildren: () => import('../publications/publications.routes'),
      },
      {
        path: AppRoutes.users,
        canMatch: [administratorGuard],
        loadChildren: () => import('../users/users.routes'),
      },
    ],
  },
];
