import { Injectable } from '@angular/core';

export interface SavedReport {
  id: string;
  communeName: string;
  submittedAt: string;
}

const STORAGE_KEY = 'vb_saved_reports';

@Injectable({ providedIn: 'root' })
export class SavedReportsService {
  save(report: SavedReport): void {
    const existing = this.getAll();
    existing.unshift(report);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(existing));
  }

  getAll(): SavedReport[] {
    try {
      return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]') as SavedReport[];
    } catch {
      return [];
    }
  }

  remove(id: string): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.getAll().filter((r) => r.id !== id)));
  }
}
