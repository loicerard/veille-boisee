import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';

export interface CitizenUser {
  userId: string;
  email: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly auth0 = inject(Auth0Service);

  private readonly auth0User = toSignal(this.auth0.user$, { initialValue: null });
  private readonly loading = toSignal(this.auth0.isLoading$, { initialValue: true });

  readonly ready = computed(() => !this.loading());
  readonly isAuthenticated = computed(() => this.auth0User() !== null);
  readonly user = computed<CitizenUser | null>(() => {
    const u = this.auth0User();
    if (!u?.sub) return null;
    return { userId: u.sub, email: u.email ?? '' };
  });

  login(): void {
    this.auth0.loginWithRedirect();
  }

  logout(): void {
    this.auth0.logout({ logoutParams: { returnTo: window.location.origin } });
  }
}
