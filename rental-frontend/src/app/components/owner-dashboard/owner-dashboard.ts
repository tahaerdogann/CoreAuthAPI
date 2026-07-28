import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './owner-dashboard.html',
  styleUrls: ['./owner-dashboard.css']
})
export class OwnerDashboardComponent implements OnInit {
  myCourts: any[] = [];
  selectedCourtId: number | null = null;
  courtSlots: any[] = [];
  scheduleForm: FormGroup;
  mesaj: string = '';
  hata: string = '';
  hataListesi: string[] = [];
  isGenerating: boolean = false;
  isAutoScheduleEnabled: boolean = false;
  hasActiveSchedule: boolean = false; // Takvimlendirme var mı?

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
      basePrice: ['', [Validators.required, Validators.min(1)]],
      primeTimePrice: ['', [Validators.required, Validators.min(1)]],
      primeTimeStart: ['18:00', Validators.required],
      primeTimeEnd: ['23:59', Validators.required]
    });
  }

  ngOnInit() {
    this.loadMyCourts();
  }

  loadMyCourts() {
    const token = localStorage.getItem('token');
    if (!token) return;

    const headers = { 'Authorization': `Bearer ${token}` };
    this.http.get('https://localhost:7284/api/Courts/my-courts', { headers }).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.myCourts = res.$values;
        else if (Array.isArray(res)) this.myCourts = res;
        else this.myCourts = [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error("API Hatası:", err)
    });
  }

  onCourtSelected(event: any) {
    const value = event.target.value;
    this.selectedCourtId = value ? Number(value) : null;

    if (this.selectedCourtId) {
      this.loadCourtSlots(this.selectedCourtId);
      this.mesaj = '';
      this.hata = '';
      this.hataListesi = [];
    } else {
      this.courtSlots = [];
      this.hasActiveSchedule = false;
    }
  }

  loadCourtSlots(courtId: number) {
    this.http.get(`https://localhost:7284/api/Courts/slots/${courtId}`).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.courtSlots = res.$values;
        else if (Array.isArray(res)) this.courtSlots = res;
        else this.courtSlots = [];
        
        // Eğer en az 1 slot varsa form inaktif olsun
        this.hasActiveSchedule = this.courtSlots.length > 0;
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Slotlar çekilemedi:", err)
    });
  }

  generateSchedule() {
    if (!this.selectedCourtId || this.hasActiveSchedule) return;
    
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

    this.http.post('https://localhost:7284/api/Courts/generate-schedule', payload, { headers }).subscribe({
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
    
    if (confirm("Takvimlendirmeyi iptal etmek istediğinize emin misiniz? Bu işlem, henüz rezerve edilmemiş tüm boş seansları silecektir.")) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      this.http.post(`https://localhost:7284/api/Courts/${this.selectedCourtId}/cancel-schedule`, {}, { headers }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadCourtSlots(this.selectedCourtId!);
        },
        error: (err) => {
          alert(err.error?.message || err.error || "İptal işlemi sırasında hata oluştu.");
        }
      });
    }
  }

  deleteCourt() {
    if (!this.selectedCourtId) return;

    if (confirm("Bu sahayı tamamen SİLMEK istediğinize emin misiniz? Eğer aktif rezervasyon varsa silinmeyecektir.")) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      this.http.delete(`https://localhost:7284/api/Courts/${this.selectedCourtId}`, { headers }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.selectedCourtId = null;
          this.courtSlots = [];
          this.loadMyCourts(); // Listeyi yenile
        },
        error: (err) => {
          alert(err.error?.message || err.error || "Silme işlemi sırasında hata oluştu.");
        }
      });
    }
  }

  toggleAutoSchedule() {
    if (!this.selectedCourtId) return;

    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`https://localhost:7284/api/Courts/${this.selectedCourtId}/toggle-auto-schedule`, {}, { headers }).subscribe({
      next: (res: any) => {
        alert(res.message);
        this.isAutoScheduleEnabled = res.isAutoScheduleEnabled;
      },
      error: (err) => {
        alert(err.error?.message || err.error || "Döngü güncellenirken hata oluştu.");
      }
    });
  }

  toggleSlot(slotId: number) {
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`https://localhost:7284/api/Courts/toggle-slot/${slotId}`, {}, { headers }).subscribe({
      next: (res: any) => {
        const slot = this.courtSlots.find(s => s.id === slotId || s.Id === slotId);
        if (slot) {
          if (slot.isBooked !== undefined) slot.isBooked = res.isBooked;
          if (slot.IsBooked !== undefined) slot.IsBooked = res.isBooked;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Slot güncellenemedi:", err);
        alert(err.error || "İşlem sırasında bir hata oluştu.");
      }
    });
  }

  yeniSahaSayfasinaGit() {
    this.router.navigate(['/saha-ekle']);
  }
}
