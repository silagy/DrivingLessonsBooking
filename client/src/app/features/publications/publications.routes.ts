import { Routes } from '@angular/router';
import { AppRoutes } from '../../shared/config/app-routes';
import { PublicationsDashboardPage } from './ui/pages/publications-dashboard/publications-dashboard.page';
import { PublicationsHistoryPage } from './ui/pages/publications-history/publications-history.page';

export default [
    { path: '', component: PublicationsDashboardPage },
    { path: AppRoutes.publicationsHistory, component: PublicationsHistoryPage },
] satisfies Routes;
