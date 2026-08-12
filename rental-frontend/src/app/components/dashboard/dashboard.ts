import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, NgZone, ChangeDetectorRef, OnDestroy, HostListener } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FlatpickrModule } from 'angularx-flatpickr';
import { AlertModalComponent } from '../shared/alert-modal.component';

declare var google: any;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, FlatpickrModule, AlertModalComponent],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class Dashboard implements OnInit, AfterViewInit, OnDestroy {
  sahaListesi: any[] = [];
  isLoading: boolean = false;
  
  // Modal Props
  alertModalState = {
    isOpen: false,
    title: '',
    message: '',
    type: 'info' as 'success'|'error'|'warning'|'info',
    isConfirm: false
  };
  
  // Pagination & Sorting state
  page: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  sortBy: string = '';
  isFetchingMore: boolean = false;
  hasMoreData: boolean = true;
  isSortDropdownOpen: boolean = false;
  
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
          this.ngZone.run(() => {
            this.alertModalState = { isOpen: true, title: 'Hata', message: 'Konumunuz alınamadı. Lütfen tarayıcı izinlerinizi kontrol edin.', type: 'error', isConfirm: false };
          });
        }
      );
    } else {
      this.alertModalState = { isOpen: true, title: 'Hata', message: 'Tarayıcınız konum servisini desteklemiyor.', type: 'error', isConfirm: false };
    }
  }

  sahalarıYukle(isLoadMore: boolean = false) {
    if (isLoadMore) {
      if (this.isFetchingMore || !this.hasMoreData) return;
      this.isFetchingMore = true;
      this.page++;
    } else {
      this.isLoading = true;
      this.page = 1;
      this.hasMoreData = true;
      // this.sahaListesi = []; // İsterseniz skeleton gösterirken listeyi temizleyebilirsiniz
    }

    let url = `${environment.apiUrl}/Courts/search`;
    let queryParams = [];
    
    if (this.lat !== null && this.lng !== null) {
      queryParams.push(`lat=${this.lat}`);
      queryParams.push(`lng=${this.lng}`);
      queryParams.push(`distance=${this.distanceRange}`);
    }

    const selectedSportTypes = Object.keys(this.selectedSports).filter(key => this.selectedSports[key]);
    if (selectedSportTypes.length > 0) {
      queryParams.push(`sportTypes=${encodeURIComponent(selectedSportTypes.join(','))}`);
    }

    if (this.startDate) queryParams.push(`startDate=${this.startDate}`);
    if (this.endDate) queryParams.push(`endDate=${this.endDate}`);
    if (this.startTime) queryParams.push(`startTime=${this.startTime}`);
    if (this.endTime) queryParams.push(`endTime=${this.endTime}`);
    if (this.minPrice !== null && this.minPrice !== undefined) queryParams.push(`minPrice=${this.minPrice}`);
    if (this.maxPrice !== null && this.maxPrice !== undefined) queryParams.push(`maxPrice=${this.maxPrice}`);

    // Pagination & Sorting
    queryParams.push(`page=${this.page}`);
    queryParams.push(`pageSize=${this.pageSize}`);
    if (this.sortBy) queryParams.push(`sortBy=${this.sortBy}`);

    if (queryParams.length > 0) {
      url += '?' + queryParams.join('&');
    }

    this.http.get(url).subscribe({
      next: (data: any) => {
        let newItems = [];
        
        let itemsField = data.items || data.Items;
        let countField = data.totalCount !== undefined ? data.totalCount : data.TotalCount;
        
        if (itemsField) {
           newItems = itemsField.$values || itemsField;
           this.totalCount = countField || 0;
        } else if (data && data.$values) {
           newItems = data.$values;
        } else if (Array.isArray(data)) {
           newItems = data;
        }

        if (newItems.length < this.pageSize) {
           this.hasMoreData = false;
        }

        if (isLoadMore) {
           this.sahaListesi = [...this.sahaListesi, ...newItems];
           this.isFetchingMore = false;
        } else {
           this.sahaListesi = newItems;
           this.isLoading = false;
        }

        this.cdr.detectChanges(); // Change detection'ı manuel tetikle (takılmayı çözer)
      },
      error: (err: any) => {
        console.error("Sahalar yüklenirken hata:", err);
        if (isLoadMore) this.isFetchingMore = false;
        else this.isLoading = false;
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

  onSearchAndScroll() {
    this.onSearch();
    this.scrollToResults();
  }

  goToCourtDetail(id: string) {
    this.router.navigate(['/court-detail', id]);
  }

  scrollToResults() {
    if (this.resultsSection && this.resultsSection.nativeElement) {
      setTimeout(() => {
        const y = this.resultsSection.nativeElement.getBoundingClientRect().top + window.scrollY - 100;
        window.scrollTo({ top: y, behavior: 'smooth' });
      }, 100);
    }
  }

  // Yükleme sırasında infinite scroll
  @HostListener('window:scroll')
  onScroll() {
    if (this.isLoading || this.isFetchingMore || !this.hasMoreData) return;
    
    // Yüksekliğe yaklaşıldığında tetikle (bottom offset 200px)
    if (window.innerHeight + window.scrollY >= document.body.offsetHeight - 200) {
      this.sahalarıYukle(true);
    }
  }

  // Sıralama Menüsü Yönetimi
  toggleSortDropdown(event: Event) {
    event.stopPropagation();
    this.isSortDropdownOpen = !this.isSortDropdownOpen;
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.isSortDropdownOpen = false;
  }

  selectSort(sortOption: string) {
    this.sortBy = sortOption;
    this.isSortDropdownOpen = false;
    this.onSearch(); // Baştan yükle
  }
}
