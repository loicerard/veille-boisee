import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./report-list/report-list').then((m) => m.ReportList),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./report-detail/report-detail').then((m) => m.ReportDetail),
  },
];
