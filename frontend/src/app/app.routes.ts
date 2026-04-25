import { Routes } from '@angular/router';
import { citizenGuard } from './core/auth/citizen.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'connexion',
    loadComponent: () => import('./features/auth/connexion').then((m) => m.Connexion),
  },
  {
    path: 'auth-callback',
    loadComponent: () => import('./core/auth/auth-callback').then((m) => m.AuthCallback),
  },
  {
    path: 'signaler',
    canActivate: [citizenGuard],
    loadComponent: () => import('./features/locate/locate/locate').then((m) => m.Locate),
  },
  {
    path: 'mes-signalements',
    canActivate: [citizenGuard],
    loadComponent: () => import('./features/my-reports/my-reports').then((m) => m.MyReports),
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
  },
];
