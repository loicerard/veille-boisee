import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  DashboardReportApi,
  ListFilters,
  REPORT_STATUS_LABELS,
  ReportPage,
  ReportStatus,
} from '../dashboard-report-api';

type ViewState =
  | { kind: 'loading' }
  | { kind: 'loaded'; page: ReportPage }
  | { kind: 'empty' }
  | { kind: 'error' };

@Component({
  selector: 'app-report-list',
  imports: [RouterLink, ReactiveFormsModule, DatePipe],
  templateUrl: './report-list.html',
  styleUrl: './report-list.scss',
})
export class ReportList implements OnInit {
  private readonly api = inject(DashboardReportApi);
  private readonly fb = inject(FormBuilder);

  readonly state = signal<ViewState>({ kind: 'loading' });
  readonly currentPage = signal(1);
  readonly pageSize = 20;
  readonly statusLabels: Record<number, string> = REPORT_STATUS_LABELS;
  readonly exporting = signal(false);

  readonly filterForm = this.fb.group({
    status: [null as ReportStatus | null],
    from: [null as string | null],
    to: [null as string | null],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.state.set({ kind: 'loading' });
    const filters = this.buildFilters();
    this.api.list(this.currentPage(), this.pageSize, filters).subscribe((outcome) => {
      if (outcome.status === 'serverError') {
        this.state.set({ kind: 'error' });
        return;
      }
      if (outcome.data.totalCount === 0) {
        this.state.set({ kind: 'empty' });
        return;
      }
      this.state.set({ kind: 'loaded', page: outcome.data });
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.currentPage.set(page);
    this.load();
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.api.exportCsv(this.buildFilters()).subscribe((outcome) => {
      this.exporting.set(false);
      if (outcome.status !== 'exported') return;
      const url = URL.createObjectURL(outcome.blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = outcome.filename;
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  totalPages(totalCount: number): number {
    return Math.ceil(totalCount / this.pageSize);
  }

  private buildFilters(): ListFilters {
    const { status, from, to } = this.filterForm.value;
    return {
      status: status ?? null,
      from: from ?? null,
      to: to ?? null,
    };
  }
}
