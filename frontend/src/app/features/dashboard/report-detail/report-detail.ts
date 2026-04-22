import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import {
  DashboardReportApi,
  NEXT_STATUSES,
  REPORT_STATUS_LABELS,
  ReportDetail as ReportDetailData,
  ReportStatus,
} from '../dashboard-report-api';
import { ReportMapDisplay } from '../report-map-display/report-map-display';

type ViewState =
  | { kind: 'loading' }
  | { kind: 'loaded'; report: ReportDetailData; updating: boolean; updateError: string | null }
  | { kind: 'notFound' }
  | { kind: 'error' };

@Component({
  selector: 'app-report-detail',
  imports: [RouterLink, ReportMapDisplay, DatePipe],
  templateUrl: './report-detail.html',
  styleUrl: './report-detail.scss',
})
export class ReportDetail implements OnInit {
  private readonly api = inject(DashboardReportApi);
  private readonly route = inject(ActivatedRoute);

  readonly state = signal<ViewState>({ kind: 'loading' });
  readonly statusLabels: Record<number, string> = REPORT_STATUS_LABELS;
  readonly nextStatuses = NEXT_STATUSES;

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'] as string;
    this.api.getDetail(id).subscribe((outcome) => {
      if (outcome.status === 'ok') {
        this.state.set({ kind: 'loaded', report: outcome.data, updating: false, updateError: null });
      } else if (outcome.status === 'notFound') {
        this.state.set({ kind: 'notFound' });
      } else {
        this.state.set({ kind: 'error' });
      }
    });
  }

  updateStatus(newStatus: ReportStatus): void {
    const s = this.state();
    if (s.kind !== 'loaded') return;

    this.state.set({ ...s, updating: true, updateError: null });

    this.api.updateStatus(s.report.id, newStatus).subscribe((outcome) => {
      const current = this.state();
      if (current.kind !== 'loaded') return;

      if (outcome.status === 'updated') {
        this.state.set({ ...current, report: { ...current.report, status: outcome.newStatus }, updating: false });
      } else if (outcome.status === 'invalidTransition') {
        this.state.set({ ...current, updating: false, updateError: 'Transition de statut invalide.' });
      } else {
        this.state.set({ ...current, updating: false, updateError: 'Erreur lors de la mise à jour.' });
      }
    });
  }

  nextStatus(current: ReportStatus): ReportStatus | undefined {
    return this.nextStatuses[current];
  }
}
