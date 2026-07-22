import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class Auth {

  private apiUrl = 'https://localhost:7284/api/Auth';

  constructor(private http: HttpClient) { }

  login(data: any) {
    return this.http.post(`${this.apiUrl}/login`, data);
  }

  register(data: any) {
    // Angular'a backend'den düz yazı (text) döneceğini söylüyoruz
    return this.http.post(`${this.apiUrl}/register`, data, { responseType: 'text' });
  }
}
