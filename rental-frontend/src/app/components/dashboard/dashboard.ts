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
        if (data && data.$values) this.sahaListesi = data.$values;
        else if (Array.isArray(data)) this.sahaListesi = data;
        else this.sahaListesi = [];
      },
      error: (err: any) => console.error("Sahalar yüklenirken hata:", err)
    });
  }

  getCoverPhoto(court: any): string | null {
    if (!court || !court.photos) return null;
    
    let photos = [];
    if (court.photos.$values) {
      photos = court.photos.$values;
    } else if (Array.isArray(court.photos)) {
      photos = court.photos;
    }

    if (photos.length === 0) return null;

    const cover = photos.find((p: any) => p.isCover);
    if (cover) return cover.url;

    return photos[0].url;
  }

  onSearch() {
    this.sahalarıYukle();
  }

  goToCourtDetail(id: string) {
    this.router.navigate(['/court-detail', id]);
  }
}
