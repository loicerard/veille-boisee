import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  output,
} from '@angular/core';
import * as L from 'leaflet';

export interface PickedCoordinates {
  latitude: number;
  longitude: number;
}

// Fix Leaflet's broken default marker icons when bundled by Webpack / esbuild.
const defaultIcon = L.icon({
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
  selector: 'app-map-picker',
  imports: [],
  templateUrl: './map-picker.html',
  styleUrl: './map-picker.scss',
})
export class MapPicker implements AfterViewInit, OnDestroy {
  readonly coordinatesPicked = output<PickedCoordinates>();

  @ViewChild('mapContainer', { static: true })
  private readonly mapContainer!: ElementRef<HTMLDivElement>;

  private map?: L.Map;
  private marker?: L.Marker;

  ngAfterViewInit(): void {
    this.map = L.map(this.mapContainer.nativeElement).setView([46.5, 2.5], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(this.map);

    this.map.on('click', (event: L.LeafletMouseEvent) => {
      this.placeMarker(event.latlng);
      this.coordinatesPicked.emit({
        latitude: event.latlng.lat,
        longitude: event.latlng.lng,
      });
    });
  }

  placeAt(latitude: number, longitude: number): void {
    const latlng = L.latLng(latitude, longitude);
    this.map?.setView(latlng, 13);
    this.placeMarker(latlng);
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  private placeMarker(latlng: L.LatLng): void {
    if (this.marker) {
      this.marker.setLatLng(latlng);
    } else {
      this.marker = L.marker(latlng, { icon: defaultIcon }).addTo(this.map!);
    }
  }
}
