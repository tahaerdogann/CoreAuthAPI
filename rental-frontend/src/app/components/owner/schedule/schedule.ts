import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FlatpickrModule } from 'angularx-flatpickr';

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, FlatpickrModule],
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
        
        this.hasEmptySlots = this.courtSlots.some(s => !(s.isBooked || s.IsBooked));
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Slotlar çekilemedi:", err)
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
    
    if (confirm("Takvimlendirmeyi iptal etmek istediğinize emin misiniz? Boş seanslar silinecektir.")) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      this.http.post(`${environment.apiUrl}/Courts/${this.selectedCourtId}/cancel-schedule`, {}, { headers }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadCourtSlots(this.selectedCourtId!);
          this.cdr.detectChanges();
        },
        error: (err) => {
          alert(err.error?.message || err.error || "İptal işlemi sırasında hata oluştu.");
          this.cdr.detectChanges();
        }
      });
    }
  }
}
