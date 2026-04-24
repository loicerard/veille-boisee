import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-connexion',
  styleUrl: './connexion.scss',
  template: `
    <main class="connexion">
      <div class="connexion__card">
        <span class="connexion__eyebrow">
          <svg viewBox="0 0 16 16" aria-hidden="true" width="14" height="14">
            <path d="M8 2c-2.8 2-4.5 4.3-4.5 7 0 2.1 1.3 3.9 3.2 4.6V15h2.6v-1.4c1.9-.7 3.2-2.5 3.2-4.6C12.5 6.3 10.8 4 8 2z" fill="currentColor"/>
          </svg>
          Veille Boisée
        </span>
        <h1 class="connexion__title">Signaler une décharge</h1>
        <p class="connexion__lead">Connectez-vous pour soumettre un signalement. Votre identité ne sera jamais transmise aux communes.</p>
        <button class="connexion__btn connexion__btn--google" (click)="auth.login()">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
          </svg>
          Continuer avec Google
        </button>
      </div>
    </main>
  `,
})
export class Connexion {
  readonly auth = inject(AuthService);
}
