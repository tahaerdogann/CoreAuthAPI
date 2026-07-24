import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OwnerService } from '../../services/owner';

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

  // Sekme yönetimi (1: Takvim/Ayarlar, 2: Rezervasyonlar)
  activeTab: number = 1;

  // Yeni İlan Formu
  slotForm!: FormGroup;
  mesaj: string = '';
  hata: string = '';

  constructor(private ownerService: OwnerService, private fb: FormBuilder) { }

  ngOnInit() {
    this.initForm();
    this.loadMyCourts();
  }

  initForm() {
    this.slotForm = this.fb.group({
      startTime: ['', Validators.required],
      endTime: ['', Validators.required],
      price: ['', [Validators.required, Validators.min(1)]]
    });
  }

  // 1. Sahibin sahalarını yükle (Dropdown için)
  loadMyCourts() {
    this.ownerService.getMyCourts().subscribe({
      next: (courts) => {
        this.myCourts = courts;
      },
      error: (err) => console.error("Sahalar yüklenemedi", err)
    });
  }

  // 2. Saha seçildiğinde slotları getir ve sol paneli aç
  onCourtSelected(event: any) {
    this.selectedCourtId = event.target.value;
    if (this.selectedCourtId) {
      this.loadSlots(this.selectedCourtId);
    } else {
      this.courtSlots = [];
    }
  }

  loadSlots(courtId: number) {
    this.ownerService.getCourtSlots(courtId).subscribe({
      next: (slots) => {
        this.courtSlots = slots;
      },
      error: (err) => console.error("Slotlar yüklenemedi", err)
    });
  }

  // 3. Sekme Değiştirme
  switchTab(tabId: number) {
    this.activeTab = tabId;
  }

  // 4. Yeni Slot Kaydetme
  saveSlot() {
    if (this.slotForm.invalid || !this.selectedCourtId) {
      this.hata = "Lütfen saha seçin ve tüm alanları (Tarih, Fiyat) doldurun.";
      return;
    }

    this.mesaj = 'Kaydediliyor...';
    this.hata = '';

    this.ownerService.createSlot(this.selectedCourtId, this.slotForm.value).subscribe({
      next: (res) => {
        this.mesaj = 'Başarıyla oluşturuldu!';
        this.slotForm.reset();
        // Listeyi güncelle
        this.loadSlots(this.selectedCourtId!);
      },
      error: (err) => {
        this.mesaj = '';
        this.hata = 'Oluşturulurken hata oluştu.';
        console.error(err);
      }
    });
  }
}
