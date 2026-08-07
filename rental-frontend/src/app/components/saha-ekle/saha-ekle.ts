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
      const uploadedPhotos: any[] = [];
      const selectedFiles: File[] = payload.selectedFiles || [];
      const existingPhotos: any[] = payload.existingPhotos || [];

      // Cloudinary'ye Yükleme
      if (selectedFiles.length > 0) {
        // İmzayı Backend'den al
        const signatureRes: any = await firstValueFrom(this.http.get(`${environment.apiUrl}/Courts/get-upload-signature`));
        const uploadUrl = `https://api.cloudinary.com/v1_1/${signatureRes.cloudName}/image/upload`;

        for (let file of selectedFiles) {
          const formData = new FormData();
          formData.append('file', file);
          formData.append('api_key', signatureRes.apiKey);
          formData.append('timestamp', signatureRes.timestamp);
          formData.append('signature', signatureRes.signature);
          formData.append('folder', signatureRes.folder); // Backend folder bilgisini veriyor

          const res: any = await firstValueFrom(this.http.post(uploadUrl, formData));
          uploadedPhotos.push({
            url: res.secure_url,
            publicId: res.public_id,
            isCover: false // Kapak mantığı daha sonra düzenleniyor
          });
        }
      }

      this.mesaj = 'Saha kaydediliyor...';

      // Tüm fotoğrafları birleştir (mevcutlar + yeniler)
      let allPhotos = [...existingPhotos, ...uploadedPhotos];
      // Eğer hiç kapak yoksa ilk fotoğrafı kapak yap
      if (allPhotos.length > 0 && !allPhotos.some(p => p.isCover)) {
        allPhotos[0].isCover = true;
      }

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
        photos: allPhotos
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
