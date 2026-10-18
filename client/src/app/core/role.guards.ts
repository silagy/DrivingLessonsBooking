import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AppRoutes } from '../shared/config/app-routes';
import { AuthService } from './auth.service';
import { ToastService } from './services/toast.service';

export const administratorGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/', AppRoutes.login]);
  }

  if (auth.isAdministrator()) {
    return true;
  }

  void inject(ToastService).info('access.refusedTitle', { key: 'access.refusedDetail' });

  return router.createUrlTree(['/', AppRoutes.weekSchedules]);
};
