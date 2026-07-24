import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment'; // 1. Ortam dosyasını çağırdık

@Injectable({
  providedIn: 'root'
})
export class CourtService {

  // 2. Adresi elle yazmak yerine merkezden çektik ve sonuna 'Courts' ekledik
  private apiUrl = `${environment.apiUrl}/Courts`;

  constructor(private http: HttpClient) { }

  getSahalar(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  ekle(saha: any): Observable<any> {
    return this.http.post(this.apiUrl, saha);
  }
}
