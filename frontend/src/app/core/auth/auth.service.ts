import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { filter, take } from 'rxjs';

export interface CitizenUser {
  userId: string;
  email: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly msal = inject(MsalService);
  private readonly broadcast = inject(MsalBroadcastService);

  private readonly _user = signal<CitizenUser | null>(null);
  private readonly _ready = signal(false);

  readonly user = this._user.asReadonly();
  readonly ready = this._ready.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  constructor() {
    this.broadcast.inProgress$
      .pipe(filter(status => status === InteractionStatus.None))
      .subscribe(() => {
        this._user.set(this.extractUser());
        this._ready.set(true);
      });
  }

  login(): void {
    this.msal.loginRedirect({ scopes: [] });
  }

  logout(): void {
    this.msal.logoutRedirect();
  }

  waitUntilReady() {
    return toObservable(this._ready).pipe(filter(Boolean), take(1));
  }

  private extractUser(): CitizenUser | null {
    const accounts = this.msal.instance.getAllAccounts();
    if (accounts.length === 0) return null;
    const account = accounts[0];
    return {
      userId: account.localAccountId,
      email: account.username,
    };
  }
}
