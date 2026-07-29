import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-court-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './court-detail.html',
  styleUrls: ['./court-detail.css']
})
export class CourtDetailComponent implements OnInit {
  courtId: number = 0;
  court: any = null;
  slots: any[] = [];
  isLoading: boolean = true;
  bookingError: string = '';
  bookingSuccess: string = '';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.courtId = +idParam;
        this.loadCourtDetails();
        this.loadCourtSlots();
      } else {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

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
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getAmenitiesList(flags: number): {icon: string, text: string}[] {
    const list: {icon: string, text: string}[] = [];
    if (flags & 1) list.push({icon: '🚻', text: 'WC / Lavabo'});
    if (flags & 2) list.push({icon: '☕', text: 'Kafe / Büfe'});
    if (flags & 4) list.push({icon: '♿', text: 'Engelli Erişimi'});
    if (flags & 8) list.push({icon: '👕', text: 'Soyunma Odası'});
    if (flags & 16) list.push({icon: '📶', text: 'Ücretsiz Wi-Fi'});
    if (flags & 32) list.push({icon: '🚿', text: 'Duş'});
    if (flags & 64) list.push({icon: '🔒', text: 'Kilitli Dolap'});
    if (flags & 128) list.push({icon: '🏟️', text: 'Tribün'});
    if (flags & 256) list.push({icon: '❄️', text: 'Klima'});
    if (flags & 512) list.push({icon: '🕌', text: 'Mescit'});
    if (flags & 1024) list.push({icon: '💡', text: 'Gece Aydınlatması'});
    return list;
  }

  loadCourtSlots() {
    this.http.get(`${environment.apiUrl}/Courts/slots/${this.courtId}`).subscribe({
      next: (data: any) => {
        this.slots = data?.$values || data || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Seanslar yüklenemedi:', err)
    });
  }

  bookSlot(slotId: number) {
    this.bookingError = '';
    this.bookingSuccess = '';

    if (!this.authService.isLoggedIn()) {
      // Login değilse login sayfasına yönlendir ve dönüş URL'sini ver
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/court-detail/${this.courtId}` } });
      return;
    }

    const payload = { slotId: slotId };
    
    this.http.post(`${environment.apiUrl}/Bookings/create`, payload).subscribe({
      next: (res: any) => {
        this.bookingSuccess = res.message || 'Kiralama başarılı!';
        this.loadCourtSlots(); // Takvimi yenile
      },
      error: (err) => {
        this.bookingError = err.error?.message || err.error || 'Kiralama sırasında bir hata oluştu.';
      }
    });
  }
}
