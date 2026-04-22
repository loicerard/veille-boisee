import { Injectable, signal } from '@angular/core';

// Bouchon d'authentification — sera remplacé par un service JWT en phase auth
@Injectable({ providedIn: 'root' })
export class CollectiviteContext {
  readonly collectiviteId = signal('DEV_COLLECTIVITE');
  readonly label = signal('Paris (développement)');
}
