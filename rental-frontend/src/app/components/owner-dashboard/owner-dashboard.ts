import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
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
  isGenerating: boolean = false;

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router, private cdr: ChangeDetectorRef) {
    // Profesyonel Kurallar Formu
    this.scheduleForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      sessionDurationMinutes: [60, [Validators.required, Validators.min(15), Validators.max(240)]],
      bufferDurationMinutes: [0, [Validators.required, Validators.min(0), Validators.max(120)]],
      openTime: ['10:00', Validators.required],
      closeTime: ['23:59', Validators.required],
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
        if (res && res.$values) {
          this.myCourts = res.$values;
        } else if (Array.isArray(res)) {
          this.myCourts = res;
        } else {
          this.myCourts = [];
        }
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
    } else {
      this.courtSlots = [];
    }
  }

  loadCourtSlots(courtId: number) {
    this.http.get(`https://localhost:7284/api/Courts/slots/${courtId}`).subscribe({
      next: (res: any) => {
        if (res && res.$values) {
          this.courtSlots = res.$values;
        } else if (Array.isArray(res)) {
          this.courtSlots = res;
        } else {
          this.courtSlots = [];
        }
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Slotlar çekilemedi:", err)
    });
  }

  generateSchedule() {
    if (!this.selectedCourtId) {
      this.hata = 'Lütfen işlem yapmak için bir saha seçin!';
      return;
    }
    if (this.scheduleForm.invalid) {
      this.hata = 'Lütfen formdaki tüm alanları geçerli şekilde doldurun!';
      return;
    }

    this.isGenerating = true;
    this.hata = '';
    this.mesaj = '';

    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    const formData = this.scheduleForm.value;

    // C# TimeSpan formatı için saatlerin sonuna ":00" ekliyoruz (örn: "10:00" -> "10:00:00")
    const formatTime = (time: string) => time.length === 5 ? `${time}:00` : time;

    const payload = {
      courtId: this.selectedCourtId,
      startDate: formData.startDate,
      endDate: formData.endDate,
      sessionDurationMinutes: formData.sessionDurationMinutes,
      bufferDurationMinutes: formData.bufferDurationMinutes,
      openTime: formatTime(formData.openTime),
      closeTime: formatTime(formData.closeTime),
      basePrice: formData.basePrice,
      primeTimePrice: formData.primeTimePrice,
      primeTimeStart: formatTime(formData.primeTimeStart),
      primeTimeEnd: formatTime(formData.primeTimeEnd)
    };

    this.http.post('https://localhost:7284/api/Courts/generate-schedule', payload, { headers }).subscribe({
      next: (res: any) => {
        this.mesaj = res.message || 'Takvim başarıyla üretildi!';
        this.isGenerating = false;
        this.scheduleForm.reset({
          sessionDurationMinutes: 60, bufferDurationMinutes: 0,
          openTime: '10:00', closeTime: '23:59', primeTimeStart: '18:00', primeTimeEnd: '23:59'
        });
        // Sağ taraftaki tabloyu güncelliyoruz
        this.loadCourtSlots(this.selectedCourtId!);
      },
      error: (err) => {
        this.hata = 'Takvim üretilirken bir hata oluştu veya yetkisiz işlem.';
        console.error("Üretim Hatası:", err);
        this.isGenerating = false;
      }
    });
  }

  yeniSahaSayfasinaGit() {
    this.router.navigate(['/saha-ekle']);
  }
}
