import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, NgZone, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
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
export class Dashboard implements OnInit, AfterViewInit, OnDestroy {
  sahaListesi: any[] = [];
  isLoading: boolean = false;
  
  @ViewChild('addressInput') addressInput!: ElementRef;
  @ViewChild('resultsSection') resultsSection!: ElementRef;

  // Arama filtreleri
  searchLocation: string = '';
  
  // Branş filtreleri (Çoklu Seçim)
  selectedSports: { [key: string]: boolean } = {
    'Futbol': false,
    'Basketbol': false,
    'Tenis': false,
    'Voleybol': false
  };

  distanceRange: number = 20; // Varsayılan mesafe (km)

  // Tarih aralığı
  startDate: string = '';
  endDate: string = '';

  // Saat aralığı
  startTime: string = '';
  endTime: string = '';

  // Fiyat aralığı
  minPrice: number | null = null;
  maxPrice: number | null = null;
  
  lat: number | null = null;
  lng: number | null = null;

  private searchSubject = new Subject<void>();
  private searchSubscription!: Subscription;

  constructor(
    private http: HttpClient, 
    private router: Router, 
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(500)
    ).subscribe(() => {
      this.onSearch();
    });
    
    this.sahalarıYukle();
  }

  ngOnDestroy() {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  ngAfterViewInit() {
    this.initAutocomplete();
  }

  initAutocomplete() {
    if (typeof google === 'undefined' || !google.maps || !google.maps.places) return;

    const autocomplete = new google.maps.places.Autocomplete(this.addressInput.nativeElement, {
      types: ['geocode', 'establishment'],
      componentRestrictions: { country: 'tr' }
    });

    autocomplete.addListener('place_changed', () => {
      this.ngZone.run(() => {
        const place = autocomplete.getPlace();
        if (place.geometry && place.geometry.location) {
          this.lat = place.geometry.location.lat();
          this.lng = place.geometry.location.lng();
          this.searchLocation = place.formatted_address || place.name || '';
          this.onSearch();
          this.scrollToResults();
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
            this.scrollToResults();
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
    this.isLoading = true;
    let url = `${environment.apiUrl}/Courts/search`;
    let queryParams = [];
    
    if (this.lat !== null && this.lng !== null) {
      queryParams.push(`lat=${this.lat}`);
      queryParams.push(`lng=${this.lng}`);
      queryParams.push(`distance=${this.distanceRange}`);
    }

    const selectedSportTypes = Object.keys(this.selectedSports).filter(key => this.selectedSports[key]);
    if (selectedSportTypes.length > 0) {
      // Backend tarafında sportTypes olarak virgülle ayrılmış bir liste beklenmesi olası, veya birden fazla sportType parametresi
      queryParams.push(`sportTypes=${encodeURIComponent(selectedSportTypes.join(','))}`);
    }

    if (this.startDate) queryParams.push(`startDate=${this.startDate}`);
    if (this.endDate) queryParams.push(`endDate=${this.endDate}`);
    if (this.startTime) queryParams.push(`startTime=${this.startTime}`);
    if (this.endTime) queryParams.push(`endTime=${this.endTime}`);
    if (this.minPrice !== null && this.minPrice !== undefined) queryParams.push(`minPrice=${this.minPrice}`);
    if (this.maxPrice !== null && this.maxPrice !== undefined) queryParams.push(`maxPrice=${this.maxPrice}`);

    if (queryParams.length > 0) {
      url += '?' + queryParams.join('&');
    }

    this.http.get(url).subscribe({
      next: (data: any) => {
        if (data && data.$values) this.sahaListesi = data.$values;
        else if (Array.isArray(data)) this.sahaListesi = data;
        else this.sahaListesi = [];
        this.isLoading = false;
        this.cdr.detectChanges(); // Change detection'ı manuel tetikle (takılmayı çözer)
      },
      error: (err: any) => {
        console.error("Sahalar yüklenirken hata:", err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getPhotos(court: any): string[] {
    if (!court) return [];
    
    let rawPhotos = court.photos || court.Photos;
    let photos = [];
    if (rawPhotos) {
      if (rawPhotos.$values) {
        photos = rawPhotos.$values;
      } else if (Array.isArray(rawPhotos)) {
        photos = rawPhotos;
      }
    }

    if (photos.length === 0) {
      let coverUrl = court.coverPhotoUrl || court.CoverPhotoUrl;
      if (coverUrl) return [coverUrl];
      return [];
    }
    
    // API might return an array of objects {url: '...'} or an array of strings
    return photos.map((p: any) => typeof p === 'string' ? p : (p.url || p.Url)).filter((url: string) => !!url);
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

  onFilterChange() {
    this.onSearch(); // Checkbox ve slider için anında çalıştır
  }

  onDebouncedFilterChange() {
    this.searchSubject.next(); // Inputlar için 500ms bekle
  }

  onSearch() {
    this.sahalarıYukle();
  }

  goToCourtDetail(id: string) {
    this.router.navigate(['/court-detail', id]);
  }

  scrollToResults() {
    if (this.resultsSection && this.resultsSection.nativeElement) {
      setTimeout(() => {
        const y = this.resultsSection.nativeElement.getBoundingClientRect().top + window.scrollY - 120;
        window.scrollTo({ top: y, behavior: 'smooth' });
      }, 100);
    }
  }
}
