import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  
  // 1. Cebimizdeki bileti (token) al
  const token = localStorage.getItem('token');
  let clonedReq = req;

  // 2. Eğer bilet varsa ve istek sadece kendi Backend'imize (apiUrl) gidiyorsa mektuba iliştir.
  // Bu sayede Cloudinary vb. dış servislere giden isteklere gereksiz/hatalı token gitmemiş olur.
  if (token && req.url.startsWith(environment.apiUrl)) {
    clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  // 3. İsteği yola çıkar ve dönüşte (response) olası hataları dinle
  return next(clonedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Backend sahte token'ı veya yetkisiz erişimi yakalayıp 401/403 dönerse
      if (error.status === 401 || error.status === 403) {
        console.error('🚨 GÜVENLİK UYARISI: Yetkisiz erişim denemesi yakalandı!', error);
        
        // Kullanıcıya mesaj göster
        // (alert yerine login sayfasına yönlendirip orda bir mesaj gösterebiliriz, şimdilik sessiz yönlendirme yapıyoruz)
        
        // Sahte/geçersiz token'ı sil
        localStorage.removeItem('token');
        
        // Sistemi kandırmaya çalışan kullanıcıyı kapı dışarı et (login'e yönlendir)
        router.navigate(['/login']);
      }
      
      // Diğer hatalar için akışı bozma, hatayı fırlatmaya devam et
      return throwError(() => error);
    })
  );
};
