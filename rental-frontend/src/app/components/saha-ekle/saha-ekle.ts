import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { CourtFormComponent } from '../shared/court-form.component';

@Component({
  selector: 'app-saha-ekle',
  standalone: true,
  imports: [CommonModule, CourtFormComponent],
  templateUrl: './saha-ekle.html',
  styleUrls: ['./saha-ekle.css']
})
export class SahaEkleComponent {
  mesaj: string = '';
  hata: string = '';
  isSubmitting = false;

  constructor(private http: HttpClient, private router: Router) {}

  async onFormSubmit(payload: any) {
    this.hata = '';
    this.mesaj = 'Fotoğraflar yükleniyor, lütfen bekleyin...';
    this.isSubmitting = true;

    try {
      const displayPhotos: any[] = payload.displayPhotos || [];
      const finalPhotos: any[] = [];
      let signatureRes: any = null;

      // Sadece yeni eklenecek (file) olanlar için imza al (Eğer varsa)
      const hasNewFiles = displayPhotos.some(p => p.file);
      if (hasNewFiles) {
        signatureRes = await firstValueFrom(this.http.get(`${environment.apiUrl}/Courts/get-upload-signature`));
      }

      for (let i = 0; i < displayPhotos.length; i++) {
        const item = displayPhotos[i];
        const isCover = (i === 0);
        
        if (item.file) {
          const uploadUrl = `https://api.cloudinary.com/v1_1/${signatureRes.cloudName}/image/upload`;
          const formData = new FormData();
          formData.append('file', item.file);
          formData.append('api_key', signatureRes.apiKey);
          formData.append('timestamp', signatureRes.timestamp);
          formData.append('signature', signatureRes.signature);
          formData.append('folder', signatureRes.folder);
          
          const res: any = await firstValueFrom(this.http.post(uploadUrl, formData));
          finalPhotos.push({
            url: res.secure_url,
            publicId: res.public_id,
            isCover: isCover,
            displayOrder: i
          });
        } else if (item.existingData) {
          finalPhotos.push({
            ...item.existingData,
            isCover: isCover,
            displayOrder: i
          });
        }
      }

      this.mesaj = 'Saha kaydediliyor...';

      // API Payload'unu hazırla
      const apiPayload = {
        name: payload.name,
        sportType: payload.sportType,
        surfaceType: payload.surfaceType,
        city: payload.city,
        district: payload.district,
        neighborhood: payload.neighborhood,
        addressDetail: payload.addressDetail,
        description: payload.description,
        latitude: payload.latitude,
        longitude: payload.longitude,
        hourlyPrice: payload.hourlyPrice,
        amenities: payload.amenities,
        rentalOptionsJson: payload.rentalOptionsJson,
        photos: finalPhotos
      };

      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      const res: any = await firstValueFrom(this.http.post(`${environment.apiUrl}/Courts/add`, apiPayload, { headers }));
      
      this.mesaj = res.message || 'Saha başarıyla eklendi!';
      this.isSubmitting = false;
      setTimeout(() => { this.router.navigate(['/owner-dashboard']); }, 1500);

    } catch (err: any) {
      this.mesaj = ''; 
      this.hata = err.error?.message || err.message || 'İşlem sırasında hata oluştu.';
      this.isSubmitting = false;
    }
  }
}
