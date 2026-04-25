import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MyReport, ReportApi, ReportStatus } from '../locate/report-api';

const STATUS_LABELS: Record<ReportStatus, string> = {
  Pending: 'En attente',
  Routed: 'Transmis',
  Acknowledged: 'Pris en charge',
  Closed: 'Clôturé',
};

@Component({
  selector: 'app-my-reports',
  imports: [RouterLink, DatePipe],
  templateUrl: './my-reports.html',
  styleUrl: './my-reports.scss',
})
export class MyReports implements OnInit {
  private readonly reportApi = inject(ReportApi);

  readonly reports = signal<MyReport[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.reportApi.getMyReports().subscribe((reports) => {
      this.reports.set(reports);
      this.loading.set(false);
    });
  }

  statusLabel(status: ReportStatus): string {
    return STATUS_LABELS[status];
  }

  shortId(id: string): string {
    return id.slice(0, 8).toUpperCase();
  }
}
