import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/locate/locate/locate').then((m) => m.Locate),
  },
];
