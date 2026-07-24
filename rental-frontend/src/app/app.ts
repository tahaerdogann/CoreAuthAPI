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

  constructor(public authService: AuthService, private router: Router) { }

  cikisYap() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
