import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FlatpickrModule } from 'angularx-flatpickr';
import { AlertModalComponent } from '../../shared/alert-modal.component';

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, FlatpickrModule, AlertModalComponent],
  templateUrl: './schedule.html',
  styleUrls: ['./schedule.css']
})
export class ScheduleComponent implements OnInit {
  myCourts: any[] = [];
  selectedCourtId: string | null = null;
  courtSlots: any[] = [];
  scheduleForm: FormGroup;
  mesaj: string = '';
  hata: string = '';
  hataListesi: string[] = [];
  isGenerating: boolean = false;
  isAutoScheduleEnabled: boolean = false;
  hasEmptySlots: boolean = false;

  // Modal Props
  alertModalState = {
    isOpen: false,
    title: '',
    message: '',
    type: 'info' as 'success'|'error'|'warning'|'info',
    isConfirm: false
  };
  pendingAction: (() => void) | null = null;

  // Action Modal Props (Dışarıdan Ekle & Bakıma Al)
  isSlotActionModalOpen = false;
  activeTab: 'external' | 'maintenance' = 'external';
  selectedSlotForAction: any = null;
  externalCustomerName: string = '';
  externalCustomerPhone: string = '';
  maintenanceNote: string = '';

  daysConfig = [
    { dayOfWeek: 1, name: 'Pazartesi', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 2, name: 'Salı', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 3, name: 'Çarşamba', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 4, name: 'Perşembe', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 5, name: 'Cuma', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 6, name: 'Cumartesi', isActive: true, openTime: '09:00', closeTime: '23:59' },
    { dayOfWeek: 0, name: 'Pazar', isActive: true, openTime: '09:00', closeTime: '23:59' }
  ];

