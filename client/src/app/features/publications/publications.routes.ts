import { Routes } from '@angular/router';
import { PublicationsDashboardPage } from './ui/pages/publications-dashboard/publications-dashboard.page';
import { PublicationsHistoryPage } from './ui/pages/publications-history/publications-history.page';

export default [
    { path: '', component: PublicationsDashboardPage },
    { path: 'history', component: PublicationsHistoryPage },
] satisfies Routes;
