import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms'; // Form için ekledik
import { CourtService } from '../../services/court'; // Servisimizi aldık

@Component({
  selector: 'app-dashboard',
  imports: [FormsModule], // Form elemanlarını kullanabilmek için ekledik
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  sahaListesi: any[] = [];

  // Formdan gelecek verileri tutacağımız obje
  yeniSaha = {
    name: '',
    type: 'Halısaha',
    hourlyPrice: 0,
    isActive: true
  };

  constructor(private router: Router, private courtService: CourtService) { }

  // Sayfa açılır açılmaz çalışacak metot
  ngOnInit() {
    this.sahalarıYukle();
  }

  sahalarıYukle() {
    this.courtService.getSahalar().subscribe({
      next: (data: any) => {
        this.sahaListesi = data;
        console.log("Sahalar başarıyla çekildi:", data);
      },
      error: (err) => console.error("Sahalar çekilirken hata oluştu:", err)
    });
  }

  sahaKaydet() {
    this.courtService.sahaEkle(this.yeniSaha).subscribe({
      next: (response) => {
        alert("Saha başarıyla eklendi!");
        this.sahalarıYukle(); // Listeyi güncelle
        // Formu sıfırla
        this.yeniSaha = { name: '', type: 'Halısaha', hourlyPrice: 0, isActive: true };
      },
      error: (err) => {
        console.error("Saha eklenemedi:", err);
        alert("Saha eklenirken bir hata oluştu.");
      }
    });
  }

  cikisYap() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
