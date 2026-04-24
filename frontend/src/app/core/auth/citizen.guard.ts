import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

export const citizenGuard: CanActivateFn = () => {
  const msal = inject(MsalService);
  const router = inject(Router);

  const isLoggedIn = msal.instance.getAllAccounts().length > 0;
  return isLoggedIn ? true : router.createUrlTree(['/connexion']);
};
