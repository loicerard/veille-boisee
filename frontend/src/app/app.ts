import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
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
  readonly auth = inject(AuthService);

  ngOnInit(): void {
    this.msalService.initialize().subscribe(() => {
      this.msalService.handleRedirectObservable().subscribe();
    });
  }
}
