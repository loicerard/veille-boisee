import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ReportApi, ReportStatus } from '../locate/report-api';
import { SavedReport, SavedReportsService } from './saved-reports.service';

interface DisplayReport extends SavedReport {
  status: ReportStatus | null;
}

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
  private readonly savedReports = inject(SavedReportsService);
  private readonly reportApi = inject(ReportApi);

  readonly reports = signal<DisplayReport[]>(
    this.savedReports.getAll().map((r) => ({ ...r, status: null })),
  );

  ngOnInit(): void {
    this.reports().forEach((report, index) => {
      this.reportApi.getStatus(report.id).subscribe((status) => {
        this.reports.update((list) => {
          const updated = [...list];
          updated[index] = { ...updated[index], status };
          return updated;
        });
      });
    });
  }

  statusLabel(status: ReportStatus | null): string {
    return status ? STATUS_LABELS[status] : '…';
  }

  shortId(id: string): string {
    return id.slice(0, 8).toUpperCase();
  }
}
