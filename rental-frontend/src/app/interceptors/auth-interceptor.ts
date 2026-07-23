import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // 1. Cebimizdeki bileti (token) al
  const token = localStorage.getItem('token');

  // 2. Eğer bilet varsa, backend'e giden mektubun (isteğin) içine bunu iliştir
  if (token) {
    const clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    // Güncellenmiş mektubu yola çıkar
    return next(clonedReq);
  }

  // 3. Eğer bilet yoksa mektubu olduğu gibi gönder (zaten backend reddedecek)
  return next(req);
};  
