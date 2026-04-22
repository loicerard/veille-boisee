import { Component, ViewChild, inject, signal } from '@angular/core';

import { SavedReportsService } from '../../my-reports/saved-reports.service';
import { CommuneApi, CommuneLookupOutcome, CommuneResponse } from '../commune-api';
import { MapPicker, PickedCoordinates } from '../map-picker/map-picker';
import { ReportForm } from '../report-form/report-form';

type ViewState =
  | { kind: 'idle' }
  | { kind: 'querying' }
  | { kind: 'result'; outcome: CommuneLookupOutcome; latitude: number; longitude: number }
  | { kind: 'geolocationDenied' }
  | { kind: 'geolocationUnavailable' }
  | { kind: 'geolocationInsecureContext' }
  | { kind: 'geolocationTimeout' }
  | { kind: 'reporting'; commune: CommuneResponse; latitude: number; longitude: number }
  | { kind: 'submitted'; reportId: string };

@Component({
  selector: 'app-locate',
  imports: [MapPicker, ReportForm],
  templateUrl: './locate.html',
  styleUrl: './locate.scss',
})
export class Locate {
  @ViewChild(MapPicker) private readonly mapPicker!: MapPicker;

  private readonly communeApi = inject(CommuneApi);
  private readonly savedReports = inject(SavedReportsService);

  readonly state = signal<ViewState>({ kind: 'idle' });

  onCoordinatesPicked(coords: PickedCoordinates): void {
    this.queryCommune(coords.latitude, coords.longitude);
  }

  useCurrentPosition(): void {
    if (!window.isSecureContext) {
      this.state.set({ kind: 'geolocationInsecureContext' });
      return;
    }

    if (!navigator.geolocation) {
      this.state.set({ kind: 'geolocationUnavailable' });
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const { latitude, longitude } = position.coords;
        this.mapPicker.placeAt(latitude, longitude);
        this.queryCommune(latitude, longitude);
      },
      (error) => {
        if (error.code === GeolocationPositionError.TIMEOUT) {
          this.state.set({ kind: 'geolocationTimeout' });
        } else if (error.code === GeolocationPositionError.POSITION_UNAVAILABLE) {
          this.state.set({ kind: 'geolocationUnavailable' });
        } else {
          this.state.set({ kind: 'geolocationDenied' });
        }
      },
      { enableHighAccuracy: true, timeout: 10000 },
    );
  }

  startReporting(): void {
    const s = this.state();
    if (s.kind !== 'result' || s.outcome.status !== 'found') return;
    this.state.set({ kind: 'reporting', commune: s.outcome.commune, latitude: s.latitude, longitude: s.longitude });
  }

  onReportSubmitted(reportId: string): void {
    const s = this.state();
    if (s.kind === 'reporting') {
      this.savedReports.save({
        id: reportId,
        communeName: s.commune.name,
        submittedAt: new Date().toISOString(),
      });
    }
    this.state.set({ kind: 'submitted', reportId });
  }

  resetToIdle(): void {
    this.state.set({ kind: 'idle' });
  }

  private queryCommune(latitude: number, longitude: number): void {
    this.state.set({ kind: 'querying' });
    this.communeApi
      .findByCoordinates(latitude, longitude)
      .subscribe((outcome) => this.state.set({ kind: 'result', outcome, latitude, longitude }));
  }
}