  constructor(private fb: FormBuilder, private http: HttpClient, private cdr: ChangeDetectorRef) {
    this.scheduleForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      sessionDurationMinutes: [60, [Validators.required, Validators.min(15), Validators.max(240)]],
      bufferDurationMinutes: [0, [Validators.required, Validators.min(0), Validators.max(120)]],
      basePrice: [null, [Validators.required, Validators.min(1)]],
      primeTimePrice: [null, [Validators.required, Validators.min(1)]],
      primeTimeStart: ['18:00', Validators.required],
      primeTimeEnd: ['23:59', Validators.required]
    });
  }

  ngOnInit() {
    this.loadMyCourts();
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

  onCourtSelected(event: any) {
    const value = event.target.value;
    this.selectedCourtId = value ? String(value) : null;

    if (this.selectedCourtId) {
      this.loadCourtSlots(this.selectedCourtId);
      this.mesaj = '';
      this.hata = '';
      this.hataListesi = [];
    } else {
      this.courtSlots = [];
      this.hasEmptySlots = false;
    }
  }

  loadCourtSlots(courtId: string) {
    this.http.get(`${environment.apiUrl}/Courts/slots/${courtId}`).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.courtSlots = res.$values;
        else if (Array.isArray(res)) this.courtSlots = res;
        else this.courtSlots = [];
        
        // Sadece 1 (Available) olan slotlar "empty" olarak sayılır.
        this.hasEmptySlots = this.courtSlots.some(s => (s.status || s.Status) === 1);
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Slotlar çekilemedi:", err)
    });
  }

  getSlotStatus(slot: any) {
    const status = slot.status || slot.Status || 1; // Default 1
    if (status === 1) return { text: 'Müsait', class: 'badge-success' };
    if (status === 2) return { text: 'Dolu', class: 'badge-danger' };
    if (status === 3) return { text: 'Bakımda', class: 'badge-warning' };
    return { text: 'Müsait', class: 'badge-success' };
  }

  openSlotActionModal(slot: any) {
    this.selectedSlotForAction = slot;
    const status = slot.status || slot.Status || 1;
    
    if (status === 1) { 
      // Available
      this.isSlotActionModalOpen = true;
      this.activeTab = 'external';
      this.externalCustomerName = '';
      this.externalCustomerPhone = '';
      this.maintenanceNote = '';
    } else if (status === 2) {
      // Booked
      this.alertModalState = {
        isOpen: true,
        title: 'Bilgi',
        message: 'Bu seans dolu (Kiralandı). İptal etmek veya rezervasyon detaylarını görmek için sol menüdeki "Rezervasyonlar" sayfasına gidiniz.',
        type: 'info',
        isConfirm: false
      };
    } else if (status === 3) {
      // Maintenance
      this.alertModalState = {
        isOpen: true,
        title: 'Bakımı Bitir',
        message: 'Bu slotu bakımdan çıkarıp tekrar kiralamaya açmak istiyor musunuz?',
        type: 'warning',
        isConfirm: true
      };
      this.pendingAction = () => {
        this.submitMaintenance(false);
      };
    }
  }

  closeSlotActionModal() {
    this.isSlotActionModalOpen = false;
  }

  submitExternalBooking() {
    if(!this.externalCustomerName) {
      this.alertModalState = { isOpen: true, title: 'Hata', message: 'Müşteri adı zorunludur.', type: 'error', isConfirm: false };
      return;
    }
    const slotId = this.selectedSlotForAction.id || this.selectedSlotForAction.Id;
    const payload = {
      slotId: slotId,
      externalCustomerName: this.externalCustomerName,
      externalCustomerPhone: this.externalCustomerPhone
    };
    
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    this.http.post(`${environment.apiUrl}/Bookings/external-booking`, payload, { headers }).subscribe({
      next: (res: any) => {
        this.isSlotActionModalOpen = false;
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: res.message, type: 'success', isConfirm: false };
        this.loadCourtSlots(this.selectedCourtId!);
      },
      error: (err) => {
        this.alertModalState = { isOpen: true, title: 'Hata', message: err.error?.message || 'Hata oluştu.', type: 'error', isConfirm: false };
      }
    });
  }

  submitMaintenance(isMaintenance: boolean = true) {
    const slotId = this.selectedSlotForAction.id || this.selectedSlotForAction.Id;
    const payload = {
      isMaintenance: isMaintenance,
      note: this.maintenanceNote
    };
    
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    this.http.put(`${environment.apiUrl}/Bookings/courtslots/${slotId}/maintenance`, payload, { headers }).subscribe({
      next: (res: any) => {
        this.isSlotActionModalOpen = false;
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: res.message, type: 'success', isConfirm: false };
        this.loadCourtSlots(this.selectedCourtId!);
      },
      error: (err) => {
        this.alertModalState = { isOpen: true, title: 'Hata', message: err.error?.message || 'Hata oluştu.', type: 'error', isConfirm: false };
      }
    });
  }

  generateSchedule() {
    if (!this.selectedCourtId || this.hasEmptySlots || this.scheduleForm.invalid) return;

    this.isGenerating = true;
    this.hata = '';
    this.mesaj = '';
    this.hataListesi = [];

    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    const formData = this.scheduleForm.value;
    const formatTime = (time: string) => time.length === 5 ? `${time}:00` : time;

    const payload = {
      courtId: this.selectedCourtId,
      startDate: formData.startDate,
      endDate: formData.endDate,
      sessionDurationMinutes: formData.sessionDurationMinutes,
      bufferDurationMinutes: formData.bufferDurationMinutes,
      openTime: "10:00:00",
      closeTime: "23:59:00",
      basePrice: formData.basePrice,
      primeTimePrice: formData.primeTimePrice,
      primeTimeStart: formatTime(formData.primeTimeStart),
      primeTimeEnd: formatTime(formData.primeTimeEnd),
      isAutoScheduleEnabled: this.isAutoScheduleEnabled,
      daysConfig: this.daysConfig.map(d => ({
        dayOfWeek: d.dayOfWeek,
        isActive: d.isActive,
        openTime: formatTime(d.openTime),
        closeTime: formatTime(d.closeTime)
      }))
    };

    this.http.post(`${environment.apiUrl}/Courts/generate-schedule`, payload, { headers }).subscribe({
      next: (res: any) => {
        this.mesaj = res.message || 'Takvim başarıyla üretildi!';
        if (res.errors && res.errors.length > 0) this.hataListesi = res.errors;
        this.isGenerating = false;
        this.loadCourtSlots(this.selectedCourtId!);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.hata = err.error?.message || 'Takvim üretilirken hata oluştu.';
        if (err.error?.errors) this.hataListesi = err.error.errors;
        this.isGenerating = false;
        this.cdr.detectChanges();
      }
    });
  }

  cancelSchedule() {
    if (!this.selectedCourtId) return;
    
    this.alertModalState = {
      isOpen: true,
      title: 'Emin misiniz?',
      message: 'Takvimlendirmeyi iptal etmek istediğinize emin misiniz? Sadece henüz KİRALANMAMIŞ (müsait) olan boş seanslar silinecektir.',
      type: 'warning',
      isConfirm: true
    };

    this.pendingAction = () => {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      this.http.post(`${environment.apiUrl}/Courts/${this.selectedCourtId}/cancel-schedule`, {}, { headers }).subscribe({
        next: (res: any) => {
          this.alertModalState = { isOpen: true, title: 'Başarılı', message: res.message, type: 'success', isConfirm: false };
          this.loadCourtSlots(this.selectedCourtId!);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.alertModalState = { isOpen: true, title: 'Hata', message: err.error?.message || err.error || "İptal işlemi sırasında hata oluştu.", type: 'error', isConfirm: false };
          this.cdr.detectChanges();
        }
      });
    };
  }

  onAlertConfirm() {
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }
}
