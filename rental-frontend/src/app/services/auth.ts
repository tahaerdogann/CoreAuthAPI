import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

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
