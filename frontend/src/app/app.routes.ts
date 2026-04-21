import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'signaler',
    loadComponent: () =>
      import('./features/locate/locate/locate').then((m) => m.Locate),
  },
  {
    path: 'mes-signalements',
    loadComponent: () =>
      import('./features/my-reports/my-reports').then((m) => m.MyReports),
  },
];
