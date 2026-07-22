import { Component } from '@angular/core';
import { Router } from '@angular/router'; // Yönlendirme için ekledik

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard {

  constructor(private router: Router) { }

  cikisYap() {
    // 1. Tarayıcının hafızasındaki VIP bileti (token) siliyoruz
    localStorage.removeItem('token');

    // 2. Kullanıcıyı tekrar giriş sayfasına postalıyrouz
    this.router.navigate(['/login']);
  }
}
