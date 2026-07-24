import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment'; // Ortam değişkenin neresiyse ona göre ayarla
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OwnerService {
  // Kendi portuna ve API yoluna göre kontrol et (Genelde environment.apiUrl kullanılır)
  private apiUrl = 'https://localhost:7284/api/OwnerDashboard';

  constructor(private http: HttpClient) { }

  // 1. Kullanıcının kendi sahalarını getirir
  getMyCourts(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/my-courts`);
  }

  // 2. Seçilen sahanın slotlarını (ilanlarını) getirir
  getCourtSlots(courtId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/my-courts/${courtId}/slots`);
  }

  // 3. Yeni slot (ilan) oluşturur
  createSlot(courtId: number, slotData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/my-courts/${courtId}/slots`, slotData);
  }
}
