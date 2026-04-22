import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface CommuneResponse {
  codeInsee: string;
  name: string;
}

export type CommuneLookupOutcome =
  | { status: 'found'; commune: CommuneResponse }
  | { status: 'notFound' }
  | { status: 'invalidCoordinates' }
  | { status: 'upstreamUnavailable' };

@Injectable({ providedIn: 'root' })
export class CommuneApi {
  private readonly http = inject(HttpClient);

  findByCoordinates(latitude: number, longitude: number): Observable<CommuneLookupOutcome> {
    const url = `${environment.apiBaseUrl}/api/communes`;
    return this.http
      .get<CommuneResponse>(url, { params: { lat: latitude, lon: longitude } })
      .pipe(
        map((commune): CommuneLookupOutcome => ({ status: 'found', commune })),
        catchError((error: HttpErrorResponse): Observable<CommuneLookupOutcome> => {
          switch (error.status) {
            case 400:
              return of({ status: 'invalidCoordinates' });
            case 404:
              return of({ status: 'notFound' });
            default:
              return of({ status: 'upstreamUnavailable' });
          }
        }),
      );
  }
}
