import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { MsalService } from '@azure/msal-angular';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly msalService = inject(MsalService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly showBackButton = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map((e) => (e as NavigationEnd).urlAfterRedirects !== '/'),
    ),
    { initialValue: this.router.url !== '/' },
  );

  ngOnInit(): void {
    this.msalService.initialize().subscribe(() => {
      this.msalService.handleRedirectObservable().subscribe();
    });
  }
}
