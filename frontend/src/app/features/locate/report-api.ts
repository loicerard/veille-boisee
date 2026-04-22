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
