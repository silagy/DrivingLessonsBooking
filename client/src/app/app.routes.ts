import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AppRoutes } from './shared/config/app-routes';

export const routes: Routes = [
  {
    path: AppRoutes.login,
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/admin-shell/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
