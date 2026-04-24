import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { environment } from '../../../environments/environment';

export type ReportStatus = 'Pending' | 'Routed' | 'Acknowledged' | 'Closed';

export type ReportStatusOutcome =
  | { kind: 'found'; status: ReportStatus }
  | { kind: 'not-found' }
  | { kind: 'error' };

export interface SubmitReportRequest {
  latitude: number;
  longitude: number;
  communeInsee: string;
  communeName: string;
  description: string;
  contactEmail: string;
}

export interface ReportSubmittedResponse {
  reportId: string;
}

export type SubmitReportOutcome =
  | { status: 'submitted'; reportId: string }
  | { status: 'validationError' }
  | { status: 'serverError' };

@Injectable({ providedIn: 'root' })
export class ReportApi {
  private readonly http = inject(HttpClient);

  getStatus(reportId: string): Observable<ReportStatusOutcome> {
    const url = `${environment.apiBaseUrl}/api/reports/${reportId}/status`;
    return this.http
      .get<{ status: ReportStatus }>(url, { headers: { 'Cache-Control': 'no-cache' } })
      .pipe(
        map((response): ReportStatusOutcome => ({ kind: 'found', status: response.status })),
        catchError((error: HttpErrorResponse): Observable<ReportStatusOutcome> => {
          if (error.status === 404) return of({ kind: 'not-found' });
          return of({ kind: 'error' });
        }),
      );
  }

  submit(request: SubmitReportRequest, photo: File | null): Observable<SubmitReportOutcome> {
    const url = `${environment.apiBaseUrl}/api/reports`;
    const formData = new FormData();
    formData.append('latitude', String(request.latitude));
    formData.append('longitude', String(request.longitude));
    formData.append('communeInsee', request.communeInsee);
    formData.append('communeName', request.communeName);
    formData.append('description', request.description);
    formData.append('contactEmail', request.contactEmail);
    if (photo) {
      formData.append('photo', photo);
    }
    return this.http
      .post<ReportSubmittedResponse>(url, formData)
      .pipe(
        map((response): SubmitReportOutcome => ({ status: 'submitted', reportId: response.reportId })),
        catchError((error: HttpErrorResponse): Observable<SubmitReportOutcome> => {
          if (error.status === 400 || error.status === 422) {
            return of({ status: 'validationError' });
          }
          return of({ status: 'serverError' });
        }),
      );
  }
}
