import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class CourtService {
  // DİKKAT: Buradaki 'localhost:XXXX' kısmını, API'nin çalıştığı gerçek portla değiştir.
  // (Backend çalışırken tarayıcıda açılan adres çubuğundan portuna bakabilirsin)
  private apiUrl = 'https://localhost:7284/api/Courts';

  constructor(private http: HttpClient) { }

  // 1. Sahaları Getir (GET)
  getSahalar() {
    return this.http.get(this.apiUrl);
  }

  // 2. Yeni Saha Ekle (POST)
  sahaEkle(sahaData: any) {
    return this.http.post(this.apiUrl, sahaData);
  }
}
