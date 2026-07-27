import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-saha-ekle',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './saha-ekle.html',
  styleUrls: ['./saha-ekle.css'] // İçi boş kalabilir
})
export class SahaEkleComponent {
  sahaForm: FormGroup;
  mesaj: string = '';
  hata: string = '';

  sehirler = ['İstanbul', 'Ankara', 'İzmir', 'Bursa', 'Antalya'];
  ilceler: { [key: string]: string[] } = {
    'İstanbul': ['Kadıköy', 'Beşiktaş', 'Şişli', 'Üsküdar', 'Maltepe'],
    'Ankara': ['Çankaya', 'Keçiören', 'Yenimahalle'],
    'İzmir': ['Karşıyaka', 'Bornova', 'Buca']
  };
  mahalleler: { [key: string]: string[] } = {
    'Kadıköy': ['Caferağa', 'Moda', 'Fenerbahçe', 'Bostancı'],
    'Beşiktaş': ['Bebek', 'Levent', 'Etiler'],
    'Çankaya': ['Kızılay', 'Bahçelievler', 'Çayyolu']
  };

  aktifIlceler: string[] = [];
  aktifMahalleler: string[] = [];

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router) {
    this.sahaForm = this.fb.group({
      name: ['', Validators.required],
      sportType: ['Futbol', Validators.required],
      surfaceType: ['Suni Çim', Validators.required],
      hourlyPrice: [null, [Validators.required, Validators.min(1)]],
      city: ['', Validators.required],
      district: ['', Validators.required],
      neighborhood: ['', Validators.required],
      addressDetail: [''],
      amenities: this.fb.group({
        restroom: [false], cafeteria: [false], disabledAccess: [false],
        changingRoom: [false], wifi: [false], shower: [false],
        locker: [false], grandstand: [false], airConditioning: [false],
        prayerRoom: [false], lighting: [false]
      }),
      rentalOptions: this.fb.group({
        krampon: this.fb.group({ isActive: [false], availableCount: [0], unitPrice: [0] }),
        ayakkabi: this.fb.group({ isActive: [false], availableCount: [0], unitPrice: [0] }),
        yelek: this.fb.group({ isActive: [false], availableCount: [0], unitPrice: [0] }),
        kaleci: this.fb.group({ isActive: [false], availableCount: [0], unitPrice: [0] }),
        hakem: this.fb.group({ isActive: [false], availableCount: [1], unitPrice: [0] })
      })
    });
  }

  onCityChange(event: any) {
    const selectedCity = event.target.value;
    this.aktifIlceler = this.ilceler[selectedCity] || [];
    this.sahaForm.patchValue({ district: '', neighborhood: '' });
    this.aktifMahalleler = [];
  }

  onDistrictChange(event: any) {
    const selectedDistrict = event.target.value;
    this.aktifMahalleler = this.mahalleler[selectedDistrict] || [];
    this.sahaForm.patchValue({ neighborhood: '' });
  }

  get isFootball(): boolean { return this.sahaForm.get('sportType')?.value === 'Futbol'; }

  calculateBitwiseAmenities(ag: any): number {
    let total = 0;
    if (ag.restroom) total += 1;
    if (ag.cafeteria) total += 2;
    if (ag.disabledAccess) total += 4;
    if (ag.changingRoom) total += 8;
    if (ag.wifi) total += 16;
    if (ag.shower) total += 32;
    if (ag.locker) total += 64;
    if (ag.grandstand) total += 128;
    if (ag.airConditioning) total += 256;
    if (ag.prayerRoom) total += 512;
    if (ag.lighting) total += 1024;
    return total;
  }

  kaydet() {
    if (this.sahaForm.invalid) {
      this.hata = 'Lütfen zorunlu alanları doldurun.'; return;
    }

    this.hata = '';
    this.mesaj = 'Saha kaydediliyor...';
    const formData = this.sahaForm.value;

    const payload = {
      name: formData.name, sportType: formData.sportType, surfaceType: formData.surfaceType,
      city: formData.city, district: formData.district, neighborhood: formData.neighborhood,
      addressDetail: formData.addressDetail, hourlyPrice: formData.hourlyPrice,
      amenities: this.calculateBitwiseAmenities(formData.amenities),
      rentalOptionsJson: JSON.stringify(formData.rentalOptions)
    };

    // DİKKAT: 7284 portunu kendi backend portunla değiştirmeyi unutma!
    this.http.post('https://localhost:7284/api/Courts/add', payload).subscribe({
      next: (res: any) => {
        this.mesaj = res.message || 'Saha başarıyla eklendi!';
        setTimeout(() => { this.router.navigate(['/owner-dashboard']); }, 1500);
      },
      error: (err) => { this.mesaj = ''; this.hata = 'Saha eklenirken hata oluştu.'; }
    });
  }
}
