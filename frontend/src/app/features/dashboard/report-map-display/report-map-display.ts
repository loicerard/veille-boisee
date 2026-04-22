import { AfterViewInit, Component, ElementRef, Input, OnDestroy, ViewChild } from '@angular/core';
import * as L from 'leaflet';

const markerIcon = L.icon({
  iconUrl: 'assets/marker-icon.png',
  iconRetinaUrl: 'assets/marker-icon-2x.png',
  shadowUrl: 'assets/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  tooltipAnchor: [16, -28],
  shadowSize: [41, 41],
});

@Component({
  selector: 'app-report-map-display',
  imports: [],
  templateUrl: './report-map-display.html',
  styleUrl: './report-map-display.scss',
})
export class ReportMapDisplay implements AfterViewInit, OnDestroy {
  @Input({ required: true }) latitude!: number;
  @Input({ required: true }) longitude!: number;

  @ViewChild('mapContainer', { static: true })
  private readonly mapContainer!: ElementRef<HTMLDivElement>;

  private map?: L.Map;

  ngAfterViewInit(): void {
    this.map = L.map(this.mapContainer.nativeElement, { zoomControl: true }).setView(
      [this.latitude, this.longitude],
      14,
    );

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(this.map);

    L.marker([this.latitude, this.longitude], { icon: markerIcon, draggable: false }).addTo(this.map);
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }
}
