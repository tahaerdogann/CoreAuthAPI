import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CourtService } from '../../services/court';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-saha-ekle',
  imports: [FormsModule],
  templateUrl: './saha-ekle.html',
  styleUrl: './saha-ekle.css'
})
export class SahaEkle {
  yeniSaha = {
    name: '',
    type: 'Football',
    hourlyPrice: 0
  };

  constructor(private courtService: CourtService, private router: Router) { }

  sahaKaydet() {
    this.courtService.ekle(this.yeniSaha).subscribe({
      next: (response: any) => {
        alert('Saha başarıyla eklendi!');
        // Ekledikten sonra ana sayfaya (vitrine) yönlendir
        this.router.navigate(['/']);
      },
      error: (err: any) => {
        console.error('Saha eklenirken hata:', err);
        alert('Saha eklenemedi! Yetkiniz olmayabilir.');
      }
    });
  }
}
