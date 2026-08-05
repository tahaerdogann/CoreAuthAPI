import { Routes, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Login } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { Dashboard } from './components/dashboard/dashboard';
import { CourtDetailComponent } from './components/court-detail/court-detail';
import { AuthService } from './services/auth';
import { OwnerDashboardComponent } from './components/owner-dashboard/owner-dashboard';
import { SahaEkleComponent } from './components/saha-ekle/saha-ekle';

// ... (Guard fonksiyonları aynen kalıyor: guestGuard, authGuard, ownerGuard) ...

import { map, take } from 'rxjs/operators';
import { of } from 'rxjs';

import { ProfileComponent } from './components/user-panel/profile/profile';
import { SessionsComponent } from './components/user-panel/sessions/sessions';
import { FavoritesComponent } from './components/user-panel/favorites/favorites';
import { UserPanelLayoutComponent } from './components/user-panel/layout/layout';

const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }
  return true;
};

const ownerGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  // Eğer bilgiler henüz çekilmediyse backend'den çekilmesini bekle
  if (!authService.getUserInfo()) {
    return authService.verifyToken().pipe(
      take(1),
      map(user => {
        if (user && (user.role === '1' || user.role === '3' || user.role === 'Admin' || user.role === 'Owner')) {
          return true;
        }
        router.navigate(['/dashboard']);
        return false;
      })
    );
  }

  // Bilgiler zaten varsa anında cevap dön (senkron)
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
  { path: 'court-detail/:id', component: CourtDetailComponent },

  // 3. KULLANICI SAYFALARI (Giriş zorunlu)
  { path: 'profile', redirectTo: 'user/profile', pathMatch: 'full' },
  { path: 'sessions', redirectTo: 'user/sessions', pathMatch: 'full' },
  { path: 'favorites', redirectTo: 'user/favorites', pathMatch: 'full' },
  {
    path: 'user',
    component: UserPanelLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'profile', component: ProfileComponent },
      { path: 'sessions', component: SessionsComponent },
      { path: 'favorites', component: FavoritesComponent },
      { path: '', redirectTo: 'profile', pathMatch: 'full' }
    ]
  },

  // 4. VIP SAYFALAR (Patronlara Özel)
  { path: 'owner-dashboard', component: OwnerDashboardComponent, canActivate: [ownerGuard] },
  { path: 'saha-ekle', component: SahaEkleComponent, canActivate: [ownerGuard] }, // JOKER'DEN HEMEN YUKARIDA OLMALI!

  // 5. JOKER ROTALAR (Kesinlikle en altta olmalı!)
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' }
];

