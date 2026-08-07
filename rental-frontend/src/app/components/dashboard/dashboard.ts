import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

declare var google: any;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class Dashboard implements OnInit, AfterViewInit {
  sahaListesi: any[] = [];
  
  @ViewChild('addressInput') addressInput!: ElementRef;

  // Arama filtreleri
  searchLocation: string = '';
  searchSportType: string = '';
  
  lat: number | null = null;
  lng: number | null = null;

  constructor(private http: HttpClient, private router: Router, private ngZone: NgZone) { }

  ngOnInit() {
    this.sahalarıYukle();
  }

  ngAfterViewInit() {
    this.initAutocomplete();
  }

  initAutocomplete() {
    if (typeof google === 'undefined' || !google.maps || !google.maps.places) return;

    const autocomplete = new google.maps.places.Autocomplete(this.addressInput.nativeElement, {
      types: ['geocode', 'establishment']
    });

    autocomplete.addListener('place_changed', () => {
      this.ngZone.run(() => {
        const place = autocomplete.getPlace();
        if (place.geometry && place.geometry.location) {
          this.lat = place.geometry.location.lat();
          this.lng = place.geometry.location.lng();
          this.searchLocation = place.formatted_address || place.name || '';
          this.onSearch();
        }
      });
    });
  }

  getUserLocation() {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.ngZone.run(() => {
            this.lat = position.coords.latitude;
            this.lng = position.coords.longitude;
            this.searchLocation = 'Mevcut Konumum';
            this.onSearch();
          });
        },
        (error) => {
          console.error("Konum alınamadı:", error);
          alert("Konumunuz alınamadı. Lütfen tarayıcı izinlerinizi kontrol edin.");
        }
      );
    } else {
      alert("Tarayıcınız konum servisini desteklemiyor.");
    }
  }

  sahalarıYukle() {
    let url = `${environment.apiUrl}/Courts/search`;
    let queryParams = [];
    
    if (this.lat !== null && this.lng !== null) {
      queryParams.push(`lat=${this.lat}`);
      queryParams.push(`lng=${this.lng}`);
    }
    if (this.searchSportType) {
      queryParams.push(`sportType=${encodeURIComponent(this.searchSportType)}`);
    }

    if (queryParams.length > 0) {
      url += '?' + queryParams.join('&');
    }

    this.http.get(url).subscribe({
      next: (data: any) => {
        if (data && data.$values) this.sahaListesi = data.$values;
        else if (Array.isArray(data)) this.sahaListesi = data;
        else this.sahaListesi = [];
      },
      error: (err: any) => console.error("Sahalar yüklenirken hata:", err)
    });
  }

  getPhotos(court: any): string[] {
    if (!court || !court.photos) return [];
    
    let photos = [];
    if (court.photos.$values) {
      photos = court.photos.$values;
    } else if (Array.isArray(court.photos)) {
      photos = court.photos;
    }

    if (photos.length === 0) return [];
    return photos.map((p: any) => p.url || p.Url).filter((url: string) => !!url);
  }

  nextPhoto(event: Event, court: any) {
    event.stopPropagation();
    const photos = this.getPhotos(court);
    if (photos.length <= 1) return;
    if (court._currentPhotoIndex === undefined) court._currentPhotoIndex = 0;
    court._currentPhotoIndex = (court._currentPhotoIndex + 1) % photos.length;
  }

  prevPhoto(event: Event, court: any) {
    event.stopPropagation();
    const photos = this.getPhotos(court);
    if (photos.length <= 1) return;
    if (court._currentPhotoIndex === undefined) court._currentPhotoIndex = 0;
    court._currentPhotoIndex = (court._currentPhotoIndex - 1 + photos.length) % photos.length;
  }

  onSearch() {
    this.sahalarıYukle();
  }

  goToCourtDetail(id: string) {
    this.router.navigate(['/court-detail', id]);
  }
}
