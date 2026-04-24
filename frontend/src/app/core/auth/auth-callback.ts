import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { filter, take } from 'rxjs';

@Component({
  selector: 'app-auth-callback',
  template: `<p>Connexion en cours…</p>`,
})
export class AuthCallback implements OnInit {
  private readonly broadcast = inject(MsalBroadcastService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.broadcast.inProgress$
      .pipe(filter(status => status === InteractionStatus.None), take(1))
      .subscribe(() => this.router.navigate(['/']));
  }
}
