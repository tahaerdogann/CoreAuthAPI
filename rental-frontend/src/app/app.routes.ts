import { Routes, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Login } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { Dashboard } from './components/dashboard/dashboard';
import { AuthService } from './services/auth';

// ---------------------------------------------------------
// PROFESYONEL BEKÇİ (GUARD): Zaten giriş yapmış olanları yakalar
// ---------------------------------------------------------
const guestGuard = () => {
  const authService = inject(AuthService); // Servisi çağır
  const router = inject(Router);           // Yönlendiriciyi çağır

  if (authService.isLoggedIn()) {
    // Adam zaten giriş yapmış, login sayfasına girmesini engelle ve dashboard'a at!
    router.navigate(['/dashboard']);
    return false;
  }
  return true; // Giriş yapmamışsa izin ver, login sayfasını görsün
};

export const routes: Routes = [
  // canActivate: [guestGuard] ekleyerek bu sayfalara kapı bekçisi koyduk
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },

  { path: 'dashboard', component: Dashboard },

  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' }
  { path: 'owner-dashboard', component: OwnerDashboardComponent }
];
