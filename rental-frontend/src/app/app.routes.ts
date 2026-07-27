import { Routes, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Login } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { Dashboard } from './components/dashboard/dashboard';
import { AuthService } from './services/auth';
import { OwnerDashboardComponent } from './components/owner-dashboard/owner-dashboard';
import { SahaEkleComponent } from './components/saha-ekle/saha-ekle'; // BU İMPORTUN OLDUĞUNA EMİN OL!

// ... (Guard fonksiyonları aynen kalıyor: guestGuard, authGuard, ownerGuard) ...

const ownerGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }
  if (authService.isOwnerOrAdmin()) {
    return true;
  }
  router.navigate(['/dashboard']);
  return false;
};

export const routes: Routes = [
  // 1. Ziyaretçi Sayfaları
  { path: 'login', component: Login },
  { path: 'register', component: RegisterComponent },

  // 2. Herkesin Girebildiği Sayfalar
  { path: 'dashboard', component: Dashboard },

  // 3. VIP SAYFALAR (Patronlara Özel)
  { path: 'owner-dashboard', component: OwnerDashboardComponent, canActivate: [ownerGuard] },
  { path: 'saha-ekle', component: SahaEkleComponent, canActivate: [ownerGuard] }, // JOKER'DEN HEMEN YUKARIDA OLMALI!

  // 4. JOKER ROTALAR (Kesinlikle en altta olmalı!)
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' }
];
