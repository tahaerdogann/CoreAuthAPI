import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CourtFormComponent } from '../../shared/court-form.component';
import { AlertModalComponent } from '../../shared/alert-modal.component';

import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-courts',
  standalone: true,
  imports: [CommonModule, CourtFormComponent, RouterModule, AlertModalComponent],
  templateUrl: './courts.html',
  styleUrls: ['./courts.css']
})
export class CourtsComponent implements OnInit {
  courts: any[] = [];
  isLoading = false;

  // Edit Modal State
  isEditModalOpen = false;
  selectedCourtData: any = null;
  isUpdatingCourt = false;

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
    this.loadCourts();
  }

  loadCourts() {
    this.isLoading = true;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    this.http.get(`${environment.apiUrl}/Courts/my-courts`, { headers }).subscribe({
      next: (res: any) => {
        if (res && res.$values) this.courts = res.$values;
        else if (Array.isArray(res)) this.courts = res;
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

  togglePublish(court: any) {
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    const courtId = court.id || court.Id;
    this.http.post(`${environment.apiUrl}/Courts/${courtId}/toggle-publish`, {}, { headers }).subscribe({
      next: (res: any) => {
        court.isPublished = res.isPublished;
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: 'Yayın durumu güncellendi.', type: 'success', isConfirm: false };
        this.loadCourts();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.alertModalState = { isOpen: true, title: 'Hata', message: 'Hata: ' + (err.error?.message || err.message), type: 'error', isConfirm: false };
        this.cdr.detectChanges();
      }
    });
  }

  deleteCourt(id: string) {
    if (!id) return;
    
    this.alertModalState = {
      isOpen: true,
      title: 'Emin misiniz?',
      message: 'Bu sahayı silmek istediğinize emin misiniz? (Bağlı rezervasyonlar iptal edilecektir!)',
      type: 'warning',
      isConfirm: true
    };

    this.pendingAction = () => {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };
      this.http.delete(`${environment.apiUrl}/Courts/${id}`, { headers }).subscribe({
        next: () => {
          this.alertModalState = { isOpen: true, title: 'Başarılı', message: 'Saha başarıyla silindi.', type: 'success', isConfirm: false };
          this.loadCourts();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          const errorMsg = err.error?.message || (typeof err.error === 'string' ? err.error : 'Silme işlemi sırasında hata oluştu.');
          this.alertModalState = { isOpen: true, title: 'Hata', message: errorMsg, type: 'error', isConfirm: false };
          this.cdr.detectChanges();
        }
      });
    };
  }

  // Edit & Add Logic
  openAddModal() {
    this.selectedCourtData = null;
    this.isEditModalOpen = true;
  }

  openEditModal(court: any) {
    const courtId = court.id || court.Id;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    this.http.get(`${environment.apiUrl}/Courts/${courtId}`, { headers }).subscribe({
      next: (data: any) => {
        this.selectedCourtData = data;
        this.isEditModalOpen = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.alertModalState = { isOpen: true, title: 'Hata', message: 'Saha detayları alınamadı.', type: 'error', isConfirm: false };
        this.cdr.detectChanges();
      }
    });
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.selectedCourtData = null;
  }

  async onCourtSubmit(formData: any) {
    this.isUpdatingCourt = true;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    try {
      const finalPhotos: any[] = [];
      let displayOrder = 0;

      for (let i = 0; i < formData.displayPhotos.length; i++) {
        const photo = formData.displayPhotos[i];
        const isCover = (i === 0);

        if (photo.file) {
          const uploadData = new FormData();
          uploadData.append('file', photo.file);
          uploadData.append('upload_preset', environment.cloudinary.uploadPreset);

          const uploadRes: any = await firstValueFrom(this.http.post(
            `https://api.cloudinary.com/v1_1/${environment.cloudinary.cloudName}/upload`, 
            uploadData
          ));

          finalPhotos.push({
            url: uploadRes.secure_url,
            publicId: uploadRes.public_id,
            isCover: isCover,
            displayOrder: displayOrder++
          });
        } else if (photo.existingData) {
          finalPhotos.push({
            url: photo.existingData.url,
            publicId: photo.existingData.publicId,
            isCover: isCover,
            displayOrder: displayOrder++
          });
        }
      }

      formData.Photos = finalPhotos;

      // Delete removed photos
      if (this.selectedCourtData && formData.deletedPhotoIds && formData.deletedPhotoIds.length > 0) {
        const courtId = this.selectedCourtData.id || this.selectedCourtData.Id;
        for (const photoId of formData.deletedPhotoIds) {
          try {
             await firstValueFrom(this.http.delete(`${environment.apiUrl}/Courts/${courtId}/photos/${photoId}`, { headers }));
          } catch(e) { console.error('Photo delete error', e); }
        }
      }

      delete formData.displayPhotos;
      delete formData.deletedPhotoIds;

      if (this.selectedCourtData) {
        // DÜZENLEME (PUT)
        const courtId = this.selectedCourtData.id || this.selectedCourtData.Id;
        await firstValueFrom(this.http.put(`${environment.apiUrl}/Courts/${courtId}`, formData, { headers }));
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: 'Saha başarıyla güncellendi.', type: 'success', isConfirm: false };
      } else {
        // YENİ EKLEME (POST)
        await firstValueFrom(this.http.post(`${environment.apiUrl}/Courts/add`, formData, { headers }));
        this.alertModalState = { isOpen: true, title: 'Başarılı', message: 'Yeni saha başarıyla eklendi.', type: 'success', isConfirm: false };
      }
      
      this.isUpdatingCourt = false;
      this.closeEditModal();
      this.loadCourts();
      this.cdr.detectChanges();

    } catch (err: any) {
      console.error(err);
      this.isUpdatingCourt = false;
      this.alertModalState = { isOpen: true, title: 'Hata', message: 'İşlem sırasında hata oluştu.', type: 'error', isConfirm: false };
      this.cdr.detectChanges();
    }
  }

  onAlertConfirm() {
    this.alertModalState.isOpen = false;
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }
}
