import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  // İleride buraya "Token'ın süresi dolmuş mu (JWT Decode)?" kontrolü de ekleyeceğiz
  if (token) {
    return true;
  }

  // Token yoksa şutla
  router.navigate(['/login']);
  return false;
};
