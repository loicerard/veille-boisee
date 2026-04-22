import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

export type ReportStatus = 'Pending' | 'Routed' | 'Acknowledged' | 'Closed';

import { environment } from '../../../environments/environment';

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

  getStatus(reportId: string): Observable<ReportStatus | null> {
    const url = `${environment.apiBaseUrl}/api/reports/${reportId}/status`;
    return this.http
      .get<{ status: ReportStatus }>(url)
      .pipe(
        map((response) => response.status),
        catchError(() => of(null)),
      );
  }

  submit(request: SubmitReportRequest): Observable<SubmitReportOutcome> {
    const url = `${environment.apiBaseUrl}/api/reports`;
    return this.http
      .post<ReportSubmittedResponse>(url, request)
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
