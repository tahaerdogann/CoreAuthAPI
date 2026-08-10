import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CourtFormComponent } from '../../shared/court-form.component';

@Component({
  selector: 'app-courts',
  standalone: true,
  imports: [CommonModule, CourtFormComponent],
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
        alert('Yayın durumu güncellendi.');
        this.loadCourts();
      },
      error: (err) => alert('Hata: ' + (err.error?.message || err.message))
    });
  }

  deleteCourt(courtId: string) {
    if (confirm('Bu sahayı silmek istediğinize emin misiniz? (Mevcut kiralama ve takvimleri etkilenebilir)')) {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };
      this.http.delete(`${environment.apiUrl}/Courts/${courtId}`, { headers }).subscribe({
        next: (res: any) => {
          alert('Saha başarıyla silindi.');
          this.loadCourts();
        },
        error: (err) => alert('Hata: ' + (err.error?.message || err.message))
      });
    }
  }

  // Edit Logic
  openEditModal(court: any) {
    const courtId = court.id || court.Id;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    // Saha detayını çek ki form dolsun
    this.http.get(`${environment.apiUrl}/Courts/${courtId}`, { headers }).subscribe({
      next: (data: any) => {
        this.selectedCourtData = data;
        this.isEditModalOpen = true;
      },
      error: (err) => alert('Saha detayları alınamadı.')
    });
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.selectedCourtData = null;
  }

  onEditCourtSubmit(formData: any) {
    this.isUpdatingCourt = true;
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    // Düzenlenen sahanın Id'si
    const courtId = this.selectedCourtData.id || this.selectedCourtData.Id;

    this.http.put(`${environment.apiUrl}/Courts/${courtId}`, formData, { headers }).subscribe({
      next: (res) => {
        alert('Saha başarıyla güncellendi!');
        this.isUpdatingCourt = false;
        this.closeEditModal();
        this.loadCourts();
      },
      error: (err) => {
        console.error(err);
        alert('Güncelleme sırasında bir hata oluştu.');
        this.isUpdatingCourt = false;
      }
    });
  }
}
