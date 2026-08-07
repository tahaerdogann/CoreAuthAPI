import { Component, Input, Output, EventEmitter, OnInit, AfterViewInit, OnDestroy, ElementRef, ViewChild, OnChanges, SimpleChanges, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface MapAddressResult {
  city: string;
  district: string;
  neighborhood: string;
  fullAddress: string;
  latitude: number;
  longitude: number;
}

declare var google: any;

@Component({
  selector: 'app-map-picker',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="map-wrapper">
      <!-- Arama ve Konum Butonları (sadece picker modunda) -->
      <div class="map-controls" *ngIf="mode === 'picker'">
        <div class="search-container">
          <input #searchInput type="text" class="map-search-input" placeholder="Adres veya yer adı arayın...">
          <span class="search-icon">🔍</span>
        </div>
        <button type="button" class="btn-locate" (click)="getCurrentLocation()" [disabled]="isLocating">
          <span class="locate-icon">📍</span>
          {{ isLocating ? 'Konum alınıyor...' : 'Konumumu Bul' }}
        </button>
      </div>
      
      <!-- Harita -->
      <div #mapContainer class="map-container" [class.map-display]="mode === 'display'"></div>
      
      <!-- Konum bulunamadı uyarısı -->
      <div class="map-error" *ngIf="locationError">
        ⚠️ {{ locationError }}
      </div>
    </div>
  `,
  styles: [`
    .map-wrapper {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .map-controls {
      display: flex;
      gap: 10px;
      align-items: stretch;
    }
    .search-container {
      flex: 1;
      position: relative;
    }
    .map-search-input {
      width: 100%;
      padding: 10px 12px 10px 36px;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      font-size: 14px;
      outline: none;
      transition: border-color 0.2s;
      box-sizing: border-box;
      font-family: 'Inter', sans-serif;
    }
    .map-search-input:focus {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }
    .search-icon {
      position: absolute;
      left: 10px;
      top: 50%;
      transform: translateY(-50%);
      font-size: 16px;
      pointer-events: none;
    }
    .btn-locate {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 10px 16px;
      background: linear-gradient(135deg, #10b981, #059669);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      white-space: nowrap;
      transition: all 0.2s;
      font-family: 'Inter', sans-serif;
    }
    .btn-locate:hover:not([disabled]) {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
    }
    .btn-locate[disabled] {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .locate-icon {
      font-size: 16px;
    }
    .map-container {
      width: 100%;
      height: 350px;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      overflow: hidden;
    }
    .map-container.map-display {
      height: 250px;
    }
    .map-error {
      background: #fef2f2;
      color: #991b1b;
      padding: 8px 12px;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 500;
      border: 1px solid #fecaca;
    }

    /* Google Places Autocomplete dropdown styling */
    :host ::ng-deep .pac-container {
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
      font-family: 'Inter', sans-serif;
      margin-top: 4px;
    }
    :host ::ng-deep .pac-item {
      padding: 8px 12px;
      font-size: 14px;
      cursor: pointer;
    }
    :host ::ng-deep .pac-item:hover {
      background: #f1f5f9;
    }
  `]
})
export class MapPickerComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() mode: 'picker' | 'display' = 'picker';
  @Input() latitude: number | null = null;
  @Input() longitude: number | null = null;

  @Output() addressSelected = new EventEmitter<MapAddressResult>();

  @ViewChild('mapContainer') mapContainer!: ElementRef;
  @ViewChild('searchInput') searchInput!: ElementRef;

  private map: any;
  private marker: any;
  private geocoder: any;
  private autocomplete: any;

  isLocating = false;
  locationError: string | null = null;

  // Varsayılan konum: Türkiye merkezi
  private defaultLat = 39.9334;
  private defaultLng = 32.8597;
  private defaultZoom = 6;

  constructor(private ngZone: NgZone) {}

  ngAfterViewInit() {
    this.waitForGoogleMaps().then(() => {
      this.initMap();
    });
    window.addEventListener('scroll', this.onScroll, true);
  }

  ngOnDestroy() {
    window.removeEventListener('scroll', this.onScroll, true);
  }

  private onScroll = (event: any) => {
    // Sayfa kaydırıldığında input odağını kaldır, böylece sabit kalan dropdown kapanır
    if (this.searchInput && document.activeElement === this.searchInput.nativeElement) {
      this.searchInput.nativeElement.blur();
    }
  };

  ngOnChanges(changes: SimpleChanges) {
    if ((changes['latitude'] || changes['longitude']) && this.map) {
      if (this.latitude && this.longitude) {
        const pos = { lat: this.latitude, lng: this.longitude };
        this.map.setCenter(pos);
        this.map.setZoom(15);
        this.updateMarker(pos);
      }
    }
  }

  private waitForGoogleMaps(): Promise<void> {
    return new Promise((resolve) => {
      if (typeof google !== 'undefined' && google.maps) {
        resolve();
        return;
      }
      const interval = setInterval(() => {
        if (typeof google !== 'undefined' && google.maps) {
          clearInterval(interval);
          resolve();
        }
      }, 100);
    });
  }

  private initMap() {
    const lat = this.latitude || this.defaultLat;
    const lng = this.longitude || this.defaultLng;
    const zoom = (this.latitude && this.longitude) ? 15 : this.defaultZoom;

    this.map = new google.maps.Map(this.mapContainer.nativeElement, {
      center: { lat, lng },
      zoom,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: true,
      zoomControl: true,
      styles: [
        { featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] }
      ]
    });

    this.geocoder = new google.maps.Geocoder();

    // Eğer başlangıç koordinatı varsa marker koy
    if (this.latitude && this.longitude) {
      this.updateMarker({ lat: this.latitude, lng: this.longitude });
    }

    if (this.mode === 'picker') {
      // Haritaya tıklama ile pin koyma
      this.map.addListener('click', (e: any) => {
        const pos = { lat: e.latLng.lat(), lng: e.latLng.lng() };
        this.updateMarker(pos);
        this.reverseGeocode(pos);
      });

      // Places Autocomplete
      this.initAutocomplete();
    }
  }

  private initAutocomplete() {
    if (!this.searchInput) return;

    this.autocomplete = new google.maps.places.Autocomplete(
      this.searchInput.nativeElement,
      {
        types: ['geocode', 'establishment'],
        componentRestrictions: { country: 'tr' },
        fields: ['geometry', 'formatted_address', 'address_components']
      }
    );

    this.autocomplete.addListener('place_changed', () => {
      this.ngZone.run(() => {
        const place = this.autocomplete.getPlace();
        if (!place.geometry || !place.geometry.location) {
          this.locationError = 'Seçilen yer için konum bilgisi bulunamadı.';
          return;
        }

        this.locationError = null;
        const pos = {
          lat: place.geometry.location.lat(),
          lng: place.geometry.location.lng()
        };

        this.map.setCenter(pos);
        this.map.setZoom(17);
        this.updateMarker(pos);
        this.processAddressComponents(place.address_components, place.formatted_address, pos);
      });
    });
  }

  private updateMarker(pos: { lat: number; lng: number }) {
    if (this.marker) {
      this.marker.setPosition(pos);
    } else {
      this.marker = new google.maps.Marker({
        position: pos,
        map: this.map,
        draggable: this.mode === 'picker',
        animation: google.maps.Animation.DROP
      });

      // Sürükleme bittiğinde reverse geocode
      if (this.mode === 'picker') {
        this.marker.addListener('dragend', (e: any) => {
          this.ngZone.run(() => {
            const newPos = { lat: e.latLng.lat(), lng: e.latLng.lng() };
            this.reverseGeocode(newPos);
          });
        });
      }
    }
  }

  private reverseGeocode(pos: { lat: number; lng: number }) {
    this.geocoder.geocode({ location: pos }, (results: any[], status: string) => {
      this.ngZone.run(() => {
        if (status === 'OK' && results && results.length > 0) {
          const result = results[0];
          this.processAddressComponents(
            result.address_components,
            result.formatted_address,
            pos
          );
        } else {
          // Geocode başarısız olsa bile koordinatları gönder
          this.addressSelected.emit({
            city: '',
            district: '',
            neighborhood: '',
            fullAddress: '',
            latitude: pos.lat,
            longitude: pos.lng
          });
        }
      });
    });
  }

  private processAddressComponents(components: any[], formattedAddress: string, pos: { lat: number; lng: number }) {
    let city = '';
    let district = '';
    let neighborhood = '';

    // Google Maps Türkiye için adres bileşenleri:
    // administrative_area_level_1 = İl
    // administrative_area_level_2 = İlçe  
    // neighborhood veya sublocality = Mahalle
    for (const comp of components) {
      const types = comp.types;
      if (types.includes('administrative_area_level_1')) {
        city = comp.long_name;
      } else if (types.includes('administrative_area_level_2')) {
        district = comp.long_name;
      } else if (types.includes('neighborhood') || types.includes('sublocality') || types.includes('sublocality_level_1') || types.includes('administrative_area_level_3') || types.includes('administrative_area_level_4') || types.includes('locality')) {
        // Eğer içinde 'mah' geçiyorsa önceliği ona ver (Google bazen farklı kategorilere atayabiliyor)
        if (!neighborhood || comp.long_name.toLowerCase().includes('mah')) {
          neighborhood = comp.long_name;
        }
      }
    }

    // Hala mahalle bulunamadıysa isim bazlı son bir arama yap
    if (!neighborhood) {
      const mahComponent = components.find(c => c.long_name.toLowerCase().includes('mah') || c.long_name.toLowerCase().includes('köy'));
      if (mahComponent) {
        neighborhood = mahComponent.long_name;
      }
    }

    // Açık adres: formattedAddress'den İl, İlçe, Mahalle, ülke bilgilerini çıkar
    // ve kalan kısmı sokak/cadde bilgisi olarak kullan
    let streetAddress = formattedAddress || '';
    // Türkiye, posta kodu gibi genel bilgileri çıkar
    streetAddress = streetAddress.replace(/,?\s*T(ü|u)rkiye\s*$/i, '');
    streetAddress = streetAddress.replace(/,?\s*\d{5}\s*/g, ''); // posta kodu
    if (city) streetAddress = streetAddress.replace(new RegExp(',?\\s*' + city.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&') + '\\s*', 'g'), '');
    if (district) streetAddress = streetAddress.replace(new RegExp(',?\\s*' + district.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&') + '\\s*', 'g'), '');
    if (neighborhood) streetAddress = streetAddress.replace(new RegExp(',?\\s*' + neighborhood.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&') + '\\s*', 'g'), '');
    streetAddress = streetAddress.replace(/^[,\s]+|[,\s]+$/g, '').trim();

    this.addressSelected.emit({
      city,
      district,
      neighborhood,
      fullAddress: streetAddress || formattedAddress,
      latitude: pos.lat,
      longitude: pos.lng
    });
  }

  getCurrentLocation() {
    if (!navigator.geolocation) {
      this.locationError = 'Tarayıcınız konum özelliğini desteklemiyor.';
      return;
    }

    this.isLocating = true;
    this.locationError = null;

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.ngZone.run(() => {
          const pos = {
            lat: position.coords.latitude,
            lng: position.coords.longitude
          };
          this.map.setCenter(pos);
          this.map.setZoom(17);
          this.updateMarker(pos);
          this.reverseGeocode(pos);
          this.isLocating = false;
        });
      },
      (error) => {
        this.ngZone.run(() => {
          this.isLocating = false;
          switch (error.code) {
            case error.PERMISSION_DENIED:
              this.locationError = 'Konum izni reddedildi. Lütfen tarayıcı ayarlarından konum iznini açın.';
              break;
            case error.POSITION_UNAVAILABLE:
              this.locationError = 'Konum bilgisi alınamadı.';
              break;
            case error.TIMEOUT:
              this.locationError = 'Konum isteği zaman aşımına uğradı.';
              break;
            default:
              this.locationError = 'Bilinmeyen bir hata oluştu.';
          }
        });
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
  }
}
