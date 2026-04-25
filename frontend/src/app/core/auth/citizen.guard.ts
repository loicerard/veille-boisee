import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { filter, map, switchMap, take } from 'rxjs';

export const citizenGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isLoading$.pipe(
    filter(loading => !loading),
    take(1),
    switchMap(() => auth.isAuthenticated$),
    take(1),
    map(isAuthenticated => isAuthenticated ? true : router.createUrlTree(['/connexion']))
  );
};
