import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { environment } from '../../../environments/environment';

export type ReportStatus = 0 | 1 | 2 | 3; // Pending | Routed | Acknowledged | Closed

export const REPORT_STATUS_LABELS: Record<ReportStatus, string> = {
  0: 'En attente',
  1: 'Transmis',
  2: 'Pris en charge',
  3: 'Clôturé',
};

export const NEXT_STATUSES: Partial<Record<ReportStatus, ReportStatus>> = {
  0: 1,
  1: 2,
  2: 3,
};

export interface ReportSummary {
  id: string;
  communeName: string;
  communeInsee: string;
  status: ReportStatus;
  submittedAt: string;
  isInForest: boolean;
  isInNatura2000Zone: boolean;
}

export interface ReportPage {
  items: ReportSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ReportDetail {
  id: string;
  latitude: number;
  longitude: number;
  communeInsee: string;
  communeName: string;
  description: string;
  contactEmail: string;
  status: ReportStatus;
  submittedAt: string;
  parcelleSection: string | null;
  parcelleNumero: string | null;
  isInForest: boolean | null;
  isInNatura2000Zone: boolean | null;
  hasPhoto: boolean;
}

export interface ListFilters {
  status: ReportStatus | null;
  from: string | null;
  to: string | null;
}

export type ListOutcome = { status: 'ok'; data: ReportPage } | { status: 'serverError' };
export type DetailOutcome = { status: 'ok'; data: ReportDetail } | { status: 'notFound' } | { status: 'serverError' };
export type StatusUpdateOutcome =
  | { status: 'updated'; newStatus: ReportStatus }
  | { status: 'invalidTransition' }
  | { status: 'notFound' }
  | { status: 'serverError' };
export type CsvExportOutcome =
  | { status: 'exported'; blob: Blob; filename: string }
  | { status: 'noData' }
  | { status: 'serverError' };

@Injectable({ providedIn: 'root' })
export class DashboardReportApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/collectivite/reports`;

  list(page: number, pageSize: number, filters: ListFilters): Observable<ListOutcome> {
    const params: Record<string, string> = { page: String(page), pageSize: String(pageSize) };
    if (filters.status !== null) params['status'] = String(filters.status);
    if (filters.from) params['from'] = filters.from;
    if (filters.to) params['to'] = filters.to;

    return this.http.get<ReportPage>(this.base, { params }).pipe(
      map((data): ListOutcome => ({ status: 'ok', data })),
      catchError((): Observable<ListOutcome> => of({ status: 'serverError' })),
    );
  }

  getDetail(id: string): Observable<DetailOutcome> {
    return this.http.get<ReportDetail>(`${this.base}/${id}`).pipe(
      map((data): DetailOutcome => ({ status: 'ok', data })),
      catchError((error: HttpErrorResponse): Observable<DetailOutcome> => {
        if (error.status === 404) return of({ status: 'notFound' });
        return of({ status: 'serverError' });
      }),
    );
  }

  updateStatus(id: string, newStatus: ReportStatus): Observable<StatusUpdateOutcome> {
    return this.http.patch<{ status: ReportStatus }>(`${this.base}/${id}/status`, { newStatus }).pipe(
      map((response): StatusUpdateOutcome => ({ status: 'updated', newStatus: response.status })),
      catchError((error: HttpErrorResponse): Observable<StatusUpdateOutcome> => {
        if (error.status === 404) return of({ status: 'notFound' });
        if (error.status === 422) return of({ status: 'invalidTransition' });
        return of({ status: 'serverError' });
      }),
    );
  }

  exportCsv(filters: ListFilters): Observable<CsvExportOutcome> {
    const params: Record<string, string> = {};
    if (filters.status !== null) params['status'] = String(filters.status);
    if (filters.from) params['from'] = filters.from;
    if (filters.to) params['to'] = filters.to;

    return this.http.get(`${this.base}/export.csv`, { params, responseType: 'blob', observe: 'response' }).pipe(
      map((response): CsvExportOutcome => {
        if (response.status === 204) return { status: 'noData' };
        const disposition = response.headers.get('Content-Disposition') ?? '';
        const match = disposition.match(/filename="([^"]+)"/);
        const filename = match ? match[1] : 'signalements.csv';
        return { status: 'exported', blob: response.body!, filename };
      }),
      catchError((): Observable<CsvExportOutcome> => of({ status: 'serverError' })),
    );
  }
}
