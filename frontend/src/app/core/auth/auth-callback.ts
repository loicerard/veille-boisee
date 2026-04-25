import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { filter, take } from 'rxjs';

@Component({
  selector: 'app-auth-callback',
  template: `<p>Connexion en cours…</p>`,
})
export class AuthCallback implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.auth.isLoading$.pipe(
      filter(loading => !loading),
      take(1)
    ).subscribe(() => this.router.navigate(['/']));
  }
}
