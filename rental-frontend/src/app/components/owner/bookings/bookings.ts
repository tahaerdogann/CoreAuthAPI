import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FormsModule } from '@angular/forms';
import { AlertModalComponent } from '../../shared/alert-modal.component';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule, AlertModalComponent],
  templateUrl: './bookings.html',
  styleUrls: ['./bookings.css']
})
export class BookingsComponent implements OnInit {
  myCourts: any[] = [];
  selectedCourtId: string = '';
  slots: any[] = [];
  isLoading = false;

  // Modal Props
  alertModalState = {
    isOpen: false,
    title: '',
    message: '',
    type: 'info' as 'success'|'error'|'warning'|'info',
    isConfirm: false
  };
  pendingAction: (() => void) | null = null;

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

  get filteredSlots() {
    // Yalnızca Bekliyor (0) ve Onaylandı (1) statüsündekileri göster
    return this.slots.filter(s => s.status === 0 || s.status === 1);
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
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: res.message, type: 'success', isConfirm: false };
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.alertModalState = { isOpen: true, title: 'Hata', message: err.error?.message || 'Hata oluştu', type: 'error', isConfirm: false };
        this.cdr.detectChanges();
      }
    });
  }

  updateStatus(bookingId: string, newStatus: number) {
    if (!bookingId) return;
    
    let actionName = newStatus === 1 ? 'onaylamak' : 'iptal etmek';
    
    this.alertModalState = {
      isOpen: true,
      title: 'Emin misiniz?',
      message: `Bu rezervasyonu ${actionName} istediğinize emin misiniz?`,
      type: newStatus === 1 ? 'info' : 'warning',
      isConfirm: true
    };

    this.pendingAction = () => {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };
      this.http.post(`${environment.apiUrl}/Bookings/update-status/${bookingId}`, { status: newStatus }, { headers }).subscribe({
        next: (res: any) => {
          this.alertModalState = { isOpen: true, title: 'Başarılı', message: res.message, type: 'success', isConfirm: false };
          this.loadBookedSlots();
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.alertModalState = { isOpen: true, title: 'Hata', message: err.error?.message || 'Hata oluştu', type: 'error', isConfirm: false };
          this.cdr.detectChanges();
        }
      });
    };
  }

  getStatusBadge(status: number) {
    switch(status) {
      case 0: return { class: 'badge-warning', text: 'Bekliyor' };
      case 1: return { class: 'badge-success', text: 'Onaylandı' };
      case 2: return { class: 'badge-danger', text: 'İptal Edildi' };
      case 3: return { class: 'badge-primary', text: 'Tamamlandı' };
      default: return { class: 'badge-secondary', text: 'Bilinmiyor' };
    }
  }

  onAlertConfirm() {
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }
}
