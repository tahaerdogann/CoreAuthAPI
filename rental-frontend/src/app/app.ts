import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common'; // ngIf için gerekli
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css'] // Varsa css dosyan
})
export class App { // DİKKAT: Burası AppComponent değil, App olmalı!
  userInfo: any = null;
  isProfileDropdownOpen: boolean = false;

  constructor(public authService: AuthService, private router: Router) { 
    if (this.authService.isLoggedIn()) {
      this.userInfo = this.authService.getUserInfo();
    }
  }

  toggleProfileDropdown() {
    this.isProfileDropdownOpen = !this.isProfileDropdownOpen;
  }

  cikisYap() {
    localStorage.removeItem('token');
    this.userInfo = null;
    this.isProfileDropdownOpen = false;
    this.router.navigate(['/login']);
  }
}
