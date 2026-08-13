import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth';
import { FavoriteService } from '../../services/favorite';
import { MapPickerComponent } from '../shared/map-picker.component';
import { AlertModalComponent } from '../shared/alert-modal.component';

@Component({
  selector: 'app-court-detail',
  standalone: true,
  imports: [CommonModule, MapPickerComponent, AlertModalComponent],
  templateUrl: './court-detail.html',
  styleUrls: ['./court-detail.css']
})
export class CourtDetailComponent implements OnInit {
  courtId: string = '';
  court: any = null;
  slots: any[] = [];
  isLoading: boolean = true;
  bookingError: string = '';
  bookingSuccess: string = '';
  
  isFavorite: boolean = false;
  confirmModalVisible: boolean = false;
  slotToBook: any = null;

  // Yeni Eklenen State Değişkenleri
  availableDates: string[] = [];
  selectedDate: string = '';
  filteredSlots: any[] = [];

  alertModalState = {
    isOpen: false,
    title: '',
    message: '',
    type: 'success' as 'success' | 'error' | 'warning' | 'info',
    isConfirm: false
  };

  lightboxVisible: boolean = false;
  currentPhotoIndex: number = 0;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private authService: AuthService,
    private favoriteService: FavoriteService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.courtId = idParam;
        this.loadCourtDetails();
        this.loadCourtSlots();
        this.checkFavoriteStatus();
      } else {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  pageError: string = '';

  loadCourtDetails() {
    this.http.get(`${environment.apiUrl}/Courts/${this.courtId}`).subscribe({
      next: (data: any) => {
        this.court = data;
        
        // Parse Rental Options
        try {
          if (this.court.rentalOptionsJson) {
            this.court.parsedRentalOptions = JSON.parse(this.court.rentalOptionsJson);
          }
        } catch(e) {}

        // Parse Amenities (Bitwise)
        this.court.parsedAmenities = this.getAmenitiesList(this.court.amenities || 0);

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Saha detayları yüklenemedi:', err);
        this.pageError = err.error?.message || 'Saha bulunamadı veya bir hata oluştu.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getAmenitiesList(flags: number): { text: string }[] {
    const list: { text: string }[] = [];
    if (flags & 1) list.push({ text: 'WC / Lavabo' });
    if (flags & 2) list.push({ text: 'Kafe / Büfe' });
    if (flags & 4) list.push({ text: 'Engelli Erişimi' });
    if (flags & 8) list.push({ text: 'Soyunma Odası' });
    if (flags & 16) list.push({ text: 'Ücretsiz Wi-Fi' });
    if (flags & 32) list.push({ text: 'Duş' });
    if (flags & 64) list.push({ text: 'Kilitli Dolap' });
    if (flags & 128) list.push({ text: 'Tribün' });
    if (flags & 256) list.push({ text: 'Klima' });
    if (flags & 512) list.push({ text: 'Mescit' });
    if (flags & 1024) list.push({ text: 'Gece Aydınlatması' });
    return list;
  }

  loadCourtSlots() {
    this.http.get(`${environment.apiUrl}/Courts/slots/${this.courtId}`).subscribe({
      next: (data: any) => {
        this.slots = data?.$values || data || [];
        this.groupSlotsByDate();
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Seanslar yüklenemedi:', err)
    });
  }

  checkFavoriteStatus() {
    if (this.authService.isLoggedIn()) {
      console.log('checkFavoriteStatus: API isteği atılıyor...');
      this.favoriteService.checkFavorite(this.courtId).subscribe({
        next: (res) => {
          console.log('checkFavoriteStatus: API Yanıtı:', res);
          this.isFavorite = res.isFavorite;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('checkFavoriteStatus: API Hatası:', err);
          this.cdr.detectChanges();
        }
      });
    } else {
      console.log('checkFavoriteStatus: Kullanıcı giriş yapmamış, kontrol atlandı.');
    }
  }

  toggleFavorite() {
    console.log('toggleFavorite tetiklendi! CourtId:', this.courtId);
    if (!this.authService.isLoggedIn()) {
      console.log('Kullanıcı giriş yapmamış, logine yönlendiriliyor.');
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/court-detail/${this.courtId}` } });
      return;
    }
    
    console.log('API isteği atılıyor...');
    this.favoriteService.toggleFavorite(this.courtId).subscribe({
      next: (res) => {
        console.log('API Başarılı Yanıt Döndü:', res);
        this.isFavorite = res.isFavorite;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('API Hata Döndü:', err);
        this.cdr.detectChanges();
      }
    });
  }

  openConfirmModal(slot: any) {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/court-detail/${this.courtId}` } });
      return;
    }
    this.slotToBook = slot;
    this.confirmModalVisible = true;
  }

  closeConfirmModal() {
    this.confirmModalVisible = false;
    this.slotToBook = null;
  }

  showAlert(title: string, message: string, type: 'success' | 'error' | 'warning' | 'info') {
    this.alertModalState = {
      isOpen: true,
      title,
      message,
      type,
      isConfirm: false
    };
  }

  bookSlot() {
    if (!this.slotToBook) return;
    
    this.bookingError = '';
    this.bookingSuccess = '';

    const payload = { slotId: this.slotToBook.id };
    
    this.http.post(`${environment.apiUrl}/Bookings/create`, payload).subscribe({
      next: (res: any) => {
        this.closeConfirmModal();
        this.showAlert('Başarılı!', res.message || 'Kiralama başarılı!', 'success');
        this.loadCourtSlots(); // Takvimi yenile
      },
      error: (err) => {
        this.closeConfirmModal();
        this.showAlert('Hata!', err.error?.message || err.error || 'Kiralama sırasında bir hata oluştu.', 'error');
      }
    });
  }

  // YENİ EKLENEN METOTLAR (Tarih Sekmeleri)
  groupSlotsByDate() {
    if (!this.slots || this.slots.length === 0) {
      this.availableDates = [];
      this.filteredSlots = [];
      return;
    }

    const uniqueDates = new Set<string>();
    this.slots.forEach(slot => {
      if (slot.startTime) {
        const dateStr = new Date(slot.startTime).toLocaleDateString('tr-TR', { year: 'numeric', month: '2-digit', day: '2-digit' });
        uniqueDates.add(dateStr);
      }
    });

    // string array'e çevir
    this.availableDates = Array.from(uniqueDates).sort((a, b) => {
      const [dayA, monthA, yearA] = a.split('.');
      const [dayB, monthB, yearB] = b.split('.');
      return new Date(+yearA, +monthA - 1, +dayA).getTime() - new Date(+yearB, +monthB - 1, +dayB).getTime();
    });

    if (this.availableDates.length > 0) {
      if (!this.availableDates.includes(this.selectedDate)) {
        this.selectedDate = this.availableDates[0];
      }
      this.filterSlotsByDate(this.selectedDate);
    } else {
      this.filteredSlots = [];
    }
  }

  filterSlotsByDate(dateStr: string) {
    this.selectedDate = dateStr;
    this.filteredSlots = this.slots.filter(slot => {
      if (!slot.startTime) return false;
      const sDate = new Date(slot.startTime).toLocaleDateString('tr-TR', { year: 'numeric', month: '2-digit', day: '2-digit' });
      return sDate === dateStr;
    });
  }

  getDateLabel(dateStr: string): string {
    const parts = dateStr.split('.');
    if (parts.length !== 3) return dateStr;
    
    const [d, m, y] = parts;
    const dateObj = new Date(+y, +m - 1, +d);
    dateObj.setHours(0, 0, 0, 0);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (dateObj.getTime() === today.getTime()) return 'Bugün';
    if (dateObj.getTime() === tomorrow.getTime()) return 'Yarın';

    // Diğer günler için kısa isimler
    return dateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });
  }

  // YENİ EKLENEN METOTLAR (Lightbox)
  get photosArray(): any[] {
    return this.court?.photos?.$values || this.court?.photos || [];
  }

  openLightbox(index: number = 0) {
    if (this.photosArray.length > 0) {
      this.currentPhotoIndex = index;
      this.lightboxVisible = true;
      document.body.style.overflow = 'hidden'; // arkayı kaydırmayı engelle
    }
  }

  closeLightbox() {
    this.lightboxVisible = false;
    document.body.style.overflow = 'auto';
  }

  nextPhoto() {
    if (this.currentPhotoIndex < this.photosArray.length - 1) {
      this.currentPhotoIndex++;
    } else {
      this.currentPhotoIndex = 0; // başa dön
    }
  }

  prevPhoto() {
    if (this.currentPhotoIndex > 0) {
      this.currentPhotoIndex--;
    } else {
      this.currentPhotoIndex = this.photosArray.length - 1; // sona git
    }
  }

  // YENİ EKLENEN: Scroll to Section metodu
  scrollTo(sectionId: string) {
    const element = document.getElementById(sectionId);
    if (element) {
      const yOffset = -100; 
      const y = element.getBoundingClientRect().top + window.scrollY + yOffset;
      window.scrollTo({ top: y, behavior: 'smooth' });
    }
  }
}
