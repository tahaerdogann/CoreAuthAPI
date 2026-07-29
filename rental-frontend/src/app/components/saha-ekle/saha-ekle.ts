import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
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

  onFormSubmit(payload: any) {
    this.hata = '';
    this.mesaj = 'Saha kaydediliyor...';
    this.isSubmitting = true;

    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`${environment.apiUrl}/Courts/add`, payload, { headers }).subscribe({
      next: (res: any) => {
        this.mesaj = res.message || 'Saha başarıyla eklendi!';
        this.isSubmitting = false;
        setTimeout(() => { this.router.navigate(['/owner-dashboard']); }, 1500);
      },
      error: (err) => { 
        this.mesaj = ''; 
        this.hata = err.error?.message || 'Saha eklenirken hata oluştu.';
        this.isSubmitting = false;
      }
    });
  }
}
