import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, map, switchMap } from 'rxjs/operators';
import { isPlatformBrowser } from '@angular/common';

export interface UserInfo {
  email: string;
  phoneNumber?: string;
  name: string;
  role: string;
  roleText: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  
  // UI'ın anlık tepki verebilmesi için kullanıcı bilgilerini tutuyoruz
  private currentUserSubject = new BehaviorSubject<UserInfo | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  
  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    // Sayfa yenilendiğinde token varsa arka planda doğrula
    if (this.isLoggedIn()) {
      this.verifyToken().subscribe();
    }
  }

  // GÜVENLİ METOT: Backend'e sorar, token imzasını onaylatır
  verifyToken(): Observable<UserInfo | null> {
    return this.http.get<UserInfo>(`${this.apiUrl}/Auth/me`).pipe(
      tap(user => {
        this.currentUserSubject.next(user);
        console.log('✅ Token başarıyla backend tarafından onaylandı:', user);
      }),
      catchError(error => {
        console.error('❌ Backend tokenı geçersiz buldu!', error);
        this.currentUserSubject.next(null);
        return of(null);
      })
    );
  }

  // UI için senkron değer döner (app.html vb.)
  getUserInfo(): UserInfo | null {
    return this.currentUserSubject.value;
  }

  // UI için senkron rol kontrolü
  isOwnerOrAdmin(): boolean {
    const user = this.currentUserSubject.value;
    if (!user) return false;
    return user.role === '1' || user.role === '3' || user.role === 'Admin' || user.role === 'Owner';
  }

  register(kullanici: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register`, kullanici);
  }

  checkIdentifier(identifier: string): Observable<{ exists: boolean }> {
    return this.http.get<{ exists: boolean }>(`${this.apiUrl}/Auth/check-identifier?identifier=${identifier}`);
  }

  updateProfile(data: {name: string, surname: string, phoneNumber: string, email: string}): Observable<any> {
    return this.http.put(`${this.apiUrl}/Auth/update-profile`, data);
  }

  changePassword(data: {currentPassword: string, newPassword: string}): Observable<any> {
    return this.http.put(`${this.apiUrl}/Auth/change-password`, data);
  }

  login(kullanici: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/login`, kullanici).pipe(
      switchMap((response: any) => {
        if (response && (response.token || response.Token)) {
          // Token'ı kaydet (login.ts'de de yapılıyor olabilir ama burada yapmak daha garantidir)
          localStorage.setItem('token', response.token || response.Token);
          // Giriş başarılı olduysa kimliğini backend'den alıp bekle
          return this.verifyToken().pipe(map(() => response));
        }
        return of(response);
      })
    );
  }

  isLoggedIn(): boolean {
    if (isPlatformBrowser(this.platformId)) {
      return !!localStorage.getItem('token');
    }
    return false;
  }
}
