import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class Dashboard implements OnInit {
  sahaListesi: any[] = [];
  
  // Arama filtreleri
  searchLocation: string = '';
  searchSportType: string = '';

  constructor(private http: HttpClient, private router: Router) { }

  ngOnInit() {
    this.sahalarıYukle();
  }

  sahalarıYukle() {
    let url = `${environment.apiUrl}/Courts/search`;
    let queryParams = [];
    
    if (this.searchLocation.trim()) {
      queryParams.push(`city=${encodeURIComponent(this.searchLocation.trim())}`);
    }
    if (this.searchSportType) {
      queryParams.push(`sportType=${encodeURIComponent(this.searchSportType)}`);
    }

    if (queryParams.length > 0) {
      url += '?' + queryParams.join('&');
    }

    this.http.get(url).subscribe({
      next: (data: any) => {
        this.sahaListesi = data;
      },
      error: (err: any) => console.error("Sahalar yüklenirken hata:", err)
    });
  }

  onSearch() {
    this.sahalarıYukle();
  }

  goToCourtDetail(id: number) {
    this.router.navigate(['/court-detail', id]);
  }
}
