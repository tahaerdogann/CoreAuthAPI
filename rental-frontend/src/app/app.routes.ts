import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
// Sınıf adın büyük ihtimalle Login veya LoginComponent şeklindedir, ona göre güncelleyebilirsin
import { Login } from './components/login/login';

export const routes: Routes = [
  // 1. ANA SAYFA HERKESE AÇIK: Guard yok!
  { path: '', component: Dashboard },

  // 2. LOGIN SAYFASI
  { path: 'login', component: Login }
];
