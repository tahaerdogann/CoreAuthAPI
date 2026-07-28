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

  loadCourtSlots() {
    this.http.get(`${environment.apiUrl}/Courts/slots/${this.courtId}`).subscribe({
      next: (data: any) => {
        this.slots = data;
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
