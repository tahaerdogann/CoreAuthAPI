import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { CourtFormComponent } from '../shared/court-form.component';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, CourtFormComponent],
  templateUrl: './owner-dashboard.html',
  styleUrls: ['./owner-dashboard.css']
})
export class OwnerDashboardComponent implements OnInit {
  myCourts: any[] = [];
  selectedCourtId: number | null = null;
  selectedCourtData: any = null;
  courtSlots: any[] = [];
  scheduleForm: FormGroup;
  mesaj: string = '';
  hata: string = '';
  hataListesi: string[] = [];
  isGenerating: boolean = false;
  isAutoScheduleEnabled: boolean = false;
  hasEmptySlots: boolean = false; // Yalnızca boş seans varsa buton pasif olmalı

  // Edit Modal State
  isEditModalOpen = false;
  isUpdatingCourt = false;

  // Custom Modal States
  alertModal = { isOpen: false, title: '', message: '' };
  confirmModal = { isOpen: false, title: '', message: '', onConfirm: () => {}, isDanger: true };

  daysConfig = [
    { dayOfWeek: 1, name: 'Pazartesi', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 2, name: 'Salı', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 3, name: 'Çarşamba', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 4, name: 'Perşembe', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 5, name: 'Cuma', isActive: true, openTime: '10:00', closeTime: '23:59' },
    { dayOfWeek: 6, name: 'Cumartesi', isActive: true, openTime: '09:00', closeTime: '23:59' },
    { dayOfWeek: 0, name: 'Pazar', isActive: true, openTime: '09:00', closeTime: '23:59' }
  ];

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router, private cdr: ChangeDetectorRef) {
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

  showAlert(title: string, message: string) {
    this.alertModal = { isOpen: true, title, message };
    this.cdr.detectChanges();
  }

  closeAlert() {
    this.alertModal.isOpen = false;
    this.cdr.detectChanges();
  }

  showConfirm(title: string, message: string, isDanger: boolean, onConfirm: () => void) {
    this.confirmModal = { isOpen: true, title, message, isDanger, onConfirm };
    this.cdr.detectChanges();
  }

  closeConfirm() {
    this.confirmModal.isOpen = false;
    this.cdr.detectChanges();
  }

  onConfirmAction() {
    if (this.confirmModal.onConfirm) {
      this.confirmModal.onConfirm();
    }
    this.closeConfirm();
  }

  loadMyCourts() {
    const token = localStorage.getItem('token');
    if (!token) return;

    const headers = { 'Authorization': `Bearer ${token}` };
    this.http.get(`${environment.apiUrl}/Courts/my-courts`, { headers }).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.myCourts = res.$values;
        else if (Array.isArray(res)) this.myCourts = res;
        else this.myCourts = [];
        
        // Update selected court data if it exists
        if (this.selectedCourtId) {
          this.selectedCourtData = this.myCourts.find(c => (c.id || c.Id) === this.selectedCourtId);
        }
        
        this.cdr.detectChanges();
      },
      error: (err) => console.error("API Hatası:", err)
    });
  }

  onCourtSelected(event: any) {
    const value = event.target.value;
    this.selectedCourtId = value ? Number(value) : null;
    this.selectedCourtData = this.myCourts.find(c => (c.id || c.Id) === this.selectedCourtId);

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

  loadCourtSlots(courtId: number) {
    this.http.get(`${environment.apiUrl}/Courts/slots/${courtId}`).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.courtSlots = res.$values;
        else if (Array.isArray(res)) this.courtSlots = res;
        else this.courtSlots = [];
        
        // Eğer en az 1 BOŞ slot varsa form inaktif olsun
        this.hasEmptySlots = this.courtSlots.some(s => !(s.isBooked || s.IsBooked));
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Slotlar çekilemedi:", err)
    });
  }

  generateSchedule() {
    if (!this.selectedCourtId || this.hasEmptySlots) return;
    
    if (this.scheduleForm.invalid) {
      this.hata = 'Lütfen formdaki tüm alanları geçerli şekilde doldurun!';
      return;
    }

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
        if (res.errors && res.errors.length > 0) {
          this.hataListesi = res.errors;
        }
        this.isGenerating = false;
        this.loadCourtSlots(this.selectedCourtId!);
      },
      error: (err) => {
        this.hata = err.error?.message || 'Takvim üretilirken bir hata oluştu.';
        if (err.error?.errors) {
          this.hataListesi = err.error.errors;
        }
        this.isGenerating = false;
      }
    });
  }

  cancelSchedule() {
    if (!this.selectedCourtId) return;
    
    this.showConfirm(
      "Takvimlendirmeyi İptal Et", 
      "Takvimlendirmeyi iptal etmek istediğinize emin misiniz? Bu işlem, henüz rezerve edilmemiş tüm boş seansları silecektir.",
      true,
      () => {
        const token = localStorage.getItem('token');
        const headers = { 'Authorization': `Bearer ${token}` };

        this.http.post(`${environment.apiUrl}/Courts/${this.selectedCourtId}/cancel-schedule`, {}, { headers }).subscribe({
          next: (res: any) => {
            this.showAlert("Başarılı", res.message);
            this.loadCourtSlots(this.selectedCourtId!);
          },
          error: (err) => {
            this.showAlert("Hata", err.error?.message || err.error || "İptal işlemi sırasında hata oluştu.");
          }
        });
      }
    );
  }

  deleteCourtPrompt() {
    if (!this.selectedCourtId) return;

    this.showConfirm(
      "Sahayı Tamamen Sil",
      "Bu sahayı tamamen SİLMEK istediğinize emin misiniz?",
      true,
      () => {
        const token = localStorage.getItem('token');
        const headers = { 'Authorization': `Bearer ${token}` };

        this.http.delete(`${environment.apiUrl}/Courts/${this.selectedCourtId}`, { headers }).subscribe({
          next: (res: any) => {
            this.showAlert("Başarılı", res.message);
            this.selectedCourtId = null;
            this.selectedCourtData = null;
            this.courtSlots = [];
            this.loadMyCourts();
          },
          error: (err) => {
            this.showAlert("Hata", err.error?.message || err.error || "Silme işlemi sırasında hata oluştu.");
          }
        });
      }
    );
  }

  openEditModal() {
    if (!this.selectedCourtId || !this.selectedCourtData) return;
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
  }

  onEditCourtSubmit(payload: any) {
    if (!this.selectedCourtId) return;
    this.isUpdatingCourt = true;
    
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.put(`${environment.apiUrl}/Courts/${this.selectedCourtId}`, payload, { headers }).subscribe({
      next: (res: any) => {
        this.isUpdatingCourt = false;
        this.closeEditModal();
        this.showAlert("Başarılı", res.message || "Saha başarıyla güncellendi!");
        this.loadMyCourts(); // Yeniden yükle ki dropdown verileri vs güncellensin
      },
      error: (err) => {
        this.isUpdatingCourt = false;
        this.showAlert("Hata", err.error?.message || "Saha güncellenirken hata oluştu.");
      }
    });
  }

  toggleAutoSchedule() {
    if (!this.selectedCourtId) return;

    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`${environment.apiUrl}/Courts/${this.selectedCourtId}/toggle-auto-schedule`, {}, { headers }).subscribe({
      next: (res: any) => {
        this.showAlert("Bilgi", res.message);
        this.isAutoScheduleEnabled = res.isAutoScheduleEnabled;
      },
      error: (err) => {
        this.showAlert("Hata", err.error?.message || err.error || "Döngü güncellenirken hata oluştu.");
      }
    });
  }

  toggleSlot(slotId: number) {
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`${environment.apiUrl}/Courts/toggle-slot/${slotId}`, {}, { headers }).subscribe({
      next: (res: any) => {
        const slot = this.courtSlots.find(s => s.id === slotId || s.Id === slotId);
        if (slot) {
          if (slot.isBooked !== undefined) slot.isBooked = res.isBooked;
          if (slot.IsBooked !== undefined) slot.IsBooked = res.isBooked;
        }
        
        // Yeniden API'ye istek atmadan lokal olarak hesaplayıp anında güncelliyoruz
        this.hasEmptySlots = this.courtSlots.some(s => !(s.isBooked || s.IsBooked));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Slot güncellenemedi:", err);
        this.showAlert("Hata", err.error || "İşlem sırasında bir hata oluştu.");
      }
    });
  }

  yeniSahaSayfasinaGit() {
    this.router.navigate(['/saha-ekle']);
  }
}
