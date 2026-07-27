import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Token'ı çözümler ve kullanıcının Admin (1) veya Owner (3) olup olmadığını kontrol eder
  isOwnerOrAdmin(): boolean {
    const token = localStorage.getItem('token');
    if (!token) return false;

    try {
      const payload = token.split('.')[1];
      let base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4) {
        base64 += '=';
      }

      const decodedPayload = JSON.parse(window.atob(base64));
      console.log('🚨 1. ÇÖZÜLEN TOKEN İÇERİĞİ:', decodedPayload);

      const roleClaim = decodedPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decodedPayload['role'];
      console.log('🚨 2. OKUNAN ROL DEĞERİ:', roleClaim);

      if (roleClaim == '1' || roleClaim == '3' || roleClaim === 'Admin' || roleClaim === 'Owner') {
        console.log('✅ 3. YETKİ ONAYLANDI! Kapı açılıyor.');
        return true;
      }

      console.log('❌ 3. YETKİ REDDEDİLDİ! Rol uyuşmadı.');
      return false;
    } catch (error) {
      console.error('💥 4. KOD ÇÖKERKEN HATA OLUŞTU:', error);
      return false;
    }
  }

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  register(kullanici: any): Observable<any> {
    // Artık backend text değil, JSON objesi (Token) dönüyor. responseType'ı kaldırdık.
    return this.http.post(`${this.apiUrl}/Auth/register`, kullanici);
  }

  checkEmail(email: string): Observable<{ exists: boolean }> {
    return this.http.get<{ exists: boolean }>(`${this.apiUrl}/Auth/check-email?email=${email}`);
  }

  login(kullanici: any) {
    return this.http.post(`${this.apiUrl}/Auth/login`, kullanici);
  }


  // Eğer localStorage'da giriş yapıldığına dair bir token varsa true, yoksa false döner.
  isLoggedIn(): boolean {
    if (typeof window !== 'undefined') { // Tarayıcı ortamında mıyız kontrolü (SSR hatalarını önler)
      return !!localStorage.getItem('token'); // Kendi kaydettiğin verinin adına göre 'token' kısmını değiştirebilirsin
    }
    return false;
  }
}
