import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bookings.html',
  styleUrls: ['./bookings.css']
})
export class BookingsComponent implements OnInit {
  myCourts: any[] = [];
  selectedCourtId: string = '';
  slots: any[] = [];
  isLoading = false;

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadMyCourts();
    this.loadBookedSlots();
  }

  loadMyCourts() {
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    this.http.get(`${environment.apiUrl}/Courts/my-courts`, { headers }).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.myCourts = res.$values;
        else if (Array.isArray(res)) this.myCourts = res;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  loadBookedSlots() {
    this.isLoading = true;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    let url = `${environment.apiUrl}/Bookings/owner-booked-slots`;
    if (this.selectedCourtId) {
      url += `?courtId=${this.selectedCourtId}`;
    }

    this.http.get(url, { headers }).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.slots = res.$values;
        else if (Array.isArray(res)) this.slots = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onFilterChange() {
    this.loadBookedSlots();
  }

  get selectedCourt() {
    return this.myCourts.find((c: any) => (c.id || c.Id) === this.selectedCourtId);
  }

  toggleAutoApprove() {
    if (!this.selectedCourtId) return;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    this.http.post(`${environment.apiUrl}/Courts/${this.selectedCourtId}/toggle-auto-approve`, {}, { headers }).subscribe({
      next: (res: any) => {
        if(this.selectedCourt) {
            this.selectedCourt.isAutoApproveEnabled = res.isAutoApproveEnabled;
        }
        alert(res.message);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        alert(err.error?.message || 'Hata oluştu');
      }
    });
  }

  updateStatus(bookingId: string, newStatus: number) {
    if (!bookingId) return;
    
    if (confirm('Emin misiniz?')) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };
      this.http.post(`${environment.apiUrl}/Bookings/update-status/${bookingId}`, { status: newStatus }, { headers }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadBookedSlots();
        },
        error: (err) => alert(err.error?.message || 'Hata oluştu')
      });
    }
  }

  openSlot(slotId: string) {
    // Manuel kapatılmış slotu tekrar açmak (toggle-slot)
    if (confirm('Bu seansı tekrar kiralamaya açmak istediğinize emin misiniz?')) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };
      this.http.post(`${environment.apiUrl}/Courts/toggle-slot/${slotId}`, {}, { headers }).subscribe({
        next: (res: any) => {
          alert('Seans açıldı.');
          this.loadBookedSlots();
        },
        error: (err) => alert(err.error || 'Hata oluştu')
      });
    }
  }

  getStatusBadge(status: number) {
    switch(status) {
      case 0: return { class: 'badge-warning', text: 'Bekliyor' };
      case 1: return { class: 'badge-success', text: 'Onaylandı' };
      case 2: return { class: 'badge-danger', text: 'İptal Edildi' };
      default: return { class: 'badge-secondary', text: 'Bilinmiyor' };
    }
  }
}
